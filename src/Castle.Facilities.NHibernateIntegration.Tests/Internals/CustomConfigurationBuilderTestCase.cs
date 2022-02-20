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
using Castle.MicroKernel.Facilities;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

using NUnit.Framework;

using Configuration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
    public class CustomConfigurationBuilder : IConfigurationBuilder
    {
        public int ConfigurationsCreated { get; private set; }

        public Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            ConfigurationsCreated++;

            var configuration = new DefaultConfigurationBuilder().GetConfiguration(facilityConfiguration);

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

    public class CustomNHibernateFacility : NHibernateFacility
    {
        public CustomNHibernateFacility() :
            base(new CustomConfigurationBuilder())
        {
        }
    }

    public abstract class AbstractCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
    {
        [Test]
        public void Invoked()
        {
            var session = Container.Resolve<ISessionManager>().OpenSession();

            var configurationBuilder = (CustomConfigurationBuilder) Container.Resolve<IConfigurationBuilder>();

            Assert.That(configurationBuilder.ConfigurationsCreated, Is.EqualTo(1));

            session.Close();
        }
    }

    [TestFixture]
    public class CustomConfigurationBuilderTestCase : AbstractCustomConfigurationBuilderTestCase
    {
        protected override string ConfigurationFilePath =>
            "CustomConfigurationBuilder.xml";
    }

    [TestFixture]
    public class CustomConfigurationBuilderRegressionTestCase : AbstractCustomConfigurationBuilderTestCase
    {
        protected override string ConfigurationFilePath =>
            "ConfigurationBuilderRegression.xml";
    }

    [TestFixture]
    public class InvalidCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFilePath =>
            "InvalidConfigurationBuilder.xml";

        public override void SetUp()
        {
        }

        public override void TearDown()
        {
        }

        [Test]
        public void ThrowsWithMessage()
        {
            void Method()
            {
                Container = new WindsorContainer(
                    new XmlInterpreter(
                        new AssemblyResource(GetContainerFilePath())));
            }

            Assert.Throws<FacilityException>(
                Method,
                "'ConfigurationBuilder' of type 'InvalidType' is invalid or can not be found.");
        }
    }
}
