#if NET
using NUnit.Framework;
#endif

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    internal class TestHelper
    {
        public static void AssertApplicationConfigurationFileExists()
        {
#if NET
            var configurationFilePath = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;
            var configurationFileName = Path.GetFileName(configurationFilePath);

            Assert.That(configurationFileName, Is.AnyOf("testhost.dll.config",
                                                        "testhost.x86.dll.config")
                                                 .IgnoreCase);
#endif
        }
    }
}
