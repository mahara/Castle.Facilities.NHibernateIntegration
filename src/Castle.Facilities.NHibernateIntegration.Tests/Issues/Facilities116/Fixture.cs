#region License
// Copyright (c) 2004-2026 Castle Project - https://www.castleproject.org/
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

using Castle.Core.Resource;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Persisters;
using Castle.MicroKernel;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.Configuration.Interpreters;

using NUnit.Framework;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using NHibernateConfiguration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities116
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        private const string FilePath = "myconfig.dat";

        private readonly Func<IObjectPersister<NHibernateConfiguration>> _objectPersister =
            ObjectPersisterFactory.Create<NHibernateConfiguration>;

        private CastleConfiguration _facilityConfiguration = null!;
        private IConfigurationBuilder _configurationBuilder = null!;

        protected override string ConfigurationFilePath =>
            "EmptyConfiguration.xml";

        protected override void OnSetUp()
        {
            CleanUpFiles();

            var configurationStore = new DefaultConfigurationStore();
            var resource = new AssemblyResource("Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities116/facility.xml");
            var xmlInterpreter = new XmlInterpreter(resource);
            xmlInterpreter.ProcessResource(resource, configurationStore, new DefaultKernel());
            _facilityConfiguration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName)
                                                       .Children[Constants.SessionFactories_ConfigurationElementName]
                                                       .Children[Constants.SessionFactory_ConfigurationElementName];
            _configurationBuilder = new PersistentConfigurationBuilder();
        }

        protected override void OnTearDown()
        {
            File.Delete(FilePath);
        }

        private static void CleanUpFiles()
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }

        [Test]
        public void CanCreate_SerializedFile_InTheDisk()
        {
            Assert.That(File.Exists(FilePath), Is.False);

            _configurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FilePath));

            var persister = _objectPersister();
            var configuration = persister.Read(FilePath);

            Assert.That(configuration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void Can_DeserializeFile_FromTheDiskIfNewEnough()
        {
            Assert.That(File.Exists(FilePath), Is.False);

            NHibernateConfiguration configuration;

            configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FilePath));

            var dateTime = File.GetLastWriteTime(FilePath);

            Thread.Sleep(1000);

            configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(dateTime, Is.EqualTo(File.GetLastWriteTime(FilePath)));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        [Test]
        public void Can_DeserializeFile_FromTheDiskIfOneOfTheDependentFilesIsNewer()
        {
            Assert.That(File.Exists(FilePath), Is.False);

            NHibernateConfiguration configuration;

            configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.Exists(FilePath));

            var dateTime1 = File.GetLastWriteTime(FilePath);

            Thread.Sleep(100);

            var dateTime2 = DateTime.Now;
            var dependentFilePath = "SampleDllFile.dll";
            File.Create(dependentFilePath).Dispose();
            File.SetLastWriteTime(dependentFilePath, dateTime2);
            configuration = _configurationBuilder.GetConfiguration(_facilityConfiguration);

            Assert.That(File.GetLastWriteTime(FilePath), Is.GreaterThan(dateTime1));
            Assert.That(_facilityConfiguration, Is.Not.Null);

            ConfigureConnectionSettings(configuration);

            configuration.BuildSessionFactory();
        }

        private static void ConfigureConnectionSettings(NHibernateConfiguration configuration)
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
