using Lidgren.Network;
using PataponPhotonShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer
{
    public static class UserExtension
    {
        internal static Dictionary<GameUser, NetConnection> userConnections = new Dictionary<GameUser, NetConnection>();

        public static void AddNetConnection(this GameUser user, NetConnection con)
        {
            Console.WriteLine(user == null);
            userConnections[user] = con;
        }

        public static GameUser IfInitiliazed(this GameUser user)
        {
            if (userConnections.ContainsKey(user))
                return user;
            return null;
        }

        public static NetConnection GetNetConnection(this GameUser user)
            => userConnections[user];

        public static bool GameUserExist(this NetConnection con)
            => userConnections.Where(o => o.Value == con).Count() > 0;

        public static GameUser GetGameUser(this NetConnection con)
            => userConnections.Where(o => o.Value == con)?.FirstOrDefault().Key;
    }
}
