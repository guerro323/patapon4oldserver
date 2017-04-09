using Lidgren.Network;
using Patapon4GameServer.Core.Network;
using Patapon4GameServer.Extension;
using Patapon4GameServer.MSGSerializer;
using Patapon4GameServer.Play;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Enums;
using PataponPhotonShared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer.Core
{
	public class OutGameSystem
	{

		public static async void Receive(NetIncomingMessage message, string type)
		{
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Message type: " + type);
            Console.ResetColor();
			switch (type)
			{
                case Constants.OUTGAME.GO_PLAYERDISCONNECT:
                    {
                        var userList = GameServer.UsingPeer.Connections
                            .Where(con => con.Tag != null && con.Tag.ToString() == "USER" && con != message.SenderConnection).ToList();

                        var sendMsg = GameServer.UsingPeer.CreateMessage()
                            .Start(Constants.MESSAGETYPE.OUTGAME);
                        sendMsg.Write(Constants.OUTGAME.EVENT_ON_PLAYERDISCONNECT);
                        sendMsg.Write(message.SenderConnection.GetGameUser().login);
                        GameServer.UsingPeer.SendMessage(sendMsg, userList, NetDeliveryMethod.ReliableOrdered, 0);

                        message.SenderConnection.Disconnect("SUCCESS_DISCONNECT");
                        break;
                    }
				// Quelqu'un s'est co sur notre serveur!
				// Cet event à été trigger côté gameServer par le masterServer, mais il sera redistribué côté clients
				case Constants.OUTGAME.EVENT_ON_PLAYERCONNECT:
					{
                        GameServer.MasterConnection = message.SenderConnection;

                        GameUser tempUser = null;
                        tempUser = message.ReadBytes(message.ReadInt32()).Deserialize<GameUser>();
                        GamePlayer usingPlayer = null;
                        usingPlayer = message.ReadBytes(message.ReadInt32()).Deserialize<GamePlayer>();

                        var user = UserManager.Take(tempUser.login);
                        user.currConnectionId = tempUser.currConnectionId;
                        user.currentPlayerIndex = 0;
                        user.currentRoom = tempUser.currentRoom;
                        user.currentTeams = tempUser.currentTeams;
                        user.email = tempUser.email;
                        user.guid = tempUser.guid;
                        user.login = tempUser.login;
                        user.nickname = tempUser.nickname;
                        user.ownedPlayers = new Dictionary<int, GamePlayer>()
                        {
                            { 0, usingPlayer }
                        };
                        GameServer.Log($"User joined! {user.login}");

                        usingPlayer.User = user;

                        GameServer.gameServer.usersInRoom[user.login] = user;
                        GameServer.gameServer.playersInRoom[user.login] = usingPlayer;

                        GameServer.gameServer.CheckPlayerOrder();

                        var userList = GameServer.UsingPeer.Connections
                            .Where(con => con.Tag != null && con.Tag.ToString() == "USER").ToList();
                        if (userList.Count > 0)
                        {
                            var sendMsg = GameServer.UsingPeer.CreateMessage()
                                .Start(Constants.MESSAGETYPE.OUTGAME);
                            sendMsg.Write(Constants.OUTGAME.EVENT_ON_PLAYERCONNECT);
                            sendMsg.Write(usingPlayer);

                            GameServer.UsingPeer.SendMessage(sendMsg, userList,
                                NetDeliveryMethod.ReliableOrdered, 0);
                        }

                        if (GameServer.gameServer.waitedUsers.ContainsKey(user.login))
                        {
                            GameServer.Log("(Connection) Approving waited connection: " + user.login);
                            GameServer.gameServer.waitedUsers[user.login].Approve();
                            user.AddNetConnection(GameServer.gameServer.waitedUsers[user.login]);
                            GameServer.gameServer.waitedUsers.Remove(user.login);
                        }

                        if (user.login == GameServer.Room.creatorName)
                        {
                            GameServer.HostUser = user;
                            GameServer.HostPlayer = usingPlayer;

                            GameServer.gameServer.ReplacementEntities.Add(3, 1);
                        }

                        GameServer.gameServer.lastUpdate = -1;

                        break;
					}
                case Constants.OUTGAME.EVENT_ON_RECEIVEROOMINFO:
                    {
                        GameServer.Room = new GameRoom();
                        GameServer.Room.guid = message.ReadString();
                        GameServer.Room.originalOption = message.ReadBytes(message.ReadInt32()).Deserialize<GameRoomOption>();
                        GameServer.Room.creatorName = message.ReadString();
                        GameServer.Room.gameType = (GameType)message.ReadInt32();

                        Console.WriteLine($@"ROOM INFO RECEIVED! 
{GameServer.Room.guid}
{GameServer.Room.creatorName}
{GameServer.Room.gameType}");
                        break;
                    }
                case Constants.OUTGAME.EVENT_ON_UPDATEONLINEENTITY:
                    {
                        var userLogin = message.ReadString();
                        var entityData = message.ReadBytes(message.ReadInt32()).Deserialize<EntityData>();

                        string debugMessage = Time.TimeElapsed + "Entity update provided by the server!";

                        await Task.Factory.StartNew(() =>
                        {
                            while (!GameServer.gameServer.usersInRoom.ContainsKey(userLogin)) ;
                            while (GameServer.gameServer.usersInRoom[userLogin].ownedPlayers.Count == 0) ;
                            while (GameServer.gameServer.waitedUsers.ContainsKey(userLogin)) ;
                        });

                        if (GameServer.gameServer.usersInRoom.TryGetValue(userLogin, out var user))
                        {
                            var player = user.ownedPlayers[0];
                            EntityData oldEntity = null;
                            if (player.EntitiesInPossession == null)
                                player.EntitiesInPossession = new List<EntityData>();
                            if (player.EntitiesInPossession.Exists(e => (oldEntity = e).ServerID == entityData.ServerID))
                            {
                                debugMessage += "\tUpdate";

                                // Update
                                oldEntity.ArmyIndex = entityData.ArmyIndex;
                                oldEntity.ClassesInPossession = entityData.ClassesInPossession;
                                oldEntity.CurrentClass = entityData.CurrentClass;
                                oldEntity.CurrentRarepon = entityData.CurrentRarepon;
                                oldEntity.RareponsInPossession = entityData.RareponsInPossession;
                                oldEntity.SchemeID = entityData.SchemeID;
                                oldEntity.ServerID = entityData.ServerID;
                                oldEntity.Type = entityData.Type;
                            }
                            else
                            {
                                debugMessage += "\tCreation";

                                // Create
                                player.EntitiesInPossession.Add(entityData);
                            }
                        }

                        debugMessage += $@"Struct:
ArmyIndex: {entityData.ArmyIndex}
ClassesInPossession Count: {(entityData.ClassesInPossession == null ? 0 : entityData.ClassesInPossession.Count)}
CurrentClass: {entityData.CurrentClass}
RareponsInPossession Count: {(entityData.RareponsInPossession == null ? 0 : entityData.RareponsInPossession.Count)}
CurrentRarepon: {entityData.CurrentRarepon}
SchemeID: {entityData.SchemeID}
ServerID: {entityData.ServerID}
Type: {entityData.Type}
";

                        GameServer.Log(debugMessage);

                        SendNewEntityToAll(userLogin, entityData);
                        await Task.Delay(100);
                        await Task.Factory.StartNew(() => { while (GameServer.GetUserConnections().Count == 0) ; } );
                        SendNewArmy();

                        break;
                    }
                case Constants.OUTGAME.EVENT_ON_RECEIVEFINALLOADOUT:
                    {   
                        var playerLogin = message.ReadString();
                        GameServer.Log("Receving loadout of " + playerLogin);
                        if (GameServer.gameServer.playersInRoom.ContainsKey(playerLogin))
                        {
                            var entityList = message.ReadBytes(message.ReadInt32()).Deserialize<EntityData[]>();
                            PlayerManager.LoadoutOfAll[GameServer.gameServer.playersInRoom[playerLogin]] = entityList.ToList();
                        }

                        break;
                    }

            }
		}

        public static void SendNewArmy()
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.OUTGAME);
            sendMsg.Write(Constants.OUTGAME.EVENT_ON_UPDATEARMY);

            // Write current army
            int index = 0;
            sendMsg.Write((byte)GameServer.gameServer.playersInRoom.Count);
            GameServer.Log("will send...");
            foreach (var player in GameServer.gameServer.playersInRoom)
            {
                GameServer.Log("ub of " + player.Value.User.login);
                sendMsg.Write((byte)player.Value.HubId);
                sendMsg.Write(player.Key);
                sendMsg.Write(0);
                index++;
            }
            sendMsg.Write(GameServer.gameServer.playersInRoom.Count < GameServer.Room.maxPlayers && GameServer.gameServer.ReplacementEntities.Count > 0);
            if (GameServer.gameServer.playersInRoom.Count < GameServer.Room.maxPlayers && GameServer.gameServer.ReplacementEntities.Count > 0)
            {
                // Get entities from host
                sendMsg.Write((byte)GameServer.gameServer.ReplacementEntities.Where(e => e.Key >= index).Count());
                foreach (var repEntity in GameServer.gameServer.ReplacementEntities)
                {
                    if (repEntity.Key >= index)
                    {
                        GameServer.Log("unit(" + repEntity.Key + " | " + repEntity.Value + ") of " + GameServer.HostPlayer.User.login);
                        sendMsg.Write((byte)repEntity.Key);
                        sendMsg.Write(GameServer.HostPlayer.User.login);
                        sendMsg.Write(repEntity.Value);
                    }
                }
            }

            GameServer.Log("STOP SEND ARMY");

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void SendNewEntityTo(GameUser sendTo, string userlogin, EntityData entityData)
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.OUTGAME);
            sendMsg.Write(Constants.OUTGAME.EVENT_ON_NETRECEIVEENTITYDATA);
            sendMsg.Write(userlogin);
            var objSerialized = entityData.Serialize();
            {
                sendMsg.Write(objSerialized.Length);
                sendMsg.Write(objSerialized);
            }

            sendMsg.Write(entityData.ClassesInPossession.Count);
            foreach (var c in entityData.ClassesInPossession)
            {
                sendMsg.Write(c.Key);
                sendMsg.Write(c.Value);
            }

            GameServer.UsingPeer.SendMessage(sendMsg, sendTo.GetNetConnection(), NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void SendNewEntityToAll(string userlogin, EntityData entityData)
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.OUTGAME);
            sendMsg.Write(Constants.OUTGAME.EVENT_ON_NETRECEIVEENTITYDATA);
            sendMsg.Write(userlogin);
            var objSerialized = entityData.Serialize();
            {
                sendMsg.Write(objSerialized.Length);
                sendMsg.Write(objSerialized);
            }

            sendMsg.Write(entityData.ClassesInPossession.Count);
            foreach (var c in entityData.ClassesInPossession)
            {
                sendMsg.Write(c.Key);
                sendMsg.Write(c.Value);
            }

            if (GameServer.GetUserConnections().Count > 0)
                GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), NetDeliveryMethod.ReliableOrdered, 0);
        }
	}
}
