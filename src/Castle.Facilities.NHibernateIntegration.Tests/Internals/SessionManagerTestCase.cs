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
    using Castle.Facilities.NHibernateIntegration.Tests.Common;
    using Castle.MicroKernel.Facilities;
    using Castle.Services.Transaction;

    using NHibernate;

    using NUnit.Framework;

    [TestFixture]
    public class SessionManagerTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFile =>
            "Internals/TwoDatabaseConfiguration.xml";

        [Test]
        public void InterceptedSessionByConfiguration()
        {
            var manager = Container.Resolve<ISessionManager>();

            var sessionAlias = "intercepted";

            var session = manager.OpenSession(sessionAlias);

            var order = new Order
            {
                Value = 9.3f
            };
            session.SaveOrUpdate(order);
            session.Close();

            session = manager.OpenSession(sessionAlias);

            session.Get(typeof(Order), 1);
            session.Close();

            var interceptor = Container.Resolve<TestInterceptor>("nhibernate.session.interceptor.intercepted");
            Assert.IsNotNull(interceptor);
            Assert.IsTrue(interceptor.ConfirmOnSaveCall());
            Assert.IsTrue(interceptor.ConfirmInstantiationCall());

            interceptor.ResetState();
        }

        /// <summary>
        /// In this case the transaction should not take ownership of the session
        /// (not disposing it at the end of the transaction).
        /// </summary>
        [Test]
        // [Ignore("This doesn't work with the NH 1.2 transaction property, needs to be fixed.")]
        public void NewTransactionAfterUsingSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenSession();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            // Nested
            using (var session2 = manager.OpenSession())
            {
                Assert.IsNotNull(session2);
                Assert.IsNotNull(session1);

                var transaction1 = session1.GetCurrentTransaction();
                Assert.IsNotNull(transaction1,
                                 "After requesting compatible session, first session is enlisted in transaction too.");
                Assert.IsTrue(transaction1.IsActive,
                              "After requesting compatible session, first session is enlisted in transaction too.");

                using (var session3 = manager.OpenSession())
                {
                    Assert.IsNotNull(session3);

                    var transaction3 = session3.GetCurrentTransaction();
                    Assert.IsNotNull(transaction3);
                    Assert.IsTrue(transaction3.IsActive);
                }

                var sessionDelegate1 = (SessionDelegate) session1;
                var sessionDelegate2 = (SessionDelegate) session2;

                Assert.AreSame(sessionDelegate1.InnerSession, sessionDelegate2.InnerSession);
            }

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);
            Assert.IsTrue(session1.IsConnected);

            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// In this case the transaction should not take ownership of the session
        /// (not dispose it at the end of the transaction).
        /// </summary>
        [Test]
        public void NewTransactionAfterUsingStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenStatelessSession();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            // Nested
            using (var session2 = manager.OpenStatelessSession())
            {
                Assert.IsNotNull(session2);
                Assert.IsNotNull(session1);

                var transaction1 = session1.GetCurrentTransaction();
                Assert.IsNotNull(transaction1,
                                 "After requesting compatible session, first session is enlisted in transaction too.");
                Assert.IsTrue(transaction1.IsActive,
                              "After requesting compatible session, first session is enlisted in transaction too.");

                using (var session3 = manager.OpenSession())
                {
                    Assert.IsNotNull(session3);

                    var transaction3 = session3.GetCurrentTransaction();
                    Assert.IsNotNull(transaction3);
                    Assert.IsTrue(transaction3.IsActive);
                }

                var sessionDelegate1 = (StatelessSessionDelegate) session1;
                var sessionDelegate2 = (StatelessSessionDelegate) session2;
                Assert.AreSame(sessionDelegate1.InnerSession, sessionDelegate2.InnerSession);
            }

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);
            Assert.IsTrue(session1.IsConnected);

            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction takes ownership of the session
        /// and disposes it at the end of the transaction.
        /// </summary>
        [Test]
        // [Ignore("This doesn't work with the NH 1.2 transaction property, needs to be fixed.")]
        public void NewTransactionBeforeUsingSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            var session = manager.OpenSession();
            Assert.IsNotNull(session);
            Assert.IsNotNull(session.GetCurrentTransaction());
            Assert.IsTrue(session.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction enlists the sessions of both database connections.
        /// </summary>
        [Test]
        //[Ignore("This doesn't work with the NH 1.2 transaction property, needs to be fixed.")]
        public void NewTransactionBeforeUsingSessionWithTwoDatabases()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            var session1 = manager.OpenSession();
            Assert.IsNotNull(session1);
            Assert.IsNotNull(session1.GetCurrentTransaction());
            Assert.IsTrue(session1.IsConnected);

            var session2 = manager.OpenSession("db2");
            Assert.IsNotNull(session2);
            Assert.IsNotNull(session2.GetCurrentTransaction());
            Assert.IsTrue(session2.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction takes ownership of the session
        /// and disposes it at the end of the transaction.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            var session = manager.OpenStatelessSession();
            Assert.IsNotNull(session);
            Assert.IsNotNull(session.GetCurrentTransaction());
            Assert.IsTrue(session.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the transaction enlists the sessions of both database connections.
        /// </summary>
        [Test]
        public void NewTransactionBeforeUsingStatelessSessionWithTwoDatabases()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            var session1 = manager.OpenStatelessSession();
            Assert.IsNotNull(session1);
            Assert.IsNotNull(session1.GetCurrentTransaction());
            Assert.IsTrue(session1.IsConnected);

            var session2 = manager.OpenStatelessSession("db2");
            Assert.IsNotNull(session2);
            Assert.IsNotNull(session2.GetCurrentTransaction());
            Assert.IsTrue(session2.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void NonExistentAliasSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            Assert.Throws<FacilityException>(() => manager.OpenSession("something in the way she moves"));
        }

        [Test]
        public void NonExistentAliasStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            Assert.Throws<FacilityException>(() => manager.OpenStatelessSession("something in the way she moves"));
        }

        [Test]
        public void NonInterceptedSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var sessionAlias = "db2";

            var session = manager.OpenSession(sessionAlias);
            var o = new Order
            {
                Value = 9.3f
            };
            session.SaveOrUpdate(o);
            session.Close();

            session = manager.OpenSession(sessionAlias);
            session.Get(typeof(Order), 1);
            session.Close();

            var interceptor = Container.Resolve<TestInterceptor>("nhibernate.session.interceptor.intercepted");
            Assert.IsNotNull(interceptor);
            Assert.IsFalse(interceptor.ConfirmOnSaveCall());
            Assert.IsFalse(interceptor.ConfirmInstantiationCall());

            interceptor.ResetState();
        }

        /// <summary>
        /// This test ensures that the session is enlisted only once
        /// in actual transaction for second database session.
        /// </summary>
        [Test]
        //[Ignore("This doesn't work with the NH 1.2 transaction property, needs to be fixed.")]
        public void SecondDatabaseSessionEnlistedOnlyOnceInActualTransaction()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            // Open connection to first database and enlist session in running transaction.
            var session1 = manager.OpenSession();

            // Open connection to second database and enlist session in running transaction.
            using (var session2 = manager.OpenSession("db2"))
            {
                Assert.IsNotNull(session2);
                Assert.IsNotNull(session2.GetCurrentTransaction());
            }
            // "real" NH session2 was not disposed because its in active transaction.

            // Request compatible session for db2 --> we must get existing NH session to db2 which should be already enlisted in active transaction.
            using (var session3 = manager.OpenSession("db2"))
            {
                Assert.IsNotNull(session3);

                var transaction3 = session3.GetCurrentTransaction();
                Assert.IsNotNull(transaction3);
                Assert.IsTrue(transaction3.IsActive);
            }

            Assert.IsTrue(session1.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        /// <summary>
        /// This test ensures that the session is enlisted only once
        /// in actual transaction for second database session.
        /// </summary>
        [Test]
        public void SecondDatabaseStatelessSessionEnlistedOnlyOnceInActualTransaction()
        {
            var manager = Container.Resolve<ISessionManager>();

            var transactionManager = Container.Resolve<ITransactionManager>();
            var transaction =
                transactionManager.CreateTransaction(TransactionMode.Requires,
                                                     IsolationMode.Serializable);

            transaction.Begin();

            // Open connection to first database and enlist session in running transaction.
            var session1 = manager.OpenStatelessSession();

            // Open connection to second database and enlist session in running transaction.
            using (var session2 = manager.OpenStatelessSession("db2"))
            {
                Assert.IsNotNull(session2);
                Assert.IsNotNull(session2.GetCurrentTransaction());
            }
            // "real" NH session2 was not disposed because its in active transaction.

            // Request compatible session for db2 --> we must get existing NH session to db2 which should be already enlisted in active transaction.
            using (var session3 = manager.OpenStatelessSession("db2"))
            {
                Assert.IsNotNull(session3);

                var transaction3 = session3.GetCurrentTransaction();
                Assert.IsNotNull(transaction3);
                Assert.IsTrue(transaction3.IsActive);
            }

            Assert.IsTrue(session1.IsConnected);

            transaction.Commit();

            Assert.IsTrue(transaction.Status == TransactionStatus.Committed);

            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void SharedSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenSession();
            var session2 = manager.OpenSession();
            var session3 = manager.OpenSession();

            Assert.IsNotNull(session1);
            Assert.IsNotNull(session2);
            Assert.IsNotNull(session3);
            Assert.IsTrue(SessionDelegate.AreEqual(session1, session2));
            Assert.IsTrue(SessionDelegate.AreEqual(session1, session3));

            session3.Dispose();
            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void SharedStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenStatelessSession();
            var session2 = manager.OpenStatelessSession();
            var session3 = manager.OpenStatelessSession();

            Assert.IsNotNull(session1);
            Assert.IsNotNull(session2);
            Assert.IsNotNull(session3);
            Assert.IsTrue(StatelessSessionDelegate.AreEqual(session1, session2));
            Assert.IsTrue(StatelessSessionDelegate.AreEqual(session1, session3));

            session3.Dispose();
            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void TwoDatabasesUsingSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenSession();
            var session2 = manager.OpenSession("db2");

            Assert.IsNotNull(session1);
            Assert.IsNotNull(session2);
            Assert.IsFalse(ReferenceEquals(session1, session2));

            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }

        [Test]
        public void TwoDatabasesUsingStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            var session1 = manager.OpenStatelessSession();
            var session2 = manager.OpenStatelessSession("db2");

            Assert.IsNotNull(session1);
            Assert.IsNotNull(session2);
            Assert.IsFalse(ReferenceEquals(session1, session2));

            session2.Dispose();
            session1.Dispose();

            Assert.IsTrue(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
        }
    }
}