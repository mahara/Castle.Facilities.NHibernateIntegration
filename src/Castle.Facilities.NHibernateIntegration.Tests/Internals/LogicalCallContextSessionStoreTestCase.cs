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

	[TestFixture]
	public class LogicalCallContextSessionStoreTestCase : AbstractNHibernateTestCase
	{
		private readonly AutoResetEvent _event = new AutoResetEvent(false);

		[Test]
		public void NullAliasSession()
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

			Assert.IsNull(session1);

			session1 = factory.OpenSession();

			var sessionDelegate1 = new SessionDelegate(true, session1, store);

			store.Store(Constants.DefaultAlias, sessionDelegate1);

			Assert.IsNotNull(sessionDelegate1.SessionStoreCookie);

			ISession session2 = store.FindCompatibleSession("something in the way she moves");

			Assert.IsNull(session2);

			session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate1, session2);

			session1.Dispose();

			store.Remove(sessionDelegate1);

			session1 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNull(session1);

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void FindCompatibleSessionWithTwoThreads()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			var session1 = factory.OpenSession();

			var sessionDelegate1 = new SessionDelegate(true, session1, store);

			store.Store(Constants.DefaultAlias, sessionDelegate1);

			ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate1, session2);

			var newThread = new Thread(FindCompatibleSessionOnOtherThread);
			newThread.Start();

			_event.WaitOne();

			sessionDelegate1.Dispose();

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		private void FindCompatibleSessionOnOtherThread()
		{
			var store = Container.Resolve<ISessionStore>();

			ISession session1 = store.FindCompatibleSession("something in the way she moves");

			Assert.IsNull(session1);

			ISession session2 = store.FindCompatibleSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);

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

			Assert.IsNull(session1);

			session1 = factory.OpenStatelessSession();

			var sessionDelegate1 = new StatelessSessionDelegate(true, session1, store);

			store.Store(Constants.DefaultAlias, sessionDelegate1);

			Assert.IsNotNull(sessionDelegate1.SessionStoreCookie);

			IStatelessSession session2 = store.FindCompatibleStatelessSession("something in the way she moves");

			Assert.IsNull(session2);

			session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate1, session2);

			session1.Dispose();

			store.Remove(sessionDelegate1);

			session1 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNull(session1);

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void FindCompatibleStatelessSessionWithTwoThreads()
		{
			var store = Container.Resolve<ISessionStore>();
			var factory = Container.Resolve<ISessionFactory>();

			var session1 = factory.OpenStatelessSession();

			var sessionDelegate1 = new StatelessSessionDelegate(true, session1, store);

			store.Store(Constants.DefaultAlias, sessionDelegate1);

			IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);
			Assert.AreSame(sessionDelegate1, session2);

			var newThread = new Thread(FindCompatibleStatelessSessionOnOtherThread);
			newThread.Start();

			_event.WaitOne();

			sessionDelegate1.Dispose();

			Assert.IsTrue(store.IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		private void FindCompatibleStatelessSessionOnOtherThread()
		{
			var store = Container.Resolve<ISessionStore>();

			IStatelessSession session1 = store.FindCompatibleStatelessSession("something in the way she moves");

			Assert.IsNull(session1);

			IStatelessSession session2 = store.FindCompatibleStatelessSession(Constants.DefaultAlias);

			Assert.IsNotNull(session2);

			_event.Set();
		}
	}
}