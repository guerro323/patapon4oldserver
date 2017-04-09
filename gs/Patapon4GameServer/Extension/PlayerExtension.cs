using Lidgren.Network;
using Patapon4GameServer.MSGSerializer;
using Patapon4GameServer.Play;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityActor;

namespace Patapon4GameServer
{


    public static class PlayerExtension
    {
        public static Dictionary<GamePlayer, PlayerManager> Managers = new Dictionary<GamePlayer, PlayerManager>();

        public static List<EntityBaseActor> GetOwnedActors(this GamePlayer player)
        {
            return ActorSystem.AllActors.Where(actor =>
                actor.GetType() == typeof(EntityBaseActor)
                && ((EntityBaseActor)actor).Owner == player
            ).Cast<EntityBaseActor>().ToList();
        }

        public static PlayerManager GetManager(this GamePlayer player)
        {

            return Managers.ContainsKey(player) ? Managers[player] : null;
        }

        public static PlayerManager SetManager(this GamePlayer player)
        {
            return Managers[player] = new PlayerManager()
            {
                Player = player
            };
        }

        public static void Write(this NetOutgoingMessage msg, GamePlayer player)
        {
            msg.Write(player.MightyName);
            msg.Write(player.isBot);
            msg.Write(player.HubId);
            var objSerialized = player.User.Serialize();
            msg.Write(objSerialized.Length);
            msg.Write(objSerialized);
        }

        public static GamePlayer ReplacePlayer(this NetOutgoingMessage msg, GamePlayer player)
        {
            if (player == null)
                player = new GamePlayer();

            player.MightyName = msg.ReadString();
            player.isBot = msg.ReadBoolean();
            player.HubId = msg.ReadInt32();
            player.User = msg.ReadBytes(msg.ReadInt32()).Deserialize<GameUser>();

            return player;
        }
    }
}
