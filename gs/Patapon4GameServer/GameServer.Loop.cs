using Patapon4GameServer.Core;
using Patapon4GameServer.Core.Network;
using Patapon4GameServer.Extension;
using Patapon4GameServer.MSGSerializer;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Enums;
using PataponPhotonShared.Helper;
using PataponPhotonShared.Schemes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityActor;

namespace Patapon4GameServer
{
    public partial class GameServer
    {
        public EGameState currentGameState;

        public ActorSystem entitySystem;

        public ArmyScheme WantedScheme;

        /// <summary>
        /// Les entités à placer lorsqu'il manque des joueurs. (K:HubID, V:ServerID)
        /// </summary>
        public Dictionary<int, int> ReplacementEntities = new Dictionary<int, int>();

        void CheckUserStatus()
        {
            try
            {
                foreach (var user in usersInRoom)
                {
                    if (user.Value.IfInitiliazed()?.GetNetConnection().Status == Lidgren.Network.NetConnectionStatus.Disconnected)
                    {
                        if (user.Value.login == Room.creatorName)
                        {
                            // Disband hideout
                            foreach (var other in usersInRoom)
                            {
                                other.Value.IfInitiliazed()?.GetNetConnection().Disconnect("SUCCESS_DISCONNECT");
                                if (other.Value.IfInitiliazed() == null)
                                {
                                    other.Value.GetNetConnection().Disconnect("SUCCESS_DISCONNECT");
                                }
                            }
                        }
                        usersInRoom.Remove(user.Key);
                        continue;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Check the HubId of each player and re-order if needed
        /// </summary>
        public void CheckPlayerOrder()
        {
            playersInRoom = playersInRoom
                .OrderBy(x => x.Value.User.UpTime)
                .OrderBy(x => x.Value != HostPlayer).ToDictionary(k => k.Key, v => v.Value);

            try
            {
                int index = 0;
                foreach (var pl in playersInRoom)
                {
                    pl.Value.HubId = index;
                    index++;
                }
            }
            catch { }
        }

        void CheckPlayerEntities()
        {
            // Je ne crée pas d'allocation pour le GC si il n y a pas besoin d'enlever des joueurs
            List<string> playersToRemove = null;
            foreach (var player in playersInRoom)
            {
                if (!usersInRoom.ContainsValue(player.Value.User))
                {
                    if (playersToRemove == null)
                        playersToRemove = new List<string>();

                    playersToRemove.Add(player.Key);

                    if (!player.Value.isBot)
                    {
                        var sendMsg = UsingPeer.CreateMessage("HEADER");
                        sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
                        sendMsg.Write(Constants.SYSTEM.SHARE_USERDISCONNECT_FROM_ROOM);
                        sendMsg.Write(player.Value.User.login);
                    }

                    player.Value.GetOwnedActors()
                        .ForEach(actor => actor.Destroy());
                    continue;
                }

                if (entitySystem.Actors.Exist("playerMain_main:" + player.Value.User.login))
                {

                }
                else
                {
                    EntityBaseActor actor = null;
                    (actor = entitySystem.CreateActor<EntityBaseActor>("playerMain_main:" + player.Value.User.login, true))
                        .SetActive(true);
                    actor.Owner = player.Value;
                }

                if (player.Value.GetManager() == null)
                    player.Value.SetManager();
            }

            if (playersToRemove != null)
                foreach (var playerLogin in playersToRemove)
                    playersInRoom.Remove(playerLogin);
        }

        void GameLoop()
        {
            CheckUserStatus();
            CheckPlayerOrder();
            CheckPlayerEntities();
            GameStateLoop();

            UpdateGameMap();
        }

        void UpdateGameMap()
        {
            /*if (currentGameState == EGameState.LoadingMission
                && MissionManager.MapMission.finishedBuilding)
            {
                currentGameState = EGameState.GameMissionSyncing;
            }*/
            if (MissionManager.MapMission != null)
            {
                if (MissionManager.MapMission.Arena != null)
                {
                    MissionManager.MapMission.Arena.Loop();

                    foreach (var entity in MissionManager.MapMission.Arena.entitySystem.Actors.values)
                    {
                        entity.Update();
                    }
                }
            }
        }

        void GameStateLoop()
        {
            switch (currentGameState)
            {
                case EGameState.Basement:
                    {
                        WantedScheme = PatapolisArmyScheme;
                        break;
                    }
            }

            ActorSystem.AllActors.ForEach(actor => actor.Update());

            SendCurrentStatus();
        }

        internal int lastUpdate = 0;
        void SendCurrentStatus()
        {
            if (playersInRoom == null || Room == null)
                return;

            if (Environment.TickCount > lastUpdate)
            {
                lastUpdate = Environment.TickCount + 1000;

                var sendMsg = UsingPeer.CreateMessage()
                    .Start(Constants.MESSAGETYPE.OUTGAME);
                sendMsg.Write(Constants.OUTGAME.UPDATE_GAMEINFO);
                sendMsg.Write((byte)currentGameState);
                sendMsg.Write(WantedScheme);
                sendMsg.Write((short)playersInRoom.Count);
                foreach (var player in playersInRoom)
                {
                    sendMsg.Write(player.Value);
                }

                if (GetUserConnections().Count > 0)
                    UsingPeer.SendMessage(sendMsg, GetUserConnections(), Lidgren.Network.NetDeliveryMethod.ReliableOrdered, 1);
            }
        }

        /// <summary>
        /// L'armée de base dans Patapolis en multijoueur
        /// </summary>
        public ArmyScheme PatapolisArmyScheme = new ArmyScheme("playermain")
        {
            subs = new[]
            {
                // On met UberHeroArmy car on aura le choix entre l'uberhero et le chef (= si place vide)
                new ArmyScheme("uberhero")
                {
                    leaderEntities = new EntityTypeScheme?[]
                    {
                        new EntityTypeScheme("uberhero", ActorConstants.ACScheme.TreeAllUberClasses),
                    },
                },
                new ArmyScheme("chef")
                {
                    leaderEntities = new EntityTypeScheme?[]
                    {
                        new EntityTypeScheme("chef", ActorConstants.ACScheme.TreeAllNormalUberClasses)
                    }
                },
                new ArmyScheme("unit")
                {
                    armyEntities = new EntityTypeScheme?[6]
                    {
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                        new EntityTypeScheme("unit", ActorConstants.ACScheme.TreeAllStandardClasses),
                    }
                },
            }
        };
    }

    public class Time
    {
        public static double deltaTime;
        public static double LastTime;
        public static TimeSpan StartTime;
        public static int TickElapsed;
        public static double TimeElapsed;
        public static System.Timers.Timer Timer;
        static internal void UpdateTimeDelta()
        {
            deltaTime = (TimeElapsed - LastTime) / 1000;
        }
    }
}