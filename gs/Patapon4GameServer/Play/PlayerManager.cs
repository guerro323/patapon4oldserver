using PataponPhotonShared;
using PataponPhotonShared.Enums;
using PataponPhotonShared.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Patapon4GameServer.Core.Network;
using PataponPhotonShared.Helper;

namespace Patapon4GameServer.Play
{
    public class PlayerManager
    {
        public static Dictionary<GamePlayer, List<EntityData>> LoadoutOfAll;

        public GamePlayer Player { get; internal set; }
        public EGameState Net_CurrentSyncState { get; internal set; }
        public bool Net_AllEntitiesSynced { get; internal set; }

        public EntityUnitActor UberHeroActor;
        public Dictionary<int, Dictionary<int, EntityBaseActor>> ArmyActors;
        public TributeArmyActor Army;

        public string CurrentCommand;
        public int CommandStartBeat;

        /// <summary>
        /// Get the final loadout of all players (Entity, Weapons equipped, etc...).
        /// Used when the gameserver is in a mission.
        /// </summary>
        /// <returns></returns>
        public static async Task GetFinalLoadoutOfAll()
        {
            LoadoutOfAll = new Dictionary<GamePlayer, List<EntityData>>();
            foreach (var player in GameServer.gameServer.playersInRoom)
            {
                LoadoutOfAll[player.Value] = null;
            }

            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.MESSAGETYPE.OUTGAME);
            sendMsg.Write(Constants.OUTGAME.GET_FINALLOADOUTOFALL);

            GameServer.UsingPeer.SendMessage(sendMsg, GameServer.MasterConnection, Lidgren.Network.NetDeliveryMethod.ReliableOrdered);

            await Task.Factory.StartNew(() =>
            {
                bool finish = false;
                while (!finish)
                {
                    finish = true;
                    foreach (var player in GameServer.gameServer.playersInRoom)
                    {
                        if (LoadoutOfAll[player.Value] == null)
                            finish = false;
                    }
                }
            });
        }

        public void Loop()
        {

        }
    }
}
