using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PataponPhotonShared.MessageExtension;

namespace Patapon4GameServer.Core.Network
{
	public static class Message
	{
        public static Dictionary<long, KeyValuePair<NetOutgoingMessage, Action<NetIncomingMessage>>> _handlersAction
            = new Dictionary<long, KeyValuePair<NetOutgoingMessage, Action<NetIncomingMessage>>>();

		#region Public Methods

        public static void TriggerHandler(long handle__, NetIncomingMessage msg__)
        {
            KeyValuePair<NetOutgoingMessage, Action<NetIncomingMessage>> actionkvp;
            var exist = _handlersAction.TryGetValue(handle__, out actionkvp);
            if (exist)
            {
                _handlersAction[handle__].Value(msg__);
                _handlersAction.Remove(handle__);
            }
        }


        public static PataMessage<T> Read<T>(this NetIncomingMessage msg)
		{
			string header = "";
			if (msg.ReadString(out header) && header == "HEADER")
			{
				string type = msg.ReadString();
				return new PataMessage<T>("HEADER", new PataponPhotonShared.HeaderContent<T>(type, default(T)));
			}
			/*else
                Debug.LogError(header + " :Can't read message if it don't contains any header!");*/
			return null;
		}

		public static NetOutgoingMessage Start(this NetOutgoingMessage msg, string type)
		{
            /*long handle__ = _handlersAction.Count;
            _handlersAction[handle__] = new KeyValuePair<NetOutgoingMessage, Action<NetIncomingMessage>>(
                msg, null);

            msg.Write((byte)4); //< subscribe byte
            msg.Write(handle__);*/
			msg.Write("HEADER");
			msg.Write(type);

			return msg;
		}

        public static void Subscribe(this NetOutgoingMessage msg, Action<NetIncomingMessage> action)
        {
            var kvp = _handlersAction.Where(kvp__ => kvp__.Value.Key == msg)?.First();
            if (!kvp.HasValue)
                throw new Exception("KVP is null");

            _handlersAction[kvp.Value.Key] = new KeyValuePair<NetOutgoingMessage, Action<NetIncomingMessage>>(
                msg, action); 
        }

		#endregion Public Methods
	}
}
