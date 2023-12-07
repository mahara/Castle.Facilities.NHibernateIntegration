#region License
// Copyright 2004-2023 Castle Project - https://www.castleproject.org/
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

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities106
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        protected override string ConfigurationFilePath => "EmptyConfiguration.xml";

        [Test]
        public void CanReadSessionFactoryFromNHibernateConfigurationFile()
        {
            var facilityConfiguration = new MutableConfiguration("myConfig");
            facilityConfiguration.Attributes[Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName] =
                "Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities106/SessionFactory1.xml";
            var facilityConfigurationBuilder = new XmlConfigurationBuilder();
            var configuration = facilityConfigurationBuilder.GetConfiguration(facilityConfiguration);

            Assert.That(configuration, Is.Not.Null);

            var connectionProvider = configuration.Properties["connection.provider"];

            Assert.That(connectionProvider, Is.EqualTo("DummyProvider"));

            var connectionString = configuration.Properties["connection.connection_string"];

            Assert.That(connectionString, Is.Not.Empty);

            var connectionDriverClass = configuration.Properties["connection.driver_class"];

            Assert.That(connectionDriverClass, Is.Not.Empty);

            var dialect = configuration.Properties["dialect"];

            Assert.That(dialect, Is.Not.Empty);
        }
    }
}
