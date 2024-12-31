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

using NHibernate.Cfg;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities119
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        protected override void CreateDatabaseSchemas()
        {
        }

        protected override void DropDatabaseSchemas()
        {
        }

        [Test]
        public void ConfigurationsCanBeObtainedViaDifferentConfigurationBuilders()
        {
            var configuration1 = Container.Resolve<Configuration>("sessionFactory1.cfg");
            var configuration2 = Container.Resolve<Configuration>("sessionFactory2.cfg");
            var configuration3 = Container.Resolve<Configuration>("sessionFactory3.cfg");

            Assert.That(configuration1.GetProperty("test"), Is.Null);
            Assert.That(configuration2.GetProperty("test"), Is.EqualTo("test2"));
            Assert.That(configuration3.GetProperty("test"), Is.EqualTo("test3"));
        }
    }
}
