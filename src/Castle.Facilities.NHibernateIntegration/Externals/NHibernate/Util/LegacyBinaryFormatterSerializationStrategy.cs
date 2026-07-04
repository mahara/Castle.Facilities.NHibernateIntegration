#if !NETFRAMEWORK
#pragma warning disable SYSLIB0011 // Type or member is obsolete
#endif
using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace NHibernate.Util
{
    /// <summary>
    ///     Legacy <see cref="ISerializationStrategy" /> implementation
    ///     backed by <see cref="BinaryFormatter" />.
    /// </summary>
    /// <remarks>
    ///     Restores binary serialization support on .NET 9.0 and later
    ///     where NHibernate defaults to <see cref="ThrowingSerializationStrategy" />.
    /// </remarks>
    public sealed class LegacyBinaryFormatterSerializationStrategy : ISerializationStrategy
    {
#if !NETFRAMEWORK
        private static readonly SerializationHelper.SurrogateSelector _surrogateSelector = new();
#endif

        [ThreadStatic]
        private static BinaryFormatter? _formatter;

        /// <summary>
        ///     Serializes the specified object graph into a byte array using <see cref="BinaryFormatter" />.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>
        ///     The serialized byte array, or an empty array if <paramref name="value" /> is <see langword="null" />.
        /// </returns>
        public byte[] Serialize(object value)
        {
            if (value is null)
            {
                return [];
            }

            var formatter = _formatter ??= CreateFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, value);
            return stream.ToArray();
        }

        /// <summary>
        ///     Deserializes an object graph from the specified byte array using <see cref="BinaryFormatter" />.
        /// </summary>
        /// <param name="data">The serialized byte array.</param>
        /// <returns>
        ///     The deserialized object, or <see langword="null" /> if <paramref name="data" /> is <see langword="null" /> or empty.
        /// </returns>
        public object Deserialize(byte[] data)
        {
            if (data is null || data.Length == 0)
            {
                return null!;
            }

            var formatter = _formatter ??= CreateFormatter();
            using var stream = new MemoryStream(data);
            return formatter.Deserialize(stream);
        }

        private static BinaryFormatter CreateFormatter()
        {
            return new BinaryFormatter
            {
#if !NETFRAMEWORK
                SurrogateSelector = _surrogateSelector,
#endif
            };
        }
    }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure
#if !NETFRAMEWORK
#pragma warning restore SYSLIB0011 // Type or member is obsolete
#endif
