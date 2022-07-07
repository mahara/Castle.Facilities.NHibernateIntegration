#region License
// Copyright 2004-2022 Castle Project - https://www.castleproject.org/
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

using System.Configuration;

using Castle.Core.Configuration;
using Castle.Core.Resource;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Persisters;
using Castle.MicroKernel;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.Configuration.Interpreters;

using NUnit.Framework;

using Configuration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities116
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        private const string FileName = "myconfig.dat";

        private readonly Func<IObjectPersister<Configuration>> _objectPersister =
            ObjectPersisterFactory.Create<Configuration>;

        private IConfiguration _facilityConfiguration;
        private IConfigurationBuilder _facilityConfigurationBuilder;

        protected override string ConfigurationFilePath => "EmptyConfiguration.xml";

        protected override void OnSetUp()
        {
            CleanUpFiles();

            var configurationStore = new DefaultConfigurationStore();
            var resource = new AssemblyResource("Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities116/facility.xml");
            var xmlInterpreter = new XmlInterpreter(resource);
            xmlInterpreter.ProcessResource(resource, configurationStore, new DefaultKernel());
            _facilityConfiguration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName).Children[Constants.SessionFactory_ConfigurationElementName];
            _facilityConfigurationBuilder = new PersistentConfigurationBuilder();
        }

        protected override void OnTearDown()
        {
            File.Delete(FileName);
        }

        private static void CleanUpFiles()
        {
            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
        }

        [Test]
        public void CanCreate_SerializedFile_InTheDisk()
        {
            Assert.That(File.Exists(FileName), Is.False);

            _facilityConfigurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FileName));

            var persister = _objectPersister();
            var configuration = persister.Read(FileName);

            Assert.That(configuration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void Can_DeserializeFile_FromTheDiskIfNewEnough()
        {
            Assert.That(File.Exists(FileName), Is.False);

            Configuration configuration;

            configuration = _facilityConfigurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FileName));

            var dateTime = File.GetLastWriteTime(FileName);

            Thread.Sleep(1000);

            configuration = _facilityConfigurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(dateTime, Is.EqualTo(File.GetLastWriteTime(FileName)));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void Can_DeserializeFile_FromTheDiskIfOneOfTheDependenciesIsNewer()
        {
            Assert.That(File.Exists(FileName), Is.False);

            Configuration configuration;

            configuration = _facilityConfigurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FileName));

            var dateTime1 = File.GetLastWriteTime(FileName);

            Thread.Sleep(100);

            var dateTime2 = DateTime.Now;
            var sampleFileName = "SampleDllFile.dll";
            File.Create(sampleFileName).Dispose();
            File.SetLastWriteTime(sampleFileName, dateTime2);
            configuration = _facilityConfigurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.GetLastWriteTime(FileName), Is.GreaterThan(dateTime1));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
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
    }
}
