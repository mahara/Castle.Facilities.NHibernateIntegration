using NUnit.Framework;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Data.SqlClient.Tests
{
    [TestFixture]
    [NonParallelizable]
    public class SqlClientFeaturesTests
    {
        private static readonly string[] AllSwitchNames =
        [
            SqlClientFeatures.AsyncBehavior.UseCompatibilityAsyncBehaviourSwitch,
            SqlClientFeatures.AsyncBehavior.UseCompatibilityProcessSniSwitch,
            SqlClientFeatures.Networking.UseManagedNetworkingOnWindowsSwitch,
            SqlClientFeatures.Networking.DisableTnirByDefaultSwitch,
            SqlClientFeatures.Networking.SuppressInsecureTlsWarningSwitch,
            SqlClientFeatures.Networking.UseOneSecFloorInTimeoutCalculationDuringLoginSwitch,
            SqlClientFeatures.DataCompatibility.LegacyRowVersionNullBehaviorSwitch,
            SqlClientFeatures.DataCompatibility.TruncateScaledDecimalSwitch,
        ];

        private (string Name, bool? OriginalValue)[] _savedStates = [];

        [SetUp]
        public void SetUp()
        {
            _savedStates = new (string Name, bool? OriginalValue)[AllSwitchNames.Length];

            for (int i = 0; i < AllSwitchNames.Length; i++)
            {
                string switchName = AllSwitchNames[i];
                bool? originalValue = AppContext.TryGetSwitch(switchName, out bool isEnabled) ?
                    isEnabled :
                    null;

                _savedStates[i] = (switchName, originalValue);
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach ((string name, bool? originalValue) in _savedStates)
            {
                if (originalValue.HasValue)
                {
                    AppContext.SetSwitch(name, originalValue.Value);
                }
            }
        }

        [Test]
        public void AsyncBehavior_ConfigureTrue_DisablesCompatibilitySwitchesAndEnablesPacketMultiplexing()
        {
            SqlClientFeatures.AsyncBehavior.ConfigurePacketMultiplexing(true);

            Assert.That(SqlClientFeatures.AsyncBehavior.UseCompatibilityAsyncBehaviour, Is.False);
            Assert.That(SqlClientFeatures.AsyncBehavior.UseCompatibilityProcessSni, Is.False);
            Assert.That(SqlClientFeatures.AsyncBehavior.IsPacketMultiplexingEnabled, Is.True);
        }

        [Test]
        public void AsyncBehavior_ConfigureFalse_EnablesCompatibilitySwitchesAndDisablesPacketMultiplexing()
        {
            SqlClientFeatures.AsyncBehavior.ConfigurePacketMultiplexing(false);

            Assert.That(SqlClientFeatures.AsyncBehavior.UseCompatibilityAsyncBehaviour, Is.True);
            Assert.That(SqlClientFeatures.AsyncBehavior.UseCompatibilityProcessSni, Is.True);
            Assert.That(SqlClientFeatures.AsyncBehavior.IsPacketMultiplexingEnabled, Is.False);
        }

        [Test]
        public void AsyncBehavior_PartialSwitchConfiguration_ReportsPacketMultiplexingDisabled()
        {
            AppContext.SetSwitch(SqlClientFeatures.AsyncBehavior.UseCompatibilityAsyncBehaviourSwitch, false);
            AppContext.SetSwitch(SqlClientFeatures.AsyncBehavior.UseCompatibilityProcessSniSwitch, true);

            Assert.That(SqlClientFeatures.AsyncBehavior.IsPacketMultiplexingEnabled, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Networking_UseManagedNetworkingOnWindows_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.Networking.UseManagedNetworkingOnWindows = expected;

            Assert.That(SqlClientFeatures.Networking.UseManagedNetworkingOnWindows, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Networking_DisableTnirByDefault_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.Networking.DisableTnirByDefault = expected;

            Assert.That(SqlClientFeatures.Networking.DisableTnirByDefault, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Networking_SuppressInsecureTlsWarning_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.Networking.SuppressInsecureTlsWarning = expected;

            Assert.That(SqlClientFeatures.Networking.SuppressInsecureTlsWarning, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Networking_UseOneSecFloorInTimeoutCalculationDuringLogin_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.Networking.UseOneSecFloorInTimeoutCalculationDuringLogin = expected;

            Assert.That(SqlClientFeatures.Networking.UseOneSecFloorInTimeoutCalculationDuringLogin, Is.EqualTo(expected));
        }

        [Test]
        public void Networking_NullAssignment_DoesNotOverwriteExistingSwitchValue()
        {
            SqlClientFeatures.Networking.UseManagedNetworkingOnWindows = true;
            SqlClientFeatures.Networking.UseManagedNetworkingOnWindows = null;

            Assert.That(SqlClientFeatures.Networking.UseManagedNetworkingOnWindows, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DataCompatibility_LegacyRowVersionNullBehavior_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.DataCompatibility.LegacyRowVersionNullBehavior = expected;

            Assert.That(SqlClientFeatures.DataCompatibility.LegacyRowVersionNullBehavior, Is.EqualTo(expected));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DataCompatibility_TruncateScaledDecimal_RoundTripsValue(bool expected)
        {
            SqlClientFeatures.DataCompatibility.TruncateScaledDecimal = expected;

            Assert.That(SqlClientFeatures.DataCompatibility.TruncateScaledDecimal, Is.EqualTo(expected));
        }

        [Test]
        public void DataCompatibility_NullAssignment_DoesNotOverwriteExistingSwitchValue()
        {
            SqlClientFeatures.DataCompatibility.TruncateScaledDecimal = true;
            SqlClientFeatures.DataCompatibility.TruncateScaledDecimal = null;

            Assert.That(SqlClientFeatures.DataCompatibility.TruncateScaledDecimal, Is.True);
        }
    }
}
#pragma warning restore IDE0130 // Namespace does not match folder structure
