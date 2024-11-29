#region License
// Copyright (c) 2004-2024 Castle Project - https://www.castleproject.org/
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

using Castle.Core.Configuration;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Configuration;
using Castle.MicroKernel.Facilities;
using Castle.Windsor;

using Microsoft.Extensions.Configuration;

using NUnit.Framework;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using ConfigurationErrorsException = System.Configuration.ConfigurationErrorsException;

namespace Castle.Facilities.NHibernateIntegration.Tests.Configuration
{
    [TestFixture]
    public class MicrosoftConfigurationMapperTest : TestBase
    {
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private IWindsorContainer _container = null!;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

        private DefaultMicrosoftConfigurationMapper _configurationMapper = null!;

        protected override void OnSetUp()
        {
            _container = new WindsorContainer();

            _configurationMapper = new DefaultMicrosoftConfigurationMapper(_container.Kernel);
        }

        protected override void OnTearDown()
        {
            _configurationMapper = null!;

            _container.Dispose();
            _container = null!;
        }



        [Test]
        public void GetFacilityType_CastleConfiguration_ShouldReturnConfiguredFacilityType()
        {
            var configurationNode =
                new MutableConfiguration(Constants.Facility_ConfigurationElementName);

            configurationNode.Attributes[Constants.FacilityType_ConfigurationElementAttributeName] =
                GetTypeFullNameWithAssemblyName(typeof(NHibernateFacility));

            var facilityType = _configurationMapper.GetFacilityType(configurationNode);

            Assert.That(facilityType, Is.EqualTo(typeof(NHibernateFacility)));
        }

        [Test]
        public void GetFacilityType_CastleConfiguration_ShouldThrowWhenTypeIsMissing()
        {
            var configurationNode =
                new MutableConfiguration(Constants.Facility_ConfigurationElementName);

            Assert.That(
                () => _configurationMapper.GetFacilityType(configurationNode),
                Throws.TypeOf<ConfigurationErrorsException>()
                      .With.Message.EqualTo($"The '{Constants.FacilityType_ConfigurationElementAttributeName}' attribute is required."));
        }

        [Test]
        public void GetFacilityType_CastleConfiguration_ShouldThrowWhenTypeCannotBeResolved()
        {
            var configurationNode =
                new MutableConfiguration(Constants.Facility_ConfigurationElementName);

            configurationNode.Attributes[Constants.FacilityType_ConfigurationElementAttributeName] =
                "Does.Not.Exist.SomeFacility, Does.Not.Exist";

            Assert.That(
                () => _configurationMapper.GetFacilityType(configurationNode),
                Throws.TypeOf<FacilityException>()
                      .With.Message.Contains($"The type 'Does.Not.Exist.SomeFacility, Does.Not.Exist' specified in the '{Constants.FacilityType_ConfigurationElementAttributeName}' attribute could not be resolved."));
        }



        [Test]
        public void GetFacilityType_MicrosoftConfiguration_ShouldReturnConfiguredFacilityType()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}"] =
                            GetTypeFullNameWithAssemblyName(typeof(NHibernateFacility)),
                    })
                .Build();

            var facilityType = _configurationMapper.GetFacilityType(configuration);

            Assert.That(facilityType, Is.EqualTo(typeof(NHibernateFacility)));
        }

        [Test]
        public void GetFacilityType_MicrosoftConfiguration_ShouldThrowWhenTypeIsMissing()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [Constants.NHibernateFacility_ConfigurationSectionName] =
                            string.Empty,
                    })
                .Build();

            Assert.That(
                () => _configurationMapper.GetFacilityType(configuration),
                Throws.TypeOf<ConfigurationErrorsException>()
                      .With.Message.EqualTo($"The '{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}' section is required."));
        }

        [Test]
        public void GetFacilityType_MicrosoftConfiguration_ShouldThrowWhenTypeCannotBeResolved()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [$"{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}"] =
                            "Does.Not.Exist.SomeFacility, Does.Not.Exist",
                    })
                .Build();

            Assert.That(
                () => _configurationMapper.GetFacilityType(configuration),
                Throws.TypeOf<FacilityException>()
                      .With.Message.Contains($"The type 'Does.Not.Exist.SomeFacility, Does.Not.Exist' specified in the '{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}' section could not be resolved."));
        }



        [Test]
        public void Map_ShouldMap_MicrosoftConfiguration_To_CastleConfiguration()
        {
            var microsoftConfiguration = new ConfigurationBuilder()
                .SetBasePath(TestFixtureBaseFolderPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var castleConfiguration = _configurationMapper.Map(microsoftConfiguration);

            Assert.That(castleConfiguration, Is.Not.Null);
            Assert.That(
                castleConfiguration.Name,
                Is.EqualTo(Constants.Facility_ConfigurationElementName));

            Assert.That(
                castleConfiguration.Attributes[Constants.FacilityType_ConfigurationElementAttributeName],
                Is.EqualTo(GetTypeFullNameWithAssemblyName(typeof(NHibernateFacility))));

            Assert.That(
                castleConfiguration.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName],
                Is.EqualTo(GetTypeFullNameWithAssemblyName(typeof(DefaultConfigurationBuilder))));

            Assert.That(
                castleConfiguration.Attributes[Constants.SessionStoreType_ConfigurationElementAttributeName],
                Is.Null);

            Assert.That(
                castleConfiguration.Attributes[Constants.SessionStore_IsWeb_ConfigurationElementAttributeName],
                Is.Null);

            Assert.That(
                castleConfiguration.Attributes[Constants.Session_DefaultFlushMode_ConfigurationElementAttributeName],
                Is.Null);

            Assert.That(
                castleConfiguration.Attributes[Constants.UseReflectionOptimizer_ConfigurationElementAttributeName],
                Is.Null);

            Assert.That(castleConfiguration.Children, Has.Count.EqualTo(2));

            var sessionFactory1 = castleConfiguration.Children[0];

            Assert.That(
                sessionFactory1.Name,
                Is.EqualTo(Constants.SessionFactory_ConfigurationElementName));

            Assert.That(
                sessionFactory1.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName],
                Is.EqualTo("sessionFactory1"));

            Assert.That(
                sessionFactory1.Attributes[Constants.SessionFactory_Alias_ConfigurationElementAttributeName],
                Is.Null);

            var sessionFactory1Settings = sessionFactory1.Children[0];

            Assert.That(
                sessionFactory1Settings.Name,
                Is.EqualTo(Constants.SessionFactory_Settings_ConfigurationElementName));

            Assert.That(sessionFactory1Settings.Children, Has.Count.EqualTo(5));

            AssertSetting(
                sessionFactory1Settings,
                NHibernate.Cfg.Environment.ConnectionStringName,
                "Connection.1");

            AssertSetting(
                sessionFactory1Settings,
                NHibernate.Cfg.Environment.ConnectionString,
                "Server=.; Initial Catalog=test; Integrated Security=SSPI");

            AssertSetting(
                sessionFactory1Settings,
                NHibernate.Cfg.Environment.ConnectionProvider,
                "NHibernate.Connection.DriverConnectionProvider");

            AssertSetting(
                sessionFactory1Settings,
                NHibernate.Cfg.Environment.ConnectionDriver,
                "NHibernate.Driver.Sql2008ClientDriver");

            AssertSetting(
                sessionFactory1Settings,
                NHibernate.Cfg.Environment.Dialect,
                "NHibernate.Dialect.MsSql2012Dialect");

            var sessionFactory2 = castleConfiguration.Children[1];

            Assert.That(
                sessionFactory2.Name,
                Is.EqualTo(Constants.SessionFactory_ConfigurationElementName));

            Assert.That(
                sessionFactory2.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName],
                Is.EqualTo("sessionFactory2"));

            Assert.That(
                sessionFactory2.Attributes[Constants.SessionFactory_Alias_ConfigurationElementAttributeName],
                Is.EqualTo("sessionFactory2"));

            var sessionFactory2Settings = sessionFactory2.Children[0];

            Assert.That(
                sessionFactory2Settings.Name,
                Is.EqualTo(Constants.SessionFactory_Settings_ConfigurationElementName));

            Assert.That(sessionFactory2Settings.Children, Has.Count.EqualTo(5));

            AssertSetting(
                sessionFactory2Settings,
                NHibernate.Cfg.Environment.ConnectionStringName,
                "Connection.2");

            AssertSetting(
                sessionFactory2Settings,
                NHibernate.Cfg.Environment.ConnectionString,
                "Server=.; Initial Catalog=test2; Integrated Security=SSPI");

            AssertSetting(
                sessionFactory2Settings,
                NHibernate.Cfg.Environment.ConnectionProvider,
                "NHibernate.Connection.DriverConnectionProvider");

            AssertSetting(
                sessionFactory2Settings,
                NHibernate.Cfg.Environment.ConnectionDriver,
                "NHibernate.Driver.Sql2008ClientDriver");

            AssertSetting(
                sessionFactory2Settings,
                NHibernate.Cfg.Environment.Dialect,
                "NHibernate.Dialect.MsSql2012Dialect");
        }

        private static void AssertSetting(
            CastleConfiguration settings,
            string key,
            string expectedValue)
        {
            var item = settings.Children.Single(
                x =>
                x.Attributes[Constants.SessionFactory_Settings_Key_ConfigurationElementAttributeName] == key);

            Assert.That(
                item.Name,
                Is.EqualTo(Constants.SessionFactory_Settings_Item_ConfigurationElementAttributeName));

            Assert.That(
                item.Attributes[Constants.SessionFactory_Settings_Key_ConfigurationElementAttributeName],
                Is.EqualTo(key));

            Assert.That(item.Value, Is.EqualTo(expectedValue));
        }



        private static string GetTypeFullNameWithAssemblyName(Type type) =>
            $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
