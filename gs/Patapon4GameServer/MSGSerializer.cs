using MsgPack.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZeroFormatter;

namespace Patapon4GameServer.MSGSerializer
{
    public static class MSGSerializer
    {
        static Dictionary<Type, MessagePackSerializer> serializers = new Dictionary<Type, MessagePackSerializer>();

        public static byte[] Serialize<T>(this T thisObj)
            => ZeroFormatterSerializer.Serialize(thisObj);

        public static byte[] SerializeCircular<T>(this T thisObj)
        {
            var serializer = MessagePackSerializer.Get<T>();

            using (var byteStream = new MemoryStream())
            {
                serializer.Pack(byteStream, thisObj);
                return byteStream.ToArray();
            }
        }

        public static T Deserialize<T>(this byte[] bytes)
            => ZeroFormatterSerializer.Deserialize<T>(bytes);

        public static T DeserializeCircular<T>(this byte[] bytes)
        {
            var serializer = MessagePackSerializer.Get<T>();
            using (var byteStream = new MemoryStream(bytes))
            {
                return serializer.Unpack(byteStream);
            }
        }
    }
}