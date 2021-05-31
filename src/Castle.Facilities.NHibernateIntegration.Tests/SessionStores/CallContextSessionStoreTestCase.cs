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

using System;
using System.Threading;

using Castle.Facilities.NHibernateIntegration.SessionStores;

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionStores
{
    /// <summary>
    /// Tests for the <see cref="CallContextSessionStore" />.
    /// </summary>
    [TestFixture]
    public class CallContextSessionStoreTestCase : AbstractNHibernateTestCase
    {
        private readonly AutoResetEvent _event = new(false);

        protected override string ConfigurationFilePath => "SessionStores/CallContextSessionStoreConfiguration.xml";

        [Test]
        public void NoSessionWithNullAlias()
        {
            var sessionStore = Container.Resolve<ISessionStore>();

            Assert.Throws<ArgumentNullException>(
                () => sessionStore.FindCompatibleSession(null));
        }

        [Test]
        public void FindCompatibleSession()
        {
            var sessionStore = Container.Resolve<ISessionStore>();
            var sessionFactory = Container.Resolve<ISessionFactory>();

            ISession session1 = sessionStore.FindCompatibleSession(Constants.DefaultAlias);

            Assert.That(session1, Is.Null);

            session1 = sessionFactory.OpenSession();
            var sessionDelegate1 = new SessionDelegate(session1, sessionStore, true);
            sessionStore.Store(Constants.DefaultAlias, sessionDelegate1);

            Assert.That(sessionDelegate1.SessionStoreCookie, Is.Not.Null);

            ISession session2 = sessionStore.FindCompatibleSession("something in the way she moves");

            Assert.That(session2, Is.Null);

            session2 = sessionStore.FindCompatibleSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            session1.Dispose();

            sessionStore.Remove(sessionDelegate1);

            session1 = sessionStore.FindCompatibleSession(Constants.DefaultAlias);

            Assert.That(session1, Is.Null);
            Assert.That(sessionStore.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void FindCompatibleSessionWithTwoThreads()
        {
            var sessionStore = Container.Resolve<ISessionStore>();
            var sessionFactory = Container.Resolve<ISessionFactory>();

            ISession session1 = sessionFactory.OpenSession();
            var sessionDelegate1 = new SessionDelegate(session1, sessionStore, true);
            sessionStore.Store(Constants.DefaultAlias, sessionDelegate1);

            ISession session2 = sessionStore.FindCompatibleSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            var newThread = new Thread(FindCompatibleSessionOnOtherThread);
            newThread.Start();

            _event.WaitOne();

            sessionDelegate1.Dispose();

            Assert.That(sessionStore.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        private void FindCompatibleSessionOnOtherThread()
        {
            var sessionStore = Container.Resolve<ISessionStore>();

            ISession session = sessionStore.FindCompatibleSession("something in the way she moves");

            Assert.That(session, Is.Null);

            ISession session2 = sessionStore.FindCompatibleSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Null);

            _event.Set();
        }

        [Test]
        public void NoStatelessSessionWithNullAlias()
        {
            var sessionStore = Container.Resolve<ISessionStore>();

            Assert.Throws<ArgumentNullException>(
                () => sessionStore.FindCompatibleStatelessSession(null));
        }

        [Test]
        public void FindCompatibleStatelessSession()
        {
            var sessionStore = Container.Resolve<ISessionStore>();
            var sessionFactory = Container.Resolve<ISessionFactory>();

            IStatelessSession session1 = sessionStore.FindCompatibleStatelessSession(Constants.DefaultAlias);

            Assert.That(session1, Is.Null);

            session1 = sessionFactory.OpenStatelessSession();
            var sessionDelegate1 = new StatelessSessionDelegate(session1, sessionStore, true);
            sessionStore.Store(Constants.DefaultAlias, sessionDelegate1);

            Assert.That(sessionDelegate1.SessionStoreCookie, Is.Not.Null);

            IStatelessSession session2 = sessionStore.FindCompatibleStatelessSession("something in the way she moves");

            Assert.That(session2, Is.Null);

            session2 = sessionStore.FindCompatibleStatelessSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            session1.Dispose();

            sessionStore.Remove(sessionDelegate1);

            session1 = sessionStore.FindCompatibleStatelessSession(Constants.DefaultAlias);

            Assert.That(session1, Is.Null);
            Assert.That(sessionStore.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void FindCompatibleStatelessSessionWithTwoThreads()
        {
            var sessionStore = Container.Resolve<ISessionStore>();
            var sessionFactory = Container.Resolve<ISessionFactory>();

            IStatelessSession session1 = sessionFactory.OpenStatelessSession();
            var sessionDelegate1 = new StatelessSessionDelegate(session1, sessionStore, true);
            sessionStore.Store(Constants.DefaultAlias, sessionDelegate1);

            IStatelessSession session2 = sessionStore.FindCompatibleStatelessSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            var newThread = new Thread(FindCompatibleStatelessSessionOnOtherThread);
            newThread.Start();

            _event.WaitOne();

            sessionDelegate1.Dispose();

            Assert.That(sessionStore.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        private void FindCompatibleStatelessSessionOnOtherThread()
        {
            var sessionStore = Container.Resolve<ISessionStore>();

            IStatelessSession session1 = sessionStore.FindCompatibleStatelessSession("something in the way she moves");

            Assert.That(session1, Is.Null);

            IStatelessSession session2 = sessionStore.FindCompatibleStatelessSession(Constants.DefaultAlias);

            Assert.That(session2, Is.Null);

            _event.Set();
        }
    }
}
