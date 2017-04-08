using ZeroFormatter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MsgPack;
using MsgPack.Serialization;

namespace Patapon4GlobalServer
{
    public static class MSGSerializer
    {
        static Dictionary<Type, MessagePackSerializer> serializers = new Dictionary<Type, MessagePackSerializer>();

        public static byte[] Serialize<T>(this T thisObj)
            => ZeroFormatterSerializer.Serialize(thisObj);

        public static byte[] SerializeCircular<T>(this T thisObj)
        {
            serializers[typeof(T)] = 
                (!serializers.ContainsKey(typeof(T)) || serializers[typeof(T)] == null) 
                ? SerializationContext.Default.GetSerializer<T>() 
                : serializers[typeof(T)];
            return serializers[typeof(T)].PackSingleObject(thisObj);
        }

        public static T Deserialize<T>(this byte[] bytes)
            => ZeroFormatterSerializer.Deserialize<T>(bytes);

        public static T DeserializeCircular<T>(this byte[] bytes)
        {
            var serializer = SerializationContext.Default.GetSerializer<T>();
            return serializer.UnpackSingleObject(bytes);
        }
    }
}
