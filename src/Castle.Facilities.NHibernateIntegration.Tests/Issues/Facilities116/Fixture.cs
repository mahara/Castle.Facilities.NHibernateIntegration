#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities116
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Threading;

    using Castle.Core.Configuration;
    using Castle.Core.Resource;
    using Castle.Facilities.NHibernateIntegration.Builders;
    using Castle.Facilities.NHibernateIntegration.Persisters;
    using Castle.MicroKernel;
    using Castle.MicroKernel.SubSystems.Configuration;
    using Castle.Windsor.Configuration.Interpreters;

    using NUnit.Framework;

    using Configuration = NHibernate.Cfg.Configuration;

    [TestFixture]
    public class Fixture : IssueTestCase
    {
        private const string FilePath = "myconfig.dat";

        private readonly Func<IObjectPersister<Configuration>> _objectPersister =
            ObjectPersisterFactory.Create<Configuration>;

        private IConfiguration _facilityConfiguration = null!;
        private IConfigurationBuilder _configurationBuilder = null!;

        protected override string ConfigurationFile =>
            "EmptyConfiguration.xml";

        [Test]
        public void CanCreateSerializedFileInTheDisk()
        {
            CleanUpFiles();

            Assert.That(File.Exists(FilePath), Is.False);

            _configurationBuilder.GetConfiguration(_facilityConfiguration);
            Assert.That(File.Exists(FilePath), Is.True);

            var persister = _objectPersister();
            var configuration = persister.Read(FilePath);

            Assert.That(configuration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void CanDeserializeFileFromTheDiskIfNewEnough()
        {
            CleanUpFiles();

            Assert.That(File.Exists(FilePath), Is.False);

            _configurationBuilder.GetConfiguration(_facilityConfiguration);
            Assert.That(File.Exists(FilePath), Is.True);

            var dateTime = File.GetLastWriteTime(FilePath);

            Thread.Sleep(100);

            var configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);
            Assert.That(dateTime, Is.EqualTo(File.GetLastWriteTime(FilePath)));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void CanDeserializeFileFromTheDiskIfOneOfTheDependenciesIsNewer()
        {
            CleanUpFiles();

            Assert.That(File.Exists(FilePath), Is.False);

            _configurationBuilder.GetConfiguration(_facilityConfiguration);
            Assert.That(File.Exists(FilePath), Is.True);

            var dateTime1 = File.GetLastWriteTime(FilePath);

            Thread.Sleep(100);

            var dateTime2 = DateTime.Now;
            var fileName = "SampleDllFile";
            var filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, fileName);
            File.Create(filePath).Dispose();
            File.SetLastWriteTime(filePath, dateTime2);

            var configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);
            Assert.That(File.GetLastWriteTime(filePath), Is.GreaterThan(dateTime1));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        protected override void OnSetUp()
        {
            var configurationStore = new DefaultConfigurationStore();
            var resource = new AssemblyResource("Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities116/facility.xml");
            var xmlInterpreter = new XmlInterpreter(resource);
            xmlInterpreter.ProcessResource(resource, configurationStore, new DefaultKernel());
            _facilityConfiguration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName).Children["factory"];
            _configurationBuilder = new PersistentConfigurationBuilder();
        }

        protected override void OnTearDown()
        {
            CleanUpFiles();
        }

        private static void ConfigureConnectionSettings(Configuration configuration)
        {
            configuration.Properties["dialect"] =
                ConfigurationManager.AppSettings["nhf.dialect"];
            configuration.Properties["connection.driver_class"] =
                ConfigurationManager.AppSettings["nhf.connection.driver_class"];
            configuration.Properties["connection.provider"] =
                ConfigurationManager.AppSettings["nhf.connection.provider"];
            configuration.Properties["connection.connection_string"] =
                ConfigurationManager.AppSettings["nhf.connection.connection_string.1"];
        }

        private static void CleanUpFiles()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}
