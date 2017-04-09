using Lidgren.Network;
using Patapon4GameServer.Core;
using Patapon4GameServer.Core.Network;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityActor;

namespace Patapon4GameServer.Play
{
    public class MapMission
    {
        public float GroundPosY;
        public UnityEngine.Vector2 FinishLinePos;

        public GameServer Server;
        public GameMap Map;
        public Arena Arena;

        /// <summary>
        /// First start of the map, nobody are loaded, so build the map here
        /// </summary>
        public virtual async Task InitialStart()
        { return; }

        /// <summary>
        /// The map finished building, clients got their map loaded but they aren't synced yet.
        /// </summary>
        public virtual async Task Preparation()
        { return; }

        /// <summary>
        /// Final start == everyone loaded successfully and the map is starting.
        /// </summary>
        public virtual void FinalStart()
        { }

        public virtual void Update()
        { }

        public virtual void ReceiveMessage(NetIncomingMessage message, string eventname)
        { }

        public virtual void CompileAllEntities()
        {
            List<string> filesToCompile = new List<string>();
            foreach (EntityBaseActor entityActor in Arena.entitySystem.Actors.values.Values)
            {
                if (entityActor.script != "default")
                {
                    // TODO:
                }
                else
                {
                    filesToCompile.Add(Environment.CurrentDirectory + "/" + MissionManager.DefaultEntitiyAssets[entityActor.Data.CurrentClass.ToLower()]
                        .Dependencies.Values.Where(d => d.type == "entity_server").First().path);
                }
            }

            CSScriptLibrary.CSScript.CompileFiles(filesToCompile.ToArray());
            Arena.EntitiesCompiled = true;
        }

        #region Helper (En ce qui concerne les trucs du début)
        public static void SetGroundPos(float y)
        {
            MissionManager.MapMission.GroundPosY = y;

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.SetGroundPos);
            sendMsg.Write(y);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void SetCameraPos(float x, float y)
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.SetCameraPos);
            sendMsg.Write(x);
            sendMsg.Write(y);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);
        }

        public static void SetFinishLinePos(float x, float y)
        {
            MissionManager.MapMission.FinishLinePos = new UnityEngine.Vector2(x, y);

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.SetFinishLinePos);
            sendMsg.Write(x);
            sendMsg.Write(y);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);
        }
        #endregion

        /// <summary>
        /// Communicate the actor to the client. Don't forget to send the message by yourself.
        /// </summary>
        /// <typeparam name="T">The type of the actor</typeparam>
        /// <param name="actor">The actor himself</param>
        /// <param name="owner">The owner of the actor</param>
        /// <param name="generateGUID">Automatically generate GUID</param>
        /// <returns>The message that need to be sent</returns>
        public static Lidgren.Network.NetOutgoingMessage CommunicateActorToClients<T>(T actor, GamePlayer owner = null, bool generateGUID = true, string eventOperation = "default")
            where T : WorldActor
        {
            if (actor.Guid == null && generateGUID)
            {
                actor.Guid = ArenaExt.FindFreeGUID(actor.actorSystem);
            }

            actor.OwnerLogin = owner.User?.login ?? string.Empty;

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.CreateActor);
            sendMsg.Write(owner.User.login);
            sendMsg.Write(typeof(T).FullName);
            sendMsg.Write(actor.Guid);
            sendMsg.Write(false);
            sendMsg.Write(eventOperation);

            return sendMsg;
        }

        public static Lidgren.Network.NetOutgoingMessage CreateMessage(string eventName)
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.SendUniGameMessage);
            sendMsg.Write(false);
            sendMsg.Write(eventName);

            return sendMsg;
        }

        public static void SendMessage(Lidgren.Network.NetOutgoingMessage msg, List<GamePlayer> players, Lidgren.Network.NetDeliveryMethod deliveryMethod = Lidgren.Network.NetDeliveryMethod.ReliableOrdered)
        {
            GameServer.UsingPeer.SendMessage(msg, players.Select(p => p.User.GetNetConnection()).ToList(), deliveryMethod, 0);
        }

        public static void SendMessageToAll(Lidgren.Network.NetOutgoingMessage msg, Lidgren.Network.NetDeliveryMethod deliveryMethod = Lidgren.Network.NetDeliveryMethod.ReliableOrdered)
        {
            GameServer.UsingPeer.SendMessage(msg, GameServer.GetUserConnections(), deliveryMethod, 0);
        }

        public static void MapBuildFinished()
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.MapBuildFinished);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
