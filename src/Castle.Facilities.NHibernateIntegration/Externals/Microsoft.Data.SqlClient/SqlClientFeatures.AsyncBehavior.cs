namespace Microsoft.Data.SqlClient
{
    public static partial class SqlClientFeatures
    {
        /// <summary>
        ///     Configures asynchronous execution and packet-multiplexing switches for <see cref="SqlClient" />.
        /// </summary>
        public static class AsyncBehavior
        {
            internal const string UseCompatibilityAsyncBehaviourSwitch = "Switch.Microsoft.Data.SqlClient.UseCompatibilityAsyncBehaviour";
            internal const string UseCompatibilityProcessSniSwitch = "Switch.Microsoft.Data.SqlClient.UseCompatibilityProcessSni";

            /// <summary>
            ///     Gets a value indicating whether asynchronous packet multiplexing is explicitly enabled,
            ///     or <see langword="null" /> if neither underlying switch has been configured.
            /// </summary>
            public static bool? IsPacketMultiplexingEnabled
            {
                get
                {
                    bool? useCompatibilityAsyncBehaviour = UseCompatibilityAsyncBehaviour;
                    bool? useCompatibilityProcessSni = UseCompatibilityProcessSni;

                    if (useCompatibilityAsyncBehaviour is null &&
                        useCompatibilityProcessSni is null)
                    {
                        return null;
                    }

                    return useCompatibilityAsyncBehaviour == false &&
                           useCompatibilityProcessSni == false;
                }
            }

            /// <summary>
            ///     Gets a value indicating whether <c>Switch.Microsoft.Data.SqlClient.UseCompatibilityAsyncBehaviour</c> is enabled,
            ///     or <see langword="null" /> if the switch has not been configured.
            /// </summary>
            public static bool? UseCompatibilityAsyncBehaviour =>
                GetSwitch(UseCompatibilityAsyncBehaviourSwitch);

            /// <summary>
            ///     Gets a value indicating whether <c>Switch.Microsoft.Data.SqlClient.UseCompatibilityProcessSni</c> is enabled,
            ///     or <see langword="null" /> if the switch has not been configured.
            /// </summary>
            public static bool? UseCompatibilityProcessSni =>
                GetSwitch(UseCompatibilityProcessSniSwitch);

            /// <summary>
            ///     Enables or disables asynchronous packet multiplexing for <see cref="SqlClient" />.
            /// </summary>
            /// <param name="enable">
            ///     <see langword="true" /> to enable packet multiplexing;
            ///     <see langword="false" /> to retain legacy compatibility behavior.
            /// </param>
            /// <remarks>
            ///     Must be called during startup before any <see cref="SqlClient" /> types are initialized.
            ///
            ///     REFERENCES:
            ///     -   <see href="https://github.com/dotnet/SqlClient/blob/main/release-notes/7.0/7.0.0.md" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/discussions/4061" />
            ///     -   <see href="https://github.com/dotnet/SqlClient/issues/593" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/discussions/2603" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/pull/3534" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/pull/3537" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/pull/3605" />
            ///     -   <see href="https://github.com/dotnet/SqlClient/issues/4925" />
            ///         -   <see href="https://github.com/dotnet/SqlClient/pull/4973" />
            ///     -   <see href="https://github.com/dotnet/SqlClient/issues/4974" />
            /// </remarks>
            public static void ConfigurePacketMultiplexing(bool enable)
            {
                if (enable)
                {
                    SetSwitch(UseCompatibilityAsyncBehaviourSwitch, false);
                    SetSwitch(UseCompatibilityProcessSniSwitch, false);
                }
                else
                {
                    SetSwitch(UseCompatibilityAsyncBehaviourSwitch, true);
                    SetSwitch(UseCompatibilityProcessSniSwitch, true);
                }
            }
        }
    }
}
