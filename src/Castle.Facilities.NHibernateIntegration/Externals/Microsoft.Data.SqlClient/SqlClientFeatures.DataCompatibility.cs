namespace Microsoft.Data.SqlClient
{
    public static partial class SqlClientFeatures
    {
        /// <summary>
        ///     Configures legacy data-type conversion and null-handling switches for <see cref="SqlClient" />.
        /// </summary>
        public static class DataCompatibility
        {
            internal const string LegacyRowVersionNullBehaviorSwitch = "Switch.Microsoft.Data.SqlClient.LegacyRowVersionNullBehavior";
            internal const string TruncateScaledDecimalSwitch = "Switch.Microsoft.Data.SqlClient.TruncateScaledDecimal";

            /// <summary>
            ///     Gets or sets a value indicating whether legacy null behavior is used for <c>rowversion</c> and <c>timestamp</c> columns.
            /// </summary>
            public static bool? LegacyRowVersionNullBehavior
            {
                get => GetSwitch(LegacyRowVersionNullBehaviorSwitch);
                set => SetSwitch(LegacyRowVersionNullBehaviorSwitch, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether <c>SqlDecimal</c> values are truncated rather than rounded when rescaled.
            /// </summary>
            public static bool? TruncateScaledDecimal
            {
                get => GetSwitch(TruncateScaledDecimalSwitch);
                set => SetSwitch(TruncateScaledDecimalSwitch, value);
            }
        }
    }
}
