using Lidgren.Network;
using Patapon4GameServer.Core.Network;
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

namespace Patapon4GameServer.Core
{
    public class InGameSystem
    {

        public static void Receive(NetIncomingMessage message, string type)
        {
            if (type == Constants.INGAME.SetMapToPlay)
            {
                var isDefault = message.ReadBoolean();
                var mapPath = message.ReadString().ToLower();

                if (!string.IsNullOrEmpty(mapPath))
                {
                    if (isDefault)
                        MissionManager.MissionToPlay = MissionManager.DefaultMapsInfo[mapPath];
                    else
                        throw new NotImplementedException();

                    Console.WriteLine("Next map:" + MissionManager.MissionToPlay.Id);
                }
                else
                    Console.WriteLine("No next map.");

                var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
                sendMsg.Write(Constants.INGAME.SetMapToPlay);
                sendMsg.Write(isDefault);
                if (isDefault)
                    sendMsg.Write(mapPath);
                else
                {
                    // TODO: send serialized info;
                }

                GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), NetDeliveryMethod.ReliableOrdered, 0);
            }

            if (type == Constants.INGAME.ForceMapLoad)
            {
                var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
                sendMsg.Write(Constants.INGAME.ForceMapLoad);

                GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), NetDeliveryMethod.ReliableOrdered, 0);

                MissionManager.ForceLoad();
            }
            if (type == Constants.INGAME.Helper.SendNewStatus)
            {
                GameUser user = null;
                if ((user = message.SenderConnection.GetGameUser()) != null)
                {
                    var status = message.ReadByte();
                    // Change 'Net_CurrentSyncState' (0)
                    if (status == 0)
                    {
                        var state = (EGameState)message.ReadByte();
                        var player = user.ownedPlayers[user.currentPlayerIndex];
                        player.GetManager().Net_CurrentSyncState = state;
                    }
                    // Change 'Net_AllEntitiesSynced' (1)
                    else if (status == 1)
                    {
                        var state = message.ReadBoolean();
                        var player = user.ownedPlayers[user.currentPlayerIndex];
                        player.GetManager().Net_AllEntitiesSynced = state;
                    }
                }
            }
            if (type == Constants.INGAME.SendUniGameMessage)
            {
                var forVSCall = message.ReadBoolean();
                var eventName = message.ReadString();

                if (forVSCall)
                    MissionManager.MapMission.Arena.ReceiveMessage(message, eventName);
                else
                    MissionManager.MapMission.ReceiveMessage(message, eventName);
            }
            if (type == Constants.INGAME.PlayerSendCommand)
            {
                GameUser user = null;
                if ((user = message.SenderConnection.GetGameUser()) != null)
                {
                    var playerManager = user.ownedPlayers[user.currentPlayerIndex].GetManager();

                    var pressedKey = message.ReadByte();
                    var keyBeat = message.ReadInt16();
                    var timeStamp = message.ReadDouble();
                    if (message.ReadBoolean())
                    {
                        var key1 = message.ReadByte().ToString();
                        var key2 = message.ReadByte().ToString();
                        var key3 = message.ReadByte().ToString();
                        var key4 = message.ReadByte().ToString();
                        var key5 = message.ReadByte().ToString();

                        playerManager.CurrentCommand = $"_{key1}{key2}{key3}{key4}{(key5 != "0" ? key5 : string.Empty)}";
                    }
                }
            }
        }
    }
}
