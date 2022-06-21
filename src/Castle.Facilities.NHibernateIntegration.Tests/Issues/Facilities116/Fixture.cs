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

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities116
{
    using System;
    using System.Configuration;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;

    using Builders;

    using Castle.MicroKernel;

    using Core.Configuration;
    using Core.Resource;

    using MicroKernel.SubSystems.Configuration;

    using NUnit.Framework;

    using Windsor.Configuration.Interpreters;

    using Configuration = NHibernate.Cfg.Configuration;

    [TestFixture]
    public class Fixture : IssueTestCase
    {
        protected override string ConfigurationFile =>
            "EmptyConfiguration.xml";

        private const string Filename = "myconfig.dat";

        private IConfiguration _configuration;
        private IConfigurationBuilder _configurationBuilder;

        public override void OnSetUp()
        {
            var configurationStore = new DefaultConfigurationStore();
            var resource = new AssemblyResource("Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities116/facility.xml");
            var xmlInterpreter = new XmlInterpreter(resource);
            xmlInterpreter.ProcessResource(resource, configurationStore, new DefaultKernel());
            _configuration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName).Children["factory"];
            _configurationBuilder = new PersistentConfigurationBuilder();
        }

        public override void OnTearDown()
        {
            File.Delete(Filename);
        }

        [Test]
        public void CanCreateSerializedFileInTheDisk()
        {
            Assert.IsFalse(File.Exists(Filename));

            _configurationBuilder.GetConfiguration(_configuration);
            Assert.IsTrue(File.Exists(Filename));

            var bf = new BinaryFormatter();
            Configuration nhConfig;
            using (var fileStream = new FileStream(Filename, FileMode.Open))
            {
                nhConfig = bf.Deserialize(fileStream) as Configuration;
            }

            Assert.IsNotNull(nhConfig);

            ConfigureConnectionSettings(nhConfig);

            nhConfig.BuildSessionFactory();
        }

        [Test]
        public void CanDeserializeFileFromTheDiskIfNewEnough()
        {
            Assert.IsFalse(File.Exists(Filename));

            var nhConfig = _configurationBuilder.GetConfiguration(_configuration);
            Assert.IsTrue(File.Exists(Filename));

            var dateTime = File.GetLastWriteTime(Filename);
            Thread.Sleep(1000);
            nhConfig = _configurationBuilder.GetConfiguration(_configuration);
            Assert.AreEqual(File.GetLastWriteTime(Filename), dateTime);
            Assert.IsNotNull(_configuration);

            ConfigureConnectionSettings(nhConfig);

            nhConfig.BuildSessionFactory();
        }

        [Test]
        public void CanDeserializeFileFromTheDiskIfOneOfTheDependenciesIsNewer()
        {
            Assert.IsFalse(File.Exists(Filename));

            var nhConfig = _configurationBuilder.GetConfiguration(_configuration);
            Assert.IsTrue(File.Exists(Filename));

            var dateTime = File.GetLastWriteTime(Filename);
            Thread.Sleep(1000);
            var dateTime2 = DateTime.Now;
            File.Create("SampleDllFile").Dispose();
            File.SetLastWriteTime("SampleDllFile", dateTime2);
            nhConfig = _configurationBuilder.GetConfiguration(_configuration);
            Assert.Greater(File.GetLastWriteTime(Filename), dateTime);
            Assert.IsNotNull(_configuration);

            ConfigureConnectionSettings(nhConfig);

            nhConfig.BuildSessionFactory();
        }

        private static void ConfigureConnectionSettings(Configuration nhConfig)
        {
            nhConfig.Properties["dialect"] =
                ConfigurationManager.AppSettings["nhf.dialect"];
            nhConfig.Properties["connection.driver_class"] =
                ConfigurationManager.AppSettings["nhf.connection.driver_class"];
            nhConfig.Properties["connection.provider"] =
                ConfigurationManager.AppSettings["nhf.connection.provider"];
            nhConfig.Properties["connection.connection_string"] =
                ConfigurationManager.AppSettings["nhf.connection.connection_string.1"];
        }
    }
}