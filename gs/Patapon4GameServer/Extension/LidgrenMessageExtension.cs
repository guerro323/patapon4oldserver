using Lidgren.Network;
using Patapon4GameServer.MSGSerializer;
using PataponPhotonShared.Entity;
using PataponPhotonShared.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer.Extension
{
    public static class LidgrenMessageExtension
    {
        public static void Write(this NetOutgoingMessage msg, ClassDataInfo info)
        {
            msg.Write(info.CurrentEXP);
            msg.Write(info.EquipmentFromPlayer.Count);
            foreach (var kvp in info.EquipmentFromPlayer)
            {
                msg.Write((int)kvp.Key);
                msg.Write(kvp.Value);
            }
        }

        public static ClassDataInfo ReadClassDataInfo(this NetIncomingMessage msg)
        {
            var info = new ClassDataInfo();
            info.CurrentEXP = msg.ReadInt32();
            info.EquipmentFromPlayer = new Dictionary<ItemData.EType, int>();
            var dicoLength = msg.ReadInt32();
            for (int i = 0; i < dicoLength; i++)
            {
                info.EquipmentFromPlayer[(ItemData.EType)msg.ReadInt32()] = msg.ReadInt32();
            }
            return info;
        }
    }
}
