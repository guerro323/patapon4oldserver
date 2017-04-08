using Lidgren.Network;
using PataponPhotonShared;
using PataponPhotonShared.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Patapon4GlobalServer
{
    public static class SystemMessage
    {
        /// <summary>
        /// List of know users (key is login)
        /// </summary>
        public static Dictionary<string, GameUser> users = new Dictionary<string, GameUser>();
        public static Dictionary<NetConnection, GameUser> peerGameUsers = new Dictionary<NetConnection, GameUser>();
        public static List<GameRoom> rooms = new List<GameRoom>();

        public static GameUser FindUser(string login)
        {
            return users.ContainsKey(login) ? users[login] : null;
        }

        public static async void Receive(NetIncomingMessage message, string type)
        {
            if (Program.breakPoint)
            {
                Console.Write("BREAK");
            }

            switch (type)
            {
                case Constants.SYSTEM.HANDLE_SUCCESS:
                    {
                        if (!ConnectionExtension.Hash.ContainsKey(message.SenderConnection))
                            ConnectionExtension.Hash[message.SenderConnection] = new Hashtable();
                        if (!ConnectionExtension.Hash[message.SenderConnection].ContainsKey("handle"))
                            ConnectionExtension.Hash[message.SenderConnection]["handle"] = new ArrayList();

                        var handle = message.ReadInt32();
                        var hash = ConnectionExtension.Hash[message.SenderConnection];
                        ((ArrayList)hash["handle"]).Add(handle);
                        Console.WriteLine("RECEIVED HANDLE " + handle);
                        break;
                    }

                case Constants.SYSTEM.GO_USERCREATE:
                    {
                        string login = message.ReadString();
                        string password = message.ReadString();
                        string email = message.ReadString();

                        var sendMsg = Program.Server.CreateMessage("HEADER");

                        if (FindUser(login) != null)
                        {
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.ERROR_USERCREATE);
                            sendMsg.Write("ALREADYEXIST");

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            return;
                        }
                        else if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(email))
                        {
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.ERROR_USERCREATE);
                            sendMsg.Write("EMPTYFIELDS");

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            return;
                        }

                        var user = new GameUser();
                        user.login = login;
                        user.password = password;
                        user.email = email;
                        user.guid = Guid.NewGuid().ToString();
                        user.nickname = user.login;
                        user.currConnectionId = "Need-To-Be-Connected";
                        user.currentPlayerIndex = 0;

                        users[user.login] = user;

                        sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                        sendMsg.Write(Constants.SYSTEM.SUCCESS_USERCREATE);

                        Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);

                        break;
                    }
                case Constants.SYSTEM.GO_USERCONNECT:
                    {
                        string login = message.ReadString();
                        string password = message.ReadString();
                        GameUser user = FindUser(login);

                        var sendMsg = Program.Server.CreateMessage("HEADER");

                        if (user == null)
                        {
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.ERROR_USERCONNECT);
                            sendMsg.Write("DONTEXIST");

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            return;
                        }

                        if (user.password != password)
                        {
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.ERROR_USERCONNECT);
                            sendMsg.Write("WRONGPASSWORD");

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            return;
                        }

                        user.currConnectionId = Guid.NewGuid().ToString();

                        List<NetConnection> connectionList = new List<NetConnection>();
                        connectionList = Program.Server.Connections;
                        connectionList.Remove(message.SenderConnection);

                        // Send Event to all players
                        if (connectionList.Count > 0)
                        {
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.EVENT_ON_USERCONNECT);
                            byte[] userByted = user.Serialize();
                            sendMsg.Write(userByted.Count());
                            sendMsg.Write(userByted);

                            Program.Server.SendMessage(sendMsg, connectionList, NetDeliveryMethod.ReliableOrdered, 0);
                        }
                        // Send Event and connection id to the player who asked
                        {
                            sendMsg = Program.Server.CreateMessage("HEADER");

                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.EVENT_ON_USERCONNECT);
                            byte[] userByted = user.Serialize();
                            sendMsg.Write(userByted.Count());
                            sendMsg.Write(userByted);
                            sendMsg.Write(user.currConnectionId);

                            peerGameUsers[message.SenderConnection] = user;

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                        break;
                    }
                case Constants.SYSTEM.GO_USERDISCONNECT:
                    {
                        if (peerGameUsers.ContainsKey(message.SenderConnection))
                        {
                            var user = peerGameUsers[message.SenderConnection];
                            user.currConnectionId = "disconnected";

                            if (user.currentRoom != null && user.currentRoom.playersIn.Contains(user.login))
                            {
                                var index = Array.IndexOf(user.currentRoom.playersIn, user.login);
                                if (index >= 0)
                                    user.currentRoom.playersIn[index] = null;

                                UpdateRoomNet(user.currentRoom);
                            }

                            user.currentRoom = null;
                        }
                        message.SenderConnection.Disconnect("SUCCESS_DISCONNECT");
                        break;
                    }
                case Constants.SYSTEM.GO_ROOMCREATE:
                    {
                        if (peerGameUsers.ContainsKey(message.SenderConnection))
                        {
                            GameUser user = peerGameUsers[message.SenderConnection];

                            // < Count de la classe recherche
                            int objCount = message.ReadInt32();
                            GameRoomOption option = message.ReadBytes(objCount).Deserialize<GameRoomOption>();

                            var room = GameRoom.Create(option);

                            room.guid = Guid.NewGuid().ToString();
                            room.creatorName = string.IsNullOrEmpty(user.nickname) ? user.login : user.nickname;

                            room.ipAdress = Program.currentIP;
                            room.port = RoomManager.AvailablePort();
                            room.hasLoaded = false;
                            room.playersIn = new string[room.maxPlayers];


                            Console.WriteLine($"? {room.creatorName} {room.description} {room.port}");

                            var sendMsg = Program.Server.CreateMessage("HEADER");

                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.SUCCESS_ROOMCREATE);

                            byte[] roomSerialized = room.Serialize();
                            sendMsg.Write(roomSerialized.Count());
                            sendMsg.Write(roomSerialized);

                            rooms.Add(room);
                            RoomManager.AddRoom(room, room.port);

                            Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                        }
                        else
                        {
                            // Throw error
                        }

                        break;
                    }
                case Constants.SYSTEM.GO_ROOMJOIN:
                    {
                        if (peerGameUsers.ContainsKey(message.SenderConnection))
                        {
                            var sendMsg = Program.Server.CreateMessage("HEADER");
                            GameRoom joinedRoom = null;
                            GameUser user = peerGameUsers[message.SenderConnection];

                            string roomGUID = message.ReadString();
                            foreach (var room in rooms)
                                if (room.guid == roomGUID)
                                    joinedRoom = room;

                            await Task.Factory.StartNew(() =>
                            {
                                while (joinedRoom != null && !joinedRoom.hasLoaded) ;
                            });

                            if (joinedRoom != null)
                            {
                                user.currentRoom = joinedRoom;

                                NetConnection gameServerConnection = null;
                                int timeToSearch = Environment.TickCount + 4000;
                                await Task.Factory.StartNew(() =>
                                {
                                    while (gameServerConnection == null && timeToSearch > Environment.TickCount)
                                    {
                                        foreach (var con in Program.Server.Connections)
                                        {
                                            var ip = con.RemoteEndPoint.Address.ToString();
                                            /*if (con.RemoteEndPoint.ToString()
                                                .StartsWith("127.0.0.1"))
                                            {
                                                ip = "127.0.0.1";
                                            }*/
                                            /*if (con.RemoteEndPoint.ToString()
                                                .StartsWith("192.168.0"))
                                            {
                                                ip = con.RemoteEndPoint.Address.ToString();
                                            }*/

                                            Console.WriteLine($"{con.RemoteEndPoint.ToString()} == {ip}:{joinedRoom.port}");
                                            if (con.RemoteEndPoint.ToString() == $"{ip}:{joinedRoom.port}")
                                            {
                                                gameServerConnection = con;
                                                //joinedRoom.ipAdress = ip;
                                            }
                                        }
                                    }
                                });
                                Console.WriteLine($"Is gameServerNetPeer null? {gameServerConnection == null}");

                                if (gameServerConnection == null)
                                {
                                    sendMsg = Program.Server.CreateMessage("HEADER");

                                    sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                                    sendMsg.Write(Constants.SYSTEM.ERROR_ROOMJOIN);

                                    Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                    return;
                                }
                                else
                                {

                                    sendMsg = Program.Server.CreateMessage("HEADER");
                                    sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                    sendMsg.Write(Constants.OUTGAME.EVENT_ON_PLAYERCONNECT);

                                    var userSerialized = user.Serialize();
                                    sendMsg.Write(userSerialized.Length);
                                    sendMsg.Write(userSerialized);

                                    var playerSerialized = user.ownedPlayers[user.currentPlayerIndex].Serialize();
                                    sendMsg.Write(playerSerialized.Length);
                                    sendMsg.Write(playerSerialized);

                                    Program.Server.SendMessage(sendMsg, gameServerConnection, NetDeliveryMethod.ReliableOrdered);

                                    await gameServerConnection.WaitNetHandle();
                                    await Task.Factory.StartNew(() => { Thread.Sleep(200); });

                                    if (user.currentRoom != null)
                                    {
                                        foreach (var entitydata in user.ownedPlayers[user.currentPlayerIndex].EntitiesInPossession)
                                        {
                                            sendMsg = Program.Server.CreateMessage("HEADER");
                                            sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                            sendMsg.Write(Constants.OUTGAME.EVENT_ON_UPDATEONLINEENTITY);
                                            sendMsg.Write(user.login);
                                            var _objSerialized = entitydata.Serialize();
                                            {
                                                sendMsg.Write(_objSerialized.Length);
                                                sendMsg.Write(_objSerialized);
                                            }

                                            // GameServer will not care about ArmyIndex

                                            Program.Server.SendMessage(sendMsg, gameServerConnection, NetDeliveryMethod.ReliableOrdered);
                                        }
                                    }

                                    joinedRoom.netConnection = gameServerConnection;

                                    var handle = await gameServerConnection.WaitNetHandle();
                                    Console.WriteLine("SEND MESSAGE" + handle);

                                    var index = Array.IndexOf(joinedRoom.playersIn, user.login);
                                    if (index < 0)
                                        joinedRoom.playersIn[joinedRoom.nbPlayers] = user.login;

                                    UpdateRoomNet(joinedRoom);

                                    sendMsg = Program.Server.CreateMessage("HEADER");
                                    sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                                    sendMsg.Write(Constants.SYSTEM.SUCCESS_ROOMJOIN);
                                    sendMsg.Write(joinedRoom.ipAdress);
                                    sendMsg.Write(joinedRoom.port);
                                    var objSerialized = joinedRoom.Serialize();
                                    sendMsg.Write(objSerialized.Length);
                                    sendMsg.Write(objSerialized);

                                    Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                }
                            }
                            else
                            {
                                sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                                sendMsg.Write(Constants.SYSTEM.ERROR_ROOMJOIN);

                                Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            }
                        }
                        break;
                    }
                case Constants.SYSTEM.GO_ROOMLEAVE:
                    {
                        if (peerGameUsers.ContainsKey(message.SenderConnection))
                        {
                            GameUser user = peerGameUsers[message.SenderConnection];

                            var room = user.currentRoom;
                            var index = Array.IndexOf(room.playersIn, user.login);
                            if (index >= 0)
                                room.playersIn[index] = null;

                            var newArray = new string[room.maxPlayers];
                            var i = 0;
                            foreach (var roomIn in room.playersIn)
                            {
                                if (roomIn != null)
                                {
                                    newArray[i] = roomIn;
                                    i++;
                                }
                            }

                            user.currentRoom = null;

                            UpdateRoomNet(room);
                        }
                        break;
                    }
                case Constants.SYSTEM.GO_ROOMLIST:
                    {
                        if (peerGameUsers.ContainsKey(message.SenderConnection))
                        {
                            var objCount = message.ReadInt32();
                            PataponPhotonShared.SearchOption[] options = message.ReadBytes(objCount).Deserialize<PataponPhotonShared.SearchOption[]>();

                            var sendMsg = Program.Server.CreateMessage("HEADER");
                            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                            sendMsg.Write(Constants.SYSTEM.SUCCESS_GETTINGROOMLIST);
                            var maxCount = rooms.Count();
                            if (maxCount > 25)
                                maxCount = 25;
                            if (options.Count() == 0)
                            {
                                // Give all the rooms
                                var roomsArray = rooms.GetRange(0, maxCount).ToArray();
                                var objSerialized = roomsArray.Serialize();
                                sendMsg.Write(objSerialized.Count());
                                sendMsg.Write(objSerialized);

                                Program.Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                            }
                        }
                        break;
                    }
                case Constants.SYSTEM.SHARE_USERDISCONNECT_FROM_ROOM:
                    {
                        if (message.SenderConnection.Tag is object[] &&
                            ((object[])message.SenderConnection.Tag)[0] is string)
                            if ((string)((object[])message.SenderConnection.Tag)[0] == "GAMEROOM")
                            {
                                var login = message.ReadString();
                                var room = (GameRoom)((object[])message.SenderConnection.Tag)[1];
                                var index = Array.IndexOf(room.playersIn, login);
                                if (index >= 0)
                                    room.playersIn[index] = null;

                                var newArray = new string[room.maxPlayers];
                                var i = 0;
                                foreach (var roomIn in room.playersIn)
                                {
                                    if (roomIn != null)
                                    {
                                        newArray[i] = roomIn;
                                        i++;
                                    }  
                                }

                                foreach (var user in users)
                                {
                                    if (user.Value.login == login)
                                        user.Value.currentRoom = null;
                                }
                            }
                        break;
                    }

            }
        }

        static void UpdateRoomNet(GameRoom room)
        {
            var sendMsg = Program.Server.CreateMessage("HEADER");
            sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
            sendMsg.Write(Constants.OUTGAME.EVENT_ON_RECEIVEROOMINFO);
            var objSerialized = room.Serialize();
            sendMsg.Write(objSerialized.Length);
            sendMsg.Write(objSerialized);

            var userList = peerGameUsers
                .Where(kvpUser => kvpUser.Value.currentRoom == room)
                .Select(kvp => kvp.Key).ToList();

            if (userList.Count > 0)
                Program.Server.SendMessage(sendMsg, userList, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static string GetIP()
        {
            string externalIP = "";
            externalIP = (new WebClient()).DownloadString("https://api.ipify.org");
            externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(externalIP)[0].ToString();
            return externalIP;
        }
    }
}
