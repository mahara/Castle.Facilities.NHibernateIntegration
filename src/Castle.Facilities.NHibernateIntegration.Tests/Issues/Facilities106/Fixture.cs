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
            IConfiguration castleConfiguration = new MutableConfiguration("myConfig");
            castleConfiguration.Attributes["nhibernateConfigFile"] =
                "Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities106/factory1.xml";
            var b = new XmlConfigurationBuilder();
            var cfg = b.GetConfiguration(castleConfiguration);
            Assert.IsNotNull(cfg);
            var str = cfg.Properties["connection.provider"];
            Assert.AreEqual("DummyProvider", str);
            str = cfg.Properties["connection.connection_string"];
            Assert.IsNotEmpty(str);
            str = cfg.Properties["connection.driver_class"];
            Assert.IsNotEmpty(str);
            str = cfg.Properties["dialect"];
            Assert.IsNotEmpty(str);
        }
    }
}