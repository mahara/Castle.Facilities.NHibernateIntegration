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

using Castle.Facilities.NHibernateIntegration.Tests.Common;
using Castle.MicroKernel.Facilities;
using Castle.Services.Transaction;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
    [TestFixture]
    public class SessionManagerTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFilePath =>
            "Internals/TwoDatabasesConfiguration.xml";

        [Test]
        public void InterceptedSessionByConfiguration()
        {
            var manager = Container.Resolve<ISessionManager>();

            var sessionAlias = "intercepted";

            var session = manager.OpenSession(sessionAlias);

            var order = new Order { Value = 9.3d };
            session.SaveOrUpdate(order);
            session.Close();

            session = manager.OpenSession(sessionAlias);

            session.Get<Order>(1);
            session.Close();

            var interceptor = Container.Resolve<TestInterceptor>(string.Format(Constants.SessionInterceptor_ComponentNameFormat, sessionAlias));

            Assert.That(interceptor, Is.Not.Null);
            Assert.That(interceptor.ConfirmInstantiationCall());
            Assert.That(interceptor.ConfirmOnSaveCall());

            interceptor.ResetState();
        }

        [Test]
        public void NonInterceptedSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var sessionAlias = "db2";

            var session = manager.OpenSession(sessionAlias);

            var order = new Order { Value = 9.3d };
            session.SaveOrUpdate(order);
            session.Close();

            session = manager.OpenSession(sessionAlias);

            session.Get<Order>(1);
            session.Close();

            var interceptor = Container.Resolve<TestInterceptor>(string.Format(Constants.SessionInterceptor_ComponentNameFormat, "intercepted"));

            Assert.That(interceptor, Is.Not.Null);
            Assert.That(interceptor.ConfirmInstantiationCall(), Is.False);
            Assert.That(interceptor.ConfirmOnSaveCall(), Is.False);

            interceptor.ResetState();
        }

        [Test]
        public void NonExistentAlias()
        {
            var manager = Container.Resolve<ISessionManager>();

            Assert.Throws<FacilityException>(
                () => manager.OpenSession("something in the way she moves"));
        }

        [Test]
        public void SharedSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenSession();
            var session2 = manager.OpenSession();
            var session3 = manager.OpenSession();

            Assert.That(session1, Is.Not.Null);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session3, Is.Not.Null);
            Assert.That(SessionDelegate.AreEqual(session1, session2));
            Assert.That(SessionDelegate.AreEqual(session1, session3));

            session3.Dispose();
            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction takes ownership of the session,
        /// and disposes it at the end of the transaction.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            var session = sessionManager.OpenSession();

            Assert.That(session, Is.Not.Null);

            var currentTransaction = session.Transaction;

            Assert.That(currentTransaction, Is.Not.Null);
            Assert.That(currentTransaction.IsActive);

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session.Transaction.WasCommitted);
            //Assert.That(session.IsConnected);

            session.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// In this case, the transaction should not take ownership of the session
        /// (not disposing it at the end of the transaction).
        /// </summary>
        [Test]
        public void NewTransactionAfterUsingSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var session1 = sessionManager.OpenSession();

            Assert.That(session1, Is.Not.Null);

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            // Nested sessions.
            using (var session2 = sessionManager.OpenSession())
            {
                Assert.That(session2, Is.Not.Null);
                Assert.That(session1, Is.Not.Null);

                var currentTransaction1 = session1.Transaction;

                Assert.That(currentTransaction1, Is.Not.Null,
                            "After requesting compatible session, first session is enlisted in transaction too.");
                Assert.That(currentTransaction1.IsActive,
                            "After requesting compatible session, first session is enlisted in transaction too.");

                using (var session3 = sessionManager.OpenSession())
                {
                    Assert.That(session3, Is.Not.Null);

                    var currentTransaction3 = session3.Transaction;

                    Assert.That(currentTransaction3, Is.Not.Null);
                    Assert.That(currentTransaction3.IsActive);
                }

                var delegate1 = (SessionDelegate) session1;
                var delegate2 = (SessionDelegate) session2;

                Assert.That(delegate2.InnerSession, Is.SameAs(delegate1.InnerSession));
            }

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            Assert.That(session1.IsConnected);

            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void TwoDatabases()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenSession();
            var session2 = manager.OpenSession("db2");

            Assert.That(session1, Is.Not.Null);
            Assert.That(session2, Is.Not.Null);
            Assert.That(ReferenceEquals(session1, session2), Is.False);

            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction enlists
        /// the sessions of both database connections.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingSessionWithTwoDatabases()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            var session1 = sessionManager.OpenSession();

            Assert.That(session1, Is.Not.Null);

            var currentTransaction1 = session1.Transaction;

            Assert.That(currentTransaction1, Is.Not.Null);
            Assert.That(currentTransaction1.IsActive);

            var session2 = sessionManager.OpenSession("db2");

            Assert.That(session2, Is.Not.Null);

            var currentTransaction2 = session2.Transaction;

            Assert.That(currentTransaction2, Is.Not.Null);
            Assert.That(currentTransaction2.IsActive);

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            //Assert.That(session1.IsConnected);
            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session2.Transaction.WasCommitted);
            //Assert.That(session2.IsConnected);

            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the session is enlisted in actual transaction only once
        /// for second database session.
        /// </summary>
        [Test]
        public void SecondDatabaseSessionEnlistedOnlyOnceInActualTransaction()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            // Open connection to first database and enlist session in running transaction.
            var session1 = sessionManager.OpenSession();

            Assert.That(session1, Is.Not.Null);

            // Open connection to second database and enlist session in running transaction.
            using (var session2 = sessionManager.OpenSession("db2"))
            {
                Assert.That(session2, Is.Not.Null);

                var currentTransaction2 = session2.Transaction;

                Assert.That(currentTransaction2, Is.Not.Null);
                Assert.That(currentTransaction2.IsActive);
            }
            // "Real" NHibernate session2 was not disposed because its in active transaction.

            // Request compatible session for db2 -->
            // we must get existing NHibernate session to db2 which should be already enlisted in active transaction.
            using (var session3 = sessionManager.OpenSession("db2"))
            {
                Assert.That(session3, Is.Not.Null);

                var currentTransaction3 = session3.Transaction;

                Assert.That(currentTransaction3, Is.Not.Null);
                Assert.That(currentTransaction3.IsActive);
            }

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            //Assert.That(session1.IsConnected);

            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void NonExistentAliasStateless()
        {
            var manager = Container.Resolve<ISessionManager>();

            Assert.Throws<FacilityException>(
                () => manager.OpenStatelessSession("something in the way she moves"));
        }

        [Test]
        public void SharedStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenStatelessSession();
            var session2 = manager.OpenStatelessSession();
            var session3 = manager.OpenStatelessSession();

            Assert.That(session1, Is.Not.Null);
            Assert.That(session2, Is.Not.Null);
            Assert.That(session3, Is.Not.Null);

            Assert.That(StatelessSessionDelegate.AreEqual(session1, session2));
            Assert.That(StatelessSessionDelegate.AreEqual(session1, session3));

            session3.Dispose();
            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction takes ownership of the session,
        /// and disposes it at the end of the transaction.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingStatelessSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            var session = sessionManager.OpenStatelessSession();

            Assert.That(session, Is.Not.Null);

            var currentTransaction = session.Transaction;

            Assert.That(currentTransaction, Is.Not.Null);
            Assert.That(currentTransaction.IsActive);

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session.Transaction.WasCommitted);
            //Assert.That(session.IsConnected);

            session.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// In this case the transaction should not take ownership of the session
        /// (not disposing it at the end of the transaction).
        /// </summary>
        [Test]
        public void NewTransactionAfterUsingStatelessSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var session1 = sessionManager.OpenStatelessSession();

            Assert.That(session1, Is.Not.Null);

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            // Nested sessions.
            using (var session2 = sessionManager.OpenStatelessSession())
            {
                Assert.That(session2, Is.Not.Null);
                Assert.That(session1, Is.Not.Null);

                var currentTransaction1 = session1.Transaction;

                Assert.That(currentTransaction1, Is.Not.Null,
                            "After requesting compatible session, first session is enlisted in transaction too.");
                Assert.That(currentTransaction1.IsActive,
                            "After requesting compatible session, first session is enlisted in transaction too.");

                using (var session3 = sessionManager.OpenStatelessSession())
                {
                    Assert.That(session3, Is.Not.Null);

                    var currentTransaction3 = session3.Transaction;

                    Assert.That(currentTransaction3, Is.Not.Null);
                    Assert.That(currentTransaction3.IsActive);
                }

                var delegate1 = (StatelessSessionDelegate) session1;
                var delegate2 = (StatelessSessionDelegate) session2;

                Assert.That(delegate2.InnerSession, Is.SameAs(delegate1.InnerSession));
            }

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            Assert.That(session1.IsConnected);

            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void TwoDatabasesStateless()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenStatelessSession();
            var session2 = manager.OpenStatelessSession("db2");

            Assert.That(session1, Is.Not.Null);
            Assert.That(session2, Is.Not.Null);

            Assert.That(ReferenceEquals(session1, session2), Is.False);

            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction enlists
        /// the sessions of both database connections.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingStatelessSessionWithTwoDatabases()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            var session1 = sessionManager.OpenStatelessSession();

            Assert.That(session1, Is.Not.Null);

            var currentTransaction1 = session1.Transaction;

            Assert.That(currentTransaction1, Is.Not.Null);
            Assert.That(currentTransaction1.IsActive);

            var session2 = sessionManager.OpenStatelessSession("db2");

            Assert.That(session2, Is.Not.Null);

            var currentTransaction2 = session2.Transaction;

            Assert.That(currentTransaction2, Is.Not.Null);
            Assert.That(currentTransaction2.IsActive);

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            //Assert.That(session1.IsConnected);
            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session2.Transaction.WasCommitted);
            //Assert.That(session2.IsConnected);

            session2.Dispose();
            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the session is enlisted in actual transaction only once
        /// for second database session.
        /// </summary>
        [Test]
        public void SecondDatabaseStatelessSessionEnlistedOnlyOnceInActualTransaction()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();

            var transaction = transactionManager.CreateTransaction(
                TransactionMode.Requires, IsolationMode.Serializable);

            transaction.Begin();

            // Open connection to first database and enlist session in running transaction.
            var session1 = sessionManager.OpenStatelessSession();

            Assert.That(session1, Is.Not.Null);

            // Open connection to second database and enlist session in running transaction.
            using (var session2 = sessionManager.OpenStatelessSession("db2"))
            {
                Assert.That(session2, Is.Not.Null);

                var currentTransaction2 = session2.Transaction;

                Assert.That(currentTransaction2, Is.Not.Null);
                Assert.That(currentTransaction2.IsActive);
            }
            // "Real" NHibernate session2 was not disposed because its in active transaction.

            // Request compatible session for db2 -->
            // we must get existing NHibernate session to db2 which should be already enlisted in active transaction.
            using (var session3 = sessionManager.OpenStatelessSession("db2"))
            {
                Assert.That(session3, Is.Not.Null);

                var currentTransaction3 = session3.Transaction;

                Assert.That(currentTransaction3, Is.Not.Null);
                Assert.That(currentTransaction3.IsActive);
            }

            transaction.Commit();

            //
            //  TODO:   Assert transaction was committed.
            //
            //Assert.That(session1.Transaction.WasCommitted);
            //Assert.That(session1.IsConnected);

            session1.Dispose();

            Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }
    }
}
