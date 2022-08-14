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

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
#if NETFRAMEWORK
    using System;
    using System.Threading;

    using Castle.Facilities.NHibernateIntegration.SessionStores;

    using NHibernate;

    using NUnit.Framework;

    public class CallContextSessionStoreTestCase : AbstractNHibernateTestCase
    {
        private readonly AutoResetEvent _event = new(false);

        protected override string ConfigurationFile =>
            "Internals/CallContextSessionStoreConfiguration.xml";

        [Test]
        public void ShouldUseCallContextSessionStore()
        {
            var sessionStore = Container.Resolve<ISessionStore>();

            Assert.That(sessionStore, Is.InstanceOf(typeof(CallContextSessionStore)));
        }

        [Test]
        public void NoSessionWithNullAlias()
        {
            var store = Container.Resolve<ISessionStore>();
            Assert.Throws<ArgumentNullException>(() => store.FindCompatibleSession(null));
        }

        [Test]
        public void FindCompatibleSession()
        {
            var store = Container.Resolve<ISessionStore>();
            var factory = Container.Resolve<ISessionFactory>();

            ISession session1 = store.FindCompatibleSession(Constants.DefaultAlias);
            Assert.That(session1, Is.Null);

            session1 = factory.OpenSession();
            var sessionDelegate1 = new SessionDelegate(session1, store, true);
            store.Store(Constants.DefaultAlias, sessionDelegate1);
            Assert.That(sessionDelegate1.SessionStoreCookie, Is.Not.Null);

            ISession session2 = store.FindCompatibleSession("something in the way she moves");
            Assert.That(session2, Is.Null);

            session2 = store.FindCompatibleSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            session1.Dispose();

            store.Remove(sessionDelegate1);

            session1 = store.FindCompatibleSession(Constants.DefaultAlias);
            Assert.That(session1, Is.Null);

            Assert.That(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
        }

        [Test]
        public void FindCompatibleSessionWithTwoThreads()
        {
            var store = Container.Resolve<ISessionStore>();
            var factory = Container.Resolve<ISessionFactory>();

            var session1 = factory.OpenSession();
            var sessionDelegate1 = new SessionDelegate(session1, store, true);
            store.Store(Constants.DefaultAlias, sessionDelegate1);
            ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            var newThread = new Thread(FindCompatibleSessionOnOtherThread);
            newThread.Start();

            _event.WaitOne();

            sessionDelegate1.Dispose();

            Assert.That(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
        }

        private void FindCompatibleSessionOnOtherThread()
        {
            var store = Container.Resolve<ISessionStore>();

            ISession session1 = store.FindCompatibleSession("something in the way she moves");
            Assert.That(session1, Is.Null);

            ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Null);

            _event.Set();
        }

        [Test]
        public void NullAliasStatelessSession()
        {
            var store = Container.Resolve<ISessionStore>();

            Assert.Throws<ArgumentNullException>(() => store.FindCompatibleStatelessSession(null));
        }

        [Test]
        public void FindCompatibleStatelessSession()
        {
            var store = Container.Resolve<ISessionStore>();
            var factory = Container.Resolve<ISessionFactory>();

            IStatelessSession session1 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);
            Assert.That(session1, Is.Null);

            session1 = factory.OpenStatelessSession();
            var sessionDelegate1 = new StatelessSessionDelegate(session1, store, true);
            store.Store(Constants.DefaultAlias, sessionDelegate1);
            Assert.That(sessionDelegate1.SessionStoreCookie, Is.Not.Null);

            IStatelessSession session2 = store.FindCompatibleStatelessSession("something in the way she moves");
            Assert.That(session2, Is.Null);

            session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            session1.Dispose();

            store.Remove(sessionDelegate1);

            session1 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);
            Assert.That(session1, Is.Null);

            Assert.That(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
        }

        [Test]
        public void FindCompatibleStatelessSessionWithTwoThreads()
        {
            var store = Container.Resolve<ISessionStore>();
            var factory = Container.Resolve<ISessionFactory>();

            var session1 = factory.OpenStatelessSession();
            var sessionDelegate1 = new StatelessSessionDelegate(session1, store, true);
            store.Store(Constants.DefaultAlias, sessionDelegate1);

            IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2, Is.SameAs(sessionDelegate1));

            var newThread = new Thread(FindCompatibleStatelessSessionOnOtherThread);
            newThread.Start();

            _event.WaitOne();

            sessionDelegate1.Dispose();

            Assert.That(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
        }

        private void FindCompatibleStatelessSessionOnOtherThread()
        {
            var store = Container.Resolve<ISessionStore>();

            IStatelessSession session1 = store.FindCompatibleStatelessSession("something in the way she moves");
            Assert.That(session1, Is.Null);

            IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);
            Assert.That(session2, Is.Null);

            _event.Set();
        }
    }
#endif
}