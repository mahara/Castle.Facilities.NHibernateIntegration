#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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

using NHibernate;

using NUnit.Framework;

using Configuration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
    public class CustomConfigurationBuilder : IConfigurationBuilder
    {
        private int _configurationsCreated;

        public int ConfigurationsCreated
        {
            get { return _configurationsCreated; }
        }

        #region IConfigurationBuilder Members

        public Configuration GetConfiguration(IConfiguration config)
        {
            _configurationsCreated++;

            Configuration nhConfig = new DefaultConfigurationBuilder().GetConfiguration(config);
            nhConfig.Properties["dialect"] = ConfigurationManager.AppSettings["nhf.dialect"];
            nhConfig.Properties["connection.driver_class"] = ConfigurationManager.AppSettings["nhf.connection.driver_class"];
            nhConfig.Properties["connection.provider"] = ConfigurationManager.AppSettings["nhf.connection.provider"];
            nhConfig.Properties["connection.connection_string"] =
                ConfigurationManager.AppSettings["nhf.connection.connection_string.1"];
            if (config.Attributes["id"] != "sessionFactory1")
            {
                nhConfig.Properties["connection.connection_string"] =
                    ConfigurationManager.AppSettings["nhf.connection.connection_string.2"];
            }
            return nhConfig;
        }

        #endregion
    }

    public class CustomNHibernateFacility : NHibernateFacility
    {
        public CustomNHibernateFacility()
            : base(new CustomConfigurationBuilder())
        {
        }
    }

    public abstract class AbstractCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
    {
        [Test]
        public void Invoked()
        {
            ISession session = container.Resolve<ISessionManager>().OpenSession();
            CustomConfigurationBuilder configurationBuilder =
                (CustomConfigurationBuilder) container.Resolve<IConfigurationBuilder>();
            Assert.AreEqual(1, configurationBuilder.ConfigurationsCreated);
            session.Close();
        }
    }

    [TestFixture]
    public class CustomConfigurationBuilderTestCase : AbstractCustomConfigurationBuilderTestCase
    {
        protected override string ConfigurationFile
        {
            get { return "customConfigurationBuilder.xml"; }
        }
    }

    [TestFixture]
    public class CustomConfigurationBulderRegressionTestCase : AbstractCustomConfigurationBuilderTestCase
    {
        protected override string ConfigurationFile
        {
            get { return "configurationBuilderRegression.xml"; }
        }
    }

    [TestFixture]
    public class InvalidCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
    {
        [Test]
        public void ThrowsWithMessage()
        {
            void Method()
            {
                _ = new WindsorContainer(new XmlInterpreter(new AssemblyResource(GetContainerFile())));
            }

            Assert.Throws<FacilityException>(
                Method,
                "ConfigurationBuilder type 'InvalidType' invalid or not found");
        }

        public override void SetUp()
        {
        }

        public override void TearDown()
        {
        }

        protected override string ConfigurationFile
        {
            get { return "invalidConfigurationBuilder.xml"; }
        }
    }
}
