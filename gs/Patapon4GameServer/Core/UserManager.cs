using PataponPhotonShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GameServer.Core
{
    public class UserManager
    {
        public static GameUser Take(string userName)
        {
            GameUser user = null;
            if (GameServer.gameServer.usersInRoom.TryGetValue(userName, out user))
                return user;
            return GameServer.gameServer.usersInRoom[userName] = new GameUser() { currConnectionId = "NOT_INITIED" };
        }
    }
}
