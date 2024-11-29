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

using Castle.Core.Configuration;
using Castle.Facilities.AutoTx;
using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.MicroKernel.Facilities;
using Castle.Windsor;

using NHibernate.Cfg;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Registration
{
    [TestFixture]
    public class FacilityFluentConfigurationTestCase
    {
        [Test]
        public void ShouldOverride_DefaultConfigurationBuilder()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

            Assert.That(container.Resolve<IConfigurationBuilder>().GetType(), Is.EqualTo(typeof(DummyConfigurationBuilder)));
        }

        [Test]
        public void ShouldNotAcceptNonImplementorsOf_IConfigurationBuilder_ForOverride()
        {
            void Method()
            {
                var container = new WindsorContainer();

                container.AddFacility<NHibernateFacility>(
                    f => f.ConfigurationBuilder(GetType()));
            }

            Assert.Throws<FacilityException>(Method);
        }

        [Test]
        public void ShouldUse_DefaultSessionStore()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf<AsyncLocalSessionStore>());
        }

        [Test]
        public void ShouldOverride_DefaultSessionStore()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            // Starts with AsyncLocalSessionStore,
            // then change it to WebSessionStore,
            // then change it to LogicalCallContextSessionStore,
            // then change it to CallContextSessionStore,
            // and then finally change it to DummySessionStore.
            // The latest session store set should be DummySessionStore.
            container.AddFacility<NHibernateFacility>(
                f =>
                f.IsWeb()
#if NETFRAMEWORK
                 .SessionStore<LogicalCallContextSessionStore>()
                 .SessionStore<CallContextSessionStore>()
#endif
                 .SessionStore<DummySessionStore>()
                 .ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf<DummySessionStore>());
        }

        [Test]
        public void ShouldOverride_IsWeb()
        {
            var container = new WindsorContainer();

            container.AddFacility<AutoTxFacility>();

            container.AddFacility<NHibernateFacility>(
                f => f.IsWeb().ConfigurationBuilder<DummyConfigurationBuilder>());

            var sessionStore = container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf<WebSessionStore>());
        }

        [Test]
        public void ShouldBeAbleToResolve_ISessionManager()
        {
            var container = new WindsorContainer();

            container.AddFacility<NHibernateFacility>(
                f => f.ConfigurationBuilder<TestConfigurationBuilder>());

            var sessionManager = container.Resolve<ISessionManager>();

            sessionManager.OpenSession();

            Assert.That(container.Resolve<IConfigurationBuilder>().GetType(), Is.EqualTo(typeof(TestConfigurationBuilder)));
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
        public bool IsCurrentActivityEmptyFor(string? alias)
        {
            throw new NotImplementedException();
        }

        public SessionDelegate FindCompatibleSession(string? alias)
        {
            throw new NotImplementedException();
        }

        public void Store(string? alias, SessionDelegate session)
        {
            throw new NotImplementedException();
        }

        public void Remove(SessionDelegate session)
        {
            throw new NotImplementedException();
        }

        public StatelessSessionDelegate FindCompatibleStatelessSession(string? alias)
        {
            throw new NotImplementedException();
        }

        public void Store(string? alias, StatelessSessionDelegate session)
        {
            throw new NotImplementedException();
        }

        public void Remove(StatelessSessionDelegate session)
        {
            throw new NotImplementedException();
        }
    }
}
