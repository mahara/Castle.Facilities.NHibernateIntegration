using NHibernate.Util;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace NHibernate
{
    public static class NHibernateFeatures
    {
        private static readonly ISerializationStrategy _defaultSerializationStrategy = SerializationConfiguration.Strategy;

        private static readonly ISerializationStrategy _legacyBinaryFormatterSerializationStrategy = new LegacyBinaryFormatterSerializationStrategy();

        public static void EnableBinaryFormatterSerialization()
        {
            if (SerializationConfiguration.Strategy is not LegacyBinaryFormatterSerializationStrategy)
            {
                SerializationConfiguration.Strategy = _legacyBinaryFormatterSerializationStrategy;
            }
        }

        public static void DisableBinaryFormatterSerialization()
        {
            SerializationConfiguration.Strategy = _defaultSerializationStrategy;
        }
    }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure
