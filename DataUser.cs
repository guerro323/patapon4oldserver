using PataponPhotonShared;
using PataponPhotonShared.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace Patapon4GlobalServer
{
    [ZeroFormattable]
    public class DataUser
    {
        [Index(0)]
        public virtual GameUser user { get; set; }
        [Index(1)]
        public virtual Dictionary<int, PlayerData> playerData { get; set; }
        [Index(2)]
        public virtual UserPassword userpwd { get; set; }
        [Index(3)]
        public virtual Dictionary<int, SaveData> saves { get; set; }
    }
}
