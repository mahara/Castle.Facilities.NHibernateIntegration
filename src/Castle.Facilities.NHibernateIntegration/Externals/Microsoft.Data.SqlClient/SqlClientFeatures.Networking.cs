namespace Microsoft.Data.SqlClient
{
    public static partial class SqlClientFeatures
    {
        /// <summary>
        ///     Configures transport, SNI, and login-timeout switches for <see cref="SqlClient" />.
        /// </summary>
        public static class Networking
        {
            internal const string UseManagedNetworkingOnWindowsSwitch = "Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows";
            internal const string DisableTnirByDefaultSwitch = "Switch.Microsoft.Data.SqlClient.DisableTNIRByDefault";
            internal const string SuppressInsecureTlsWarningSwitch = "Switch.Microsoft.Data.SqlClient.SuppressInsecureTLSWarning";
            internal const string UseOneSecFloorInTimeoutCalculationDuringLoginSwitch = "Switch.Microsoft.Data.SqlClient.UseOneSecFloorInTimeoutCalculationDuringLogin";

            /// <summary>
            ///     Gets or sets a value indicating whether managed SNI networking is forced on Windows instead of native SNI.
            /// </summary>
            public static bool? UseManagedNetworkingOnWindows
            {
                get => GetSwitch(UseManagedNetworkingOnWindowsSwitch);
                set => SetSwitch(UseManagedNetworkingOnWindowsSwitch, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether Transparent Network IP Resolution (TNIR) is disabled by default.
            /// </summary>
            public static bool? DisableTnirByDefault
            {
                get => GetSwitch(DisableTnirByDefaultSwitch);
                set => SetSwitch(DisableTnirByDefaultSwitch, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether insecure TLS 1.0/1.1 connection warnings are suppressed.
            /// </summary>
            public static bool? SuppressInsecureTlsWarning
            {
                get => GetSwitch(SuppressInsecureTlsWarningSwitch);
                set => SetSwitch(SuppressInsecureTlsWarningSwitch, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether login timeout calculations enforce a one-second minimum floor.
            /// </summary>
            public static bool? UseOneSecFloorInTimeoutCalculationDuringLogin
            {
                get => GetSwitch(UseOneSecFloorInTimeoutCalculationDuringLoginSwitch);
                set => SetSwitch(UseOneSecFloorInTimeoutCalculationDuringLoginSwitch, value);
            }
        }
    }
}
