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

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities106
{
    using Builders;

    using Core.Configuration;

    using NUnit.Framework;

    [TestFixture]
    public class Fixture : IssueTestCase
    {
        protected override string ConfigurationFile =>
            "EmptyConfiguration.xml";

        [Test]
        public void CanReadNHConfigFileAsTheSourceOfSessionFactory()
        {
            IConfiguration facilityConfiguration = new MutableConfiguration("myConfig");
            facilityConfiguration.Attributes["nhibernateConfigFile"] =
                "Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities106/factory1.xml";
            var b = new XmlConfigurationBuilder();
            var configuration = b.GetConfiguration(facilityConfiguration);
            Assert.That(configuration, Is.Not.Null);
            var value = configuration.Properties["connection.provider"];
            Assert.That(value, Is.EqualTo("DummyProvider"));
            value = configuration.Properties["connection.connection_string"];
            Assert.That(value, Is.Not.Empty);
            value = configuration.Properties["connection.driver_class"];
            Assert.That(value, Is.Not.Empty);
            value = configuration.Properties["dialect"];
            Assert.That(value, Is.Not.Empty);
        }
    }
}
