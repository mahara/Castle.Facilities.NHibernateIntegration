#pragma warning disable SYSLIB0011 // Type or member is obsolete
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace NHibernate.Util
{
    public sealed class LegacyBinaryFormatterSerializationStrategy : ISerializationStrategy
    {
        public byte[] Serialize(object value)
        {
            var formatter = CreateFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, value);
            return stream.ToArray();
        }

        public object Deserialize(byte[] data)
        {
            var formatter = CreateFormatter();
            using var stream = new MemoryStream(data);
            return formatter.Deserialize(stream);
        }

        private static BinaryFormatter CreateFormatter()
        {
            return new BinaryFormatter
            {
#if !NETFRAMEWORK
                SurrogateSelector = new SerializationHelper.SurrogateSelector(),
#endif
            };
        }
    }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure
#pragma warning restore SYSLIB0011 // Type or member is obsolete
