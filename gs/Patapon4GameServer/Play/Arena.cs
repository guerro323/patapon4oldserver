using Lidgren.Network;
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
using FarseerPhysics;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;

namespace Patapon4GameServer.Play
{
    public class Arena
    {
        public enum ArenaMode
        {
            NormalRace = 000,
            BigMission = 010,
            Dungeon = 020,
            VersusHeadToHead = 050,
            VersusRace = 060,
            VersusMissile = 070,
            VersusHellGate = 090,
        }

        public enum StartEventType
        {
            Loaded,
            EveryoneIn,
            SyncFinished
        }

        public ActorSystem tributeSystem;
        public ActorSystem entitySystem;

        /// <summary>
        /// If the entities are compiled on the server
        /// </summary>
        public bool EntitiesCompiled;
        /// <summary>
        /// If the entities are spawned and compiled
        /// </summary>
        public bool EntitiesReady;
        /// <summary>
        /// If all clients already spawned their entities (and compiled them)
        /// </summary>
        public bool EntitiesClientSynced =>
            GameServer.gameServer.playersInRoom.Select(p => p.Value.GetManager()).Where(m => m.Net_AllEntitiesSynced).Count() <= 0;

        public World World;

        protected virtual ArenaMode s__Mode { get; }

        public Arena()
        {
            Singleton = this;
            entitySystem = new ActorSystem("entitySystem")
            {
                Options = new ActorSystemOptions()
                {
                    GameEntityCanReplaceThemselve = true,
                    MultipleSameIdAllowed = true,
                    ThrowErrorOnMultiComponentsGameObject = false
                }
            };
            tributeSystem = new ActorSystem("tributeSystem")
            {
                Options = new ActorSystemOptions()
                {
                    GameEntityCanReplaceThemselve = false,
                    MultipleSameIdAllowed = false,
                    ThrowErrorOnMultiComponentsGameObject = false
                }
            };
        }

        public virtual void StartEvent(StartEventType e)
        {

        }

        public virtual void Loop()
        {

        }

        public virtual void OnEvent(string eventName, object[] oParams) { }

        public virtual void ReceiveMessage(NetIncomingMessage message, string eventname)
        { }

        public virtual async Task SyncPlayers()
        { return; }

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
            string baseTypeFullName = typeof(EntityBaseActor).FullName;
            if (typeof(T) == typeof(TributeArmyActor))
                baseTypeFullName = typeof(TributeArmyActor).FullName;
            else if (typeof(T) == typeof(TributeBaseActor))
                baseTypeFullName = typeof(TributeBaseActor).FullName;
            else if (typeof(T) == typeof(WorldActor))
                baseTypeFullName = typeof(WorldActor).FullName;

            if (actor.Guid == null && generateGUID)
            {
                actor.Guid = ArenaExt.FindFreeGUID(actor.actorSystem);
            }

            actor.OwnerLogin = owner.User?.login ?? string.Empty;

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.Helper.CreateActor);
            sendMsg.Write(owner.User.login);
            sendMsg.Write(baseTypeFullName);
            sendMsg.Write(actor.Guid);
            sendMsg.Write(true);
            sendMsg.Write(eventOperation);

            return sendMsg;
        }

        public static Lidgren.Network.NetOutgoingMessage CreateMessage(string eventName)
        {
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.INGAME);
            sendMsg.Write(Constants.INGAME.SendUniGameMessage);
            sendMsg.Write(true);
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

        public static Arena Singleton { get; protected set; }

        /// <summary>
        /// Is the game accessible on multiplayer?
        /// </summary>
        public static bool Multiplayer;
        public static GUIInterface Interface;
        public static ArenaMode Mode;
    }

    public struct GUIInterface
    {
        public enum GUIType
        {
            Normal = 0,
            Versus = 1,
        }

        public enum GUIMode
        {
            NormalRace = 000,
            BigMission = 010,
            Dungeon = 020,
            VersusHeadToHead = 050,
            VersusRace = 060,
            VersusMissile = 070,
            VersusHellGate = 090,
        }

        public GUIType Type;
        public GUIMode Mode;
    }
}
