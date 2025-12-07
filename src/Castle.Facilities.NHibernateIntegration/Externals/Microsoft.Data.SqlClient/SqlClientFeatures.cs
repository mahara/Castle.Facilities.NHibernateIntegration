namespace Microsoft.Data.SqlClient
{
    /// <summary>
    ///     Provides strongly typed access to <see cref="AppContext" /> feature switches for <see cref="SqlClient" />.
    /// </summary>
    public static partial class SqlClientFeatures
    {
        private static bool? GetSwitch(string switchName) =>
            AppContext.TryGetSwitch(switchName, out bool isEnabled) ?
            isEnabled :
            null;

        private static void SetSwitch(string switchName, bool? isEnabled)
        {
            if (isEnabled.HasValue)
            {
                AppContext.SetSwitch(switchName, isEnabled.Value);
            }
        }
    }
}
