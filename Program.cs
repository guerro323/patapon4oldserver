using Lidgren.Network;
using ZeroFormatter;
using PataponPhotonShared;
using PataponPhotonShared.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PataponPhotonShared.Schemes;
using ZeroFormatter.Formatters;
using MsgPack.Serialization;
using PataponPhotonShared.Structs;

namespace Patapon4GlobalServer
{
    class Program
    {
        public static string currentIP;

        #region Public Fields

        public const string ApplicationID = "(B.A0001)patapon4-alphaclient";

        public static NetServer Server;

        #endregion Public Fields

        #region Private Methods

        static void Main(string[] args)
        {
            var config = new NetPeerConfiguration(ApplicationID)
            {
                ReceiveBufferSize = 1310710,
                SendBufferSize = 1310710,
                Port = 7641
            };
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            config.EnableMessageType(NetIncomingMessageType.WarningMessage);

            Server = new NetServer(config);
            Server.Start();
            Console.WriteLine(Server.Port);

            var thread = new Thread(updateCommand);
            thread.Start();

            currentIP = SystemMessage.GetIP();
            SetConsoleCtrlHandler(new HandlerRoutine(OnProcessExit), true);

            VerifyUsers();
            
            /*SystemMessage.users["MyAccount"] = new GameUser()
            {
                login = "MyAccount",
                nickname = "MyAccount",
                password = "MyAccount",
                currentPlayerIndex = 0,
                guid = Guid.NewGuid().ToString(),
                ownedPlayers = new Dictionary<int, GamePlayer>
                {
                    { 0, new GamePlayer()
                        {
                            MightyName = "TheMightyOne",
                            Inventory = new ItemData[]
                            {
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                            },
                            armyEntitiesInfo = new Dictionary<string, PataponPhotonShared.Entity.EntityData>()
                            {
                                { "id01", new PataponPhotonShared.Entity.EntityData() },
                                { "id02", new PataponPhotonShared.Entity.EntityData() },
                                { "id03", new PataponPhotonShared.Entity.EntityData() },
                                { "id04", new PataponPhotonShared.Entity.EntityData() },
                                { "id05", new PataponPhotonShared.Entity.EntityData() },
                                { "id06", new PataponPhotonShared.Entity.EntityData() },
                            },
                            armyScheme = new PataponPhotonShared.Schemes.ArmyScheme("helloworld")
                        }
                    }
                },
                SavesData = new Dictionary<int, SaveData>()
                {
                    { 0, new SaveData(0, "TheMightyOne", "Recover the catapult", new DateTime(2016, 12, 4), "MyAccount") },
                }
            };
            
            SystemMessage.users["TheLegend27"] = new GameUser()
            {
                login = "TheLegend27",
                nickname = "John Cena",
                password = "TheLegend27",
                currentPlayerIndex = 0,
                guid = Guid.NewGuid().ToString(),
                ownedPlayers = new Dictionary<int, GamePlayer>
                {
                    { 0, new GamePlayer()
                        {
                            MightyName = "John Cena The Mighty",
                            Inventory = new ItemData[]
                            {
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                            },
                            armyEntitiesInfo = new Dictionary<string, PataponPhotonShared.Entity.EntityData>()
                            {
                                { "id01", new PataponPhotonShared.Entity.EntityData() },
                                { "id02", new PataponPhotonShared.Entity.EntityData() },
                                { "id03", new PataponPhotonShared.Entity.EntityData() },
                                { "id04", new PataponPhotonShared.Entity.EntityData() },
                                { "id05", new PataponPhotonShared.Entity.EntityData() },
                                { "id06", new PataponPhotonShared.Entity.EntityData() },
                            },
                            armyScheme = new PataponPhotonShared.Schemes.ArmyScheme("helloworld")
                        }
                    },
                    { 1, new GamePlayer()
                        {
                            MightyName = "Game Of War",
                            Inventory = new ItemData[]
                            {
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                                new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),new ItemData(),
                            },
                            armyEntitiesInfo = new Dictionary<string, PataponPhotonShared.Entity.EntityData>()
                            {
                                { "id01", new PataponPhotonShared.Entity.EntityData() },
                                { "id02", new PataponPhotonShared.Entity.EntityData() },
                                { "id03", new PataponPhotonShared.Entity.EntityData() },
                                { "id04", new PataponPhotonShared.Entity.EntityData() },
                                { "id05", new PataponPhotonShared.Entity.EntityData() },
                                { "id06", new PataponPhotonShared.Entity.EntityData() },
                            },
                            armyScheme = new PataponPhotonShared.Schemes.ArmyScheme("helloworld")
                        }
                    }
                },
                SavesData = new Dictionary<int, SaveData>()
                {
                    { 0, new SaveData(0, "John Cena The Mighty", "Meden captured!", new DateTime(2016, 12, 4), "TheLegend27") },
                    { 1, new SaveData(1, "Game Of War", "TheLegend27 kicked my ass out of the heaven.", new DateTime(2016, 12, 4), "TheLegend27") },
                }    
            };
            
            OnProcessExit(CtrlTypes.CTRL_CLOSE_EVENT);
            */
            while (true)
            {
                NetIncomingMessage message;
                if ((message = Server.ReadMessage()) != null)
                {
                    Console.WriteLine("message r!");
                    switch (message.MessageType)
                    {
                        case NetIncomingMessageType.WarningMessage:
                            {
                                Console.WriteLine("WA !!! : " + message.ReadString());
                                break;
                            }
                        case NetIncomingMessageType.ConnectionApproval:
                            {
                                string connectionType = message.ReadString();

                                if (connectionType == "CONNECT")
                                {
                                    string requestType = "CLIENT";
                                    message.ReadString(out requestType);
                                    Console.WriteLine(requestType);
                                    if (requestType == "CLIENT")
                                    {
                                        message.SenderConnection.Tag = new object[] { requestType };
                                        message.SenderConnection.Approve();
                                    }
                                    else if (requestType == "GAMEROOM")
                                    {
                                        GameRoom room = null;
                                        var ip = message.SenderConnection.RemoteEndPoint.Address.ToString();
                                        /*if (message.SenderConnection.RemoteEndPoint.ToString()
                                            .StartsWith("127.0.0.1"))
                                        {
                                            ip = "127.0.0.1";
                                        }*/

                                        room = SystemMessage.rooms
                                            .Where(r => message.SenderConnection.RemoteEndPoint.ToString() == $"{ip}:{r.port}")
                                            .FirstOrDefault();
                                        message.SenderConnection.Tag = new object[] { requestType, room };

                                        message.SenderConnection.Approve();
                                    }
                                    else
                                        message.SenderConnection.Deny("Incorrect Request Type");
                                }
                                else
                                    message.SenderConnection.Deny("Incorrect Header");
                                break;
                            }

                        case NetIncomingMessageType.Data:
                            {
                                // handle custom messages
                                var header = "";
                                //message.ReadByte();
                                //message.ReadInt64();
                                if (message.ReadString(out header) && header == "HEADER")
                                {
                                    string headerType = message.ReadString();
                                    switch (headerType)
                                    {
                                        case Constants.MESSAGETYPE.SYSTEM:
                                            {
                                                var systemType = message.ReadString();
                                                SystemMessage.Receive(message, systemType);
                                                break;
                                            }
                                        case Constants.MESSAGETYPE.OUTGAME:
                                            {
                                                var systemType = message.ReadString();
                                                OutGameMessage.Receive(message, systemType);
                                                break;
                                            }
                                    }
                                }
                                break;
                            }



                        case NetIncomingMessageType.StatusChanged:
                            // handle connection status messages
                            Console.WriteLine(message.SenderConnection.Status);
                            switch (message.SenderConnection.Status)
                            {
                                case NetConnectionStatus.Connected:
                                    {
                                        if (message.SenderConnection.Tag is object[] &&
                                            ((object[])message.SenderConnection.Tag)[0] is string)
                                            if ((string)((object[])message.SenderConnection.Tag)[0] == "GAMEROOM")
                                            {
                                                object[] fullTag = (object[])message.SenderConnection.Tag;

                                                var room = (GameRoom)fullTag[1];
                                                room.hasLoaded = true;

                                                var sendMsg = Server.CreateMessage("HEADER");
                                                sendMsg.Write(Constants.MESSAGETYPE.OUTGAME);
                                                sendMsg.Write(Constants.OUTGAME.EVENT_ON_RECEIVEROOMINFO);

                                                sendMsg.Write(room.guid);
                                                var objSerialized = room.originalOption.Serialize();
                                                sendMsg.Write(objSerialized.Count());
                                                sendMsg.Write(objSerialized);
                                                sendMsg.Write(room.creatorName);
                                                sendMsg.Write((int)room.gameType);

                                                Server.SendMessage(sendMsg, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                                            }
                                        break;
                                    }
                                case NetConnectionStatus.Disconnected:
                                    {
                                        if (message.SenderConnection.Tag is object[] &&
                                            ((object[])message.SenderConnection.Tag)[0] is string)
                                            if ((string)((object[])message.SenderConnection.Tag)[0] == "GAMEROOM")
                                            {
                                                var gameRoom = (GameRoom)((object[])message.SenderConnection.Tag)[1];
                                                if (gameRoom != null)
                                                {
                                                    if (SystemMessage.rooms.Contains(gameRoom))
                                                        SystemMessage.rooms.Remove(gameRoom);
                                                }
                                            }

                                        break;
                                    }
                            }
                            break;

                        case NetIncomingMessageType.DebugMessage:
                            // handle debug messages
                            // (only received when compiled in DEBUG mode)
                            Console.WriteLine(message.ReadString());
                            break;

                        /* .. */
                        default:
                            Console.WriteLine("unhandled message with type: "
                                + message.MessageType);
                            break;
                    }
                }
            }
        }

        public static bool breakPoint = false;

        static bool OnProcessExit(CtrlTypes type)
        {
            Console.WriteLine("Terminate.");

            string userToReadNext = "";

            try
            {
                foreach (var user in SystemMessage.users.Values)
                {
                    try
                    {
                        var data = new DataUser()
                        {
                            user = user,
                            playerData = new Dictionary<int, PlayerData>()
                        };
                        foreach (var player in user.ownedPlayers)
                        {
                            if (player.Value == null)
                                continue;

                            data.playerData[player.Key] = new PlayerData(player.Key,
                                player.Value.MightyName,
                                player.Value.wasCreated,
                                data.user.login,
                                player.Value.EntitiesInPossession.ToArray(),
                                player.Value.Inventory,
                                player.Value.BasementLocation,
                                player.Value.LastMissionID,
                                player.Value.CurrentBigMissionID,
                                player.Value.CurrentBigMissionIndex
                            );
                        }
                        data.userpwd = new UserPassword(user.login, user.password, user.email);
                        data.saves = user.SavesData;

                        File.WriteAllBytes("user_" + user.login + ".db", data.Serialize());

                        userToReadNext += "user_" + user.login + ".db\n";
                    }
                    catch (Exception ex)
                    {
                        File.WriteAllText("erroruser_" + user.login + "-" + DateTime.Now.Ticks + ".txt", ex.Message + "\n\nSTACK:\n" + ex.StackTrace);
                    }
                }

                File.WriteAllText("main.db", userToReadNext);
            }
            catch(Exception ex)
            {
                File.WriteAllText("error-" + DateTime.Now.Ticks + ".txt", ex.Message + "\n\nSTACK:\n" + ex.StackTrace);
            }

            Environment.Exit(0);

            return true;
        }

        static void VerifyUsers()
        {
            if (File.Exists("main.db"))
            {
                foreach (var filePath in File.ReadAllLines("main.db"))
                {
                    var data = File.ReadAllBytes(filePath).Deserialize<DataUser>();

                    GameUser __user;
                    __user = SystemMessage.users[data.user.login] = data.user;
                    if (__user != null)
                    {
                        foreach (var _playerData in data.playerData.Values)
                        {
                            create:
                            if (SystemMessage.users.TryGetValue(_playerData.UserLogin, out var user))
                            {
                                user.ownedPlayers[_playerData.Index] = new GamePlayer()
                                {
                                    MightyName = _playerData.MightyName,
                                    User = user,
                                    wasCreated = _playerData.WasCreated,
                                    EntitiesInPossession = _playerData.EntitiesInPossession.ToList(),
                                    Inventory = _playerData.Inventory,
                                    BasementLocation = _playerData.BasementLocation,
                                    LastMissionID = _playerData.LastMissionID,
                                    CurrentBigMissionID = _playerData.CurrentBigMissionID,
                                    CurrentBigMissionIndex = _playerData.CurrentBigMissionIndex
                                };
                            }
                            else
                            {
                                SystemMessage.users.Add(_playerData.UserLogin, new GameUser()
                                {
                                    login = _playerData.UserLogin,
                                    currConnectionId = "unknow",
                                    guid = Guid.NewGuid().ToString(),
                                    currentPlayerIndex = 0,
                                    email = "unknow",
                                    password = "unknow",
                                });

                                goto create;
                            }
                        }
                        __user.password = data.userpwd.password;
                        __user.email = data.userpwd.email;
                        __user.SavesData = data.saves;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            foreach (var user in SystemMessage.users)
            {
                foreach (var player in user.Value.ownedPlayers)
                    player.Value.User = user.Value;
            }
        }

        static void updateCommand()
        {
            while (true)
            {
                string command = Console.ReadLine();
                if (command != null)
                {
                    if (command == "breakpoint")
                    {
                        breakPoint = true;
                        SystemMessage.Receive(null, null);
                    }
                    if (command == "user")
                    {
                        string toPrint = ". ... --- {==== USERS ====} --- ... .\n";
                        foreach (var user in SystemMessage.users.Values)
                        {
                            toPrint += $"--- \t{user.login} ---\n";
                            toPrint += $"nick:\t{user.nickname}\n";
                            toPrint += $"guid:\t{user.guid}\n";
                            toPrint += $"email:\t{user.email}\n";
                            toPrint += $"pass:\t{user.password}\n";
                            toPrint += $"conID:\t{user.currConnectionId}\n";
                        }
                        Console.WriteLine(toPrint);
                    }
                    if (command == "connection")
                    {
                        string toPrint = ". ... --- {==== CONNECTION ====} --- ... .\n";
                        foreach (var user in Server.Connections)
                        {
                            toPrint += $"--- \t{user.RemoteEndPoint.ToString()} ---\n";
                        }
                        Console.WriteLine(toPrint);
                    }
                    if (command.StartsWith("createroom"))
                    {
                        int numberOfRoom = 1;
                        if (int.TryParse(command.Replace("createroom", "").Replace(" ", ""), out numberOfRoom)) ;

                        for (int i = 0; i < numberOfRoom; i++)
                        {
                            var room = GameRoom.Create(new GameRoomOption()
                            {
                                description = "hello",
                            });

                            room.guid = Guid.NewGuid().ToString();

                            room.creatorName = "kek";

                            room.ipAdress = Dns.GetHostAddresses("127.0.0.1").First().ToString();
                            room.port = RoomManager.AvailablePort();

                            SystemMessage.rooms.Add(room);
                            RoomManager.AddRoom(room, room.port);
                        }
                    }
                }
            }
        }

        #endregion Private Methods

        #region unmanaged
        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion
    }

}
