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

using Castle.MicroKernel.Registration;

using NHibernate;
using NHibernate.Cfg;

using NUnit.Framework;

using Rhino.Mocks;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities113
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        protected override string ConfigurationFilePath => "DefaultConfiguration.xml";

        [Test]
        public void Calls_ConfigurationContributors_Before_SessionFactory_IsInitialized()
        {
            var configurator1 = MockRepository.GenerateMock<IConfigurationContributor>();
            var configurator2 = MockRepository.GenerateMock<IConfigurationContributor>();
            Container.Register(
                Component.For<IConfigurationContributor>()
                         .Named("c1")
                         .Instance(configurator1));
            Container.Register(
                Component.For<IConfigurationContributor>()
                         .Named("c2")
                         .Instance(configurator2));

            var configuration = Container.Resolve<Configuration>("sessionFactory1.cfg");
            Container.Resolve<ISessionFactory>("sessionFactory1");

            configurator1.AssertWasCalled(x => x.Process("sessionFactory1", configuration));
            configurator2.AssertWasCalled(x => x.Process("sessionFactory1", configuration));
        }
    }
}
