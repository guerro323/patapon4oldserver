using Lidgren.Network;
using PataponPhotonShared.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patapon4GlobalServer
{
    internal static class ConnectionExtension
    {
        public static Dictionary<NetConnection, int> netHandles = new Dictionary<NetConnection, int>();
        public static Dictionary<NetConnection, Hashtable> Hash = new Dictionary<NetConnection, Hashtable>();

        public static NetPeer GetPeer()
            => Program.Server;

        public static async Task<int> WaitNetHandle(this NetConnection con, int handle = -1)
        {
            if (!netHandles.ContainsKey(con))
                netHandles[con] = -1;

            var _handle = handle;
            if (_handle == -1)
            {
                netHandles.TryGetValue(con, out _handle);
                if (_handle == -1)
                {
                    netHandles[con] = 0;
                }
                netHandles[con]++;

                _handle = netHandles[con];
            }

            var sendMsg = GetPeer().CreateMessage("HEADER");
            sendMsg.Write(Constants.MESSAGETYPE.SYSTEM);
            sendMsg.Write(Constants.SYSTEM.HANDLE);
            sendMsg.Write(_handle);

            GetPeer().SendMessage(sendMsg, con, NetDeliveryMethod.ReliableOrdered);

            await Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    if (!Hash.ContainsKey(con))
                        Hash[con] = new Hashtable();

                    var tag = Hash[con];
                    {
                        if (!tag.ContainsKey("handle"))
                            tag["handle"] = new ArrayList();
 
                        var handleList = (ArrayList)tag["handle"];
                        if (handleList.Contains(_handle))
                        {
                            handleList.Remove(_handle);
                            break;
                        }
                        
                    }
                }
            });

            return _handle;
        }
    }
}
