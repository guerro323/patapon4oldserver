using Lidgren.Network;
using Patapon4GameServer.MSGSerializer;
using PataponPhotonShared.Schemes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer
{
    public static class SchemeExtension
    {
        public static void Write(this NetOutgoingMessage msg, ArmyScheme scheme)
        {
            byte[] objSerialized = new byte[0];

            // Verification and correction
            if (scheme.subs == null)
                scheme.subs = new ArmyScheme[0];

            // Start by group name
            msg.Write($"armystart:{scheme.groupName}");
            {
                // Write the leaders
                objSerialized = scheme.leaderEntities.Serialize();
                msg.Write(objSerialized.Length);
                msg.Write(objSerialized);

                // Write the army entities
                objSerialized = scheme.armyEntities.Serialize();
                msg.Write(objSerialized.Length);
                msg.Write(objSerialized);

                // Now write the subs
                msg.Write(scheme.subs.Length);
                for (int i = 0; i < scheme.subs.Length; i++)
                {
                    msg.Write(scheme.subs[i]);
                }
            }
        }

        public static ArmyScheme ReadArmyScheme(this NetIncomingMessage msg)
        {
            ArmyScheme current = new ArmyScheme();
            string startString = msg.ReadString();
            int dotIndex = startString.IndexOf(':');

            // Start by getting group name
            current.groupName = startString.Substring(dotIndex + 1, startString.Length - dotIndex - 1);
            {
                // Get the leaders
                current.leaderEntities = msg.ReadBytes(msg.ReadInt32()).Deserialize<EntityTypeScheme?[]>();

                // Get the army entities
                current.armyEntities = msg.ReadBytes(msg.ReadInt32()).Deserialize<EntityTypeScheme?[]>();

                // Now get the subs
                current.subs = new ArmyScheme[msg.ReadInt32()];
                for (int i = 0; i < current.subs.Length; i++)
                {
                    current.subs[i] = msg.ReadArmyScheme();
                }
            }

            return current;
        }
    }
}
