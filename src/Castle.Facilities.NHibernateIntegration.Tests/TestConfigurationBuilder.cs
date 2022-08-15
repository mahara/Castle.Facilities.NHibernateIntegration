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

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    using System.Configuration;
    using System.IO;

    using Builders;

    using Core.Configuration;

    using NUnit.Framework;

    using Configuration = NHibernate.Cfg.Configuration;

    public class TestConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IConfigurationBuilder _configurationBuilder;

        public TestConfigurationBuilder()
        {
            _configurationBuilder = new DefaultConfigurationBuilder();
        }

        public Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            var configuration = _configurationBuilder.GetConfiguration(facilityConfiguration);

#if NET
            var configurationFilePath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath;
            var configurationFileName = Path.GetFileName(configurationFilePath);
            Assert.That(configurationFileName, Is.AnyOf("testhost.dll.config", "testhost.x86.dll.config"));
#endif

            configuration.Properties["dialect"] =
                ConfigurationManager.AppSettings["nhf.dialect"];
            configuration.Properties["connection.driver_class"] =
                ConfigurationManager.AppSettings["nhf.connection.driver_class"];
            configuration.Properties["connection.provider"] =
                ConfigurationManager.AppSettings["nhf.connection.provider"];
            configuration.Properties["connection.connection_string"] =
                ConfigurationManager.AppSettings["nhf.connection.connection_string.1"];
            if (facilityConfiguration.Attributes["id"] != "sessionFactory1")
            {
                configuration.Properties["connection.connection_string"] =
                    ConfigurationManager.AppSettings["nhf.connection.connection_string.2"];
            }

            return configuration;
        }
    }
}