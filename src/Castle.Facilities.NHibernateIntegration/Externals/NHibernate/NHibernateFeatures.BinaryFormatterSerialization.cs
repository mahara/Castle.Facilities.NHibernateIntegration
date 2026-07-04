using NHibernate.Util;

namespace NHibernate
{
    public static partial class NHibernateFeatures
    {
        /// <summary>
        ///     Manages opt-in <see cref="LegacyBinaryFormatterSerializationStrategy" /> activation
        ///     for NHibernate serialization.
        /// </summary>
        /// <remarks>
        ///     REFERENCES:
        ///     -   <see href="https://github.com/nhibernate/nhibernate-core/pull/3745" />
        /// </remarks>
        public static class BinaryFormatterSerialization
        {
            private static readonly ISerializationStrategy _defaultSerializationStrategy =
                SerializationConfiguration.Strategy;

            private static readonly ISerializationStrategy _legacyBinaryFormatterSerializationStrategy =
                new LegacyBinaryFormatterSerializationStrategy();

            /// <summary>
            ///     Configures <see cref="SerializationConfiguration.Strategy" />
            ///     to use <see cref="LegacyBinaryFormatterSerializationStrategy" />.
            /// </summary>
            public static void Enable()
            {
                if (SerializationConfiguration.Strategy is not LegacyBinaryFormatterSerializationStrategy)
                {
                    SerializationConfiguration.Strategy = _legacyBinaryFormatterSerializationStrategy;
                }
            }

            /// <summary>
            ///     Restores <see cref="SerializationConfiguration.Strategy" />
            ///     to its initial strategy captured prior to enabling.
            /// </summary>
            public static void Reset()
            {
                SerializationConfiguration.Strategy = _defaultSerializationStrategy;
            }
        }
    }
}
