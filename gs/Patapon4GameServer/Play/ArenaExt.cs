using Patapon4GameServer.Core.Network;
using PataponPhotonShared;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Enums;
using PataponPhotonShared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityActor;

namespace Patapon4GameServer.Play
{
    public partial class ArenaExt
    {
        public static void UISendMessage(IEnumerable<GamePlayer> receivers, string message, UIMessageAlignement alignement, float duration = 2f)
        {
            // Register for replay...
            // ...

            // Send message
            var sendMsg = GameServer.UsingPeer.CreateMessage().Start(Constants.INGAME.UISendMessage);
            sendMsg.Write((byte)alignement);
            sendMsg.Write(duration);
            sendMsg.Write(message);
            GameServer.UsingPeer.SendMessage(sendMsg,
                receivers.Where(p => p != null && !p.isBot).Select(p => p.User.GetNetConnection()).ToList(),
                Lidgren.Network.NetDeliveryMethod.ReliableOrdered,
                0);
        }

        /// <summary>
        /// Find a free GUID
        /// </summary>
        /// <param name="acSystem">If null, it will search in all systems</param>
        public static string FindFreeGUID(ActorSystem acSystem = null)
        {
            string guid = Guid.NewGuid().ToString();
            bool noDouble = false;
            while (!noDouble)
            {
                noDouble = true;
                if (acSystem == null)
                {
                    foreach (var entity in acSystem.Actors.values.Values)
                    {
                        if (entity.GetType().IsSubclassOf(typeof(WorldActor)))
                        {
                            if (((WorldActor)entity).Guid == guid)
                            {
                                while (((WorldActor)entity).Guid == guid)
                                    guid = Guid.NewGuid().ToString();
                                noDouble = true;
                            }
                            else
                                noDouble = true;
                        }
                    }
                }
                else
                {
                    foreach (var entity in acSystem.Actors.values.Values)
                    {
                        if (entity.GetType().IsSubclassOf(typeof(WorldActor)))
                        {
                            if (((WorldActor)entity).Guid == guid)
                            {
                                while (((WorldActor)entity).Guid == guid)
                                    guid = Guid.NewGuid().ToString();
                                noDouble = true;
                            }
                            else
                                noDouble = true;
                        }
                    }
                }
            }
            return guid;
        }
    }
}
