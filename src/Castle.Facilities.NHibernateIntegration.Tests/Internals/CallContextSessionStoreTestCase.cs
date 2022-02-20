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
	using NHibernate;

	using NUnit.Framework;

	using System;
	using System.Threading;

	public class CallContextSessionStoreTestCase : AbstractNHibernateTestCase
	{
		private readonly AutoResetEvent _event = new AutoResetEvent(false);

		protected override string ConfigurationFile
		{
			get { return "Internals/CallContextSessionStoreConfiguration.xml"; }
		}

		[Test]
		public void NullAlias()
		{
			var store = Container.Resolve<ISessionStore>();
			Assert.Throws<ArgumentNullException>(() => store.FindCompatibleSession(null));
		}

		[Test]
		public void FindCompatibleSession()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			ISession session = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNull(session);

			session = factory.OpenSession();

			var sessDelegate = new SessionDelegate(true, session, store);

			store.Store(Constants.DefaultAlias, sessDelegate);

			Assert.IsNotNull(sessDelegate.SessionStoreCookie);

			ISession session2 = store.FindCompatibleSession("something in the way she moves");

			Assert.IsNull(session2);

			session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessDelegate, session2);

			session.Dispose();

			store.Remove(sessDelegate);

			session = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNull(session);

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void FindCompatibleSessionWithTwoThreads()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			var session = factory.OpenSession();

			var sessDelegate = new SessionDelegate(true, session, store);

			store.Store(Constants.DefaultAlias, sessDelegate);

			ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessDelegate, session2);

			var newThread = new Thread(FindCompatibleSessionOnOtherThread);
			newThread.Start();

			_event.WaitOne();

			sessDelegate.Dispose();

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		private void FindCompatibleSessionOnOtherThread()
		{
			var store = Container.Resolve<ISessionStore>();

			ISession session = store.FindCompatibleSession("something in the way she moves");

			Assert.IsNull(session);

			ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNull(session2);

			_event.Set();
		}

		[Test]
		public void NullAliasStateless()
		{
			var store = Container.Resolve<ISessionStore>();
			Assert.Throws<ArgumentNullException>(() => store.FindCompatibleStatelessSession(null));
		}

		[Test]
		public void FindCompatibleStatelessSession()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			IStatelessSession session = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNull(session);

			session = factory.OpenStatelessSession();

			var sessionDelegate = new StatelessSessionDelegate(true, session, store);

			store.Store(Constants.DefaultAlias, sessionDelegate);

			Assert.IsNotNull(sessionDelegate.SessionStoreCookie);

			IStatelessSession session2 = store.FindCompatibleStatelessSession("something in the way she moves");

			Assert.IsNull(session2);

			session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate, session2);

			session.Dispose();

			store.Remove(sessionDelegate);

			session = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNull(session);

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void FindCompatibleStatelessSessionWithTwoThreads()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			var session = factory.OpenStatelessSession();

			var sessionDelegate = new StatelessSessionDelegate(true, session, store);

			store.Store(Constants.DefaultAlias, sessionDelegate);

			IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate, session2);

			var newThread = new Thread(FindCompatibleStatelessSessionOnOtherThread);
			newThread.Start();

			_event.WaitOne();

			sessionDelegate.Dispose();

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		private void FindCompatibleStatelessSessionOnOtherThread()
		{
			var store = Container.Resolve<ISessionStore>();

			IStatelessSession session = store.FindCompatibleStatelessSession("something in the way she moves");

			Assert.IsNull(session);

			IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNull(session2);

			_event.Set();
		}
	}
}