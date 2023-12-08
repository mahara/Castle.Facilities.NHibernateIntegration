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

namespace Castle.Facilities.NHibernateIntegration.Tests.Registration
{
    using Castle.Core.Configuration;
    using Castle.Facilities.AutoTx;
    using Castle.Facilities.NHibernateIntegration.SessionStores;
    using Castle.MicroKernel.Facilities;
    using Castle.Windsor;

    using NHibernate.Cfg;

    using NUnit.Framework;

    [TestFixture]
    public class FacilityFluentConfigTestCase
    {
        [Test]
        public void ShouldUseDefaultSessionStore()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf(typeof(AsyncLocalSessionStore)));
        }

        [Test]
        public void ShouldOverrideDefaultSessionStore()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            // Starts with AsyncLocalSessionStore
            // then change it to WebSessionStore
            // then change it to LogicalCallContextSessionStore.
            // then change it again to DummySessionStore.
            // The last set session store should be DummySessionStore.
            container.AddFacility<NHibernateFacility>(
                f => f.IsWeb()
                      .SessionStore<LogicalCallContextSessionStore>()
                      .SessionStore<CallContextSessionStore>()
                      .SessionStore<DummySessionStore>()
                      .ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf(typeof(DummySessionStore)));
        }

        [Test]
        public void ShouldBeAbleToResolveISessionManager()
        {
            var container = new WindsorContainer();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<TestConfigurationBuilder>());

            var sessionManager = container.Resolve<ISessionManager>();
            sessionManager.OpenSession();

            Assert.That(container.Resolve<IConfigurationBuilder>().GetType(),
                        Is.EqualTo(typeof(TestConfigurationBuilder)));
        }

        [Test]
        public void ShouldNotAcceptNonImplementorsOfIConfigurationBuilderForOverride()
        {
            void Method()
            {
                var container = new WindsorContainer();

                container.AddFacility<NHibernateFacility>(
                    f => f.ConfigurationBuilder(GetType()));
            }

            Assert.That(Method, Throws.TypeOf<FacilityException>());
        }

        [Test]
        public void ShouldOverrideDefaultConfigurationBuilder()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

            Assert.That(container.Resolve<IConfigurationBuilder>().GetType(),
                        Is.EqualTo(typeof(DummyConfigurationBuilder)));
        }

        [Test]
        public void ShouldOverrideIsWeb()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.IsWeb()
                      .ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf(typeof(WebSessionStore)));
        }
    }

    internal class DummyConfigurationBuilder : IConfigurationBuilder
    {
        public Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            return new Configuration();
        }
    }

    internal class DummySessionStore : ISessionStore
    {
        public SessionDelegate FindCompatibleSession(string alias)
        {
            throw new System.NotImplementedException();
        }

        public StatelessSessionDelegate FindCompatibleStatelessSession(string alias)
        {
            throw new System.NotImplementedException();
        }

        public bool IsCurrentActivityEmptyFor(string alias)
        {
            throw new System.NotImplementedException();
        }

        public void Store(string alias, SessionDelegate session)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(SessionDelegate session)
        {
            throw new System.NotImplementedException();
        }

        public void Store(string alias, StatelessSessionDelegate session)
        {
            throw new System.NotImplementedException();
        }

        public void Remove(StatelessSessionDelegate session)
        {
            throw new System.NotImplementedException();
        }
    }
}
