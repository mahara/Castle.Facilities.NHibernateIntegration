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

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals;

using Castle.Facilities.NHibernateIntegration.Tests.Common;
using Castle.MicroKernel.Facilities;
using Castle.Services.Transaction;

using NHibernate;

using NUnit.Framework;

[TestFixture]
public class SessionManagerTestCase : AbstractNHibernateTestCase
{
    protected override string ConfigurationFile =>
        "Internal/TwoDatabaseConfiguration.xml";

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
        Assert.That(interceptor, Is.Not.Null);
        Assert.That(interceptor.ConfirmOnSaveCall(), Is.True);
        Assert.That(interceptor.ConfirmInstantiationCall(), Is.True);

        interceptor.ResetState();
    }

    /// <summary>
    /// In this case the transaction should not take ownership of the session
    /// (not disposing it at the end of the transaction).
    /// </summary>
    [Test]
    public void NewTransactionAfterUsingSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        var session1 = manager.OpenSession();

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        // Nested
        using (var session2 = manager.OpenSession())
        {
            Assert.That(session2, Is.Not.Null);
            Assert.That(session1, Is.Not.Null);

            var tx1 = session1.GetCurrentTransaction();
            Assert.That(tx1, Is.Not.Null,
                        "After requesting compatible session, first session is enlisted in transaction too.");
            Assert.That(tx1.IsActive, Is.True,
                        "After requesting compatible session, first session is enlisted in transaction too.");

            using (var session3 = manager.OpenSession())
            {
                Assert.That(session3, Is.Not.Null);

                var tx3 = session3.GetCurrentTransaction();
                Assert.That(tx3, Is.Not.Null);
                Assert.That(tx3.IsActive, Is.True);
            }

            var sessionDelegate1 = (SessionDelegate) session1;
            var sessionDelegate2 = (SessionDelegate) session2;
            Assert.That(sessionDelegate2.InnerSession, Is.SameAs(sessionDelegate1.InnerSession));
        }

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);
        Assert.That(session1.IsConnected, Is.True);

        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
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

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        // Nested
        using (var session2 = manager.OpenStatelessSession())
        {
            Assert.That(session2, Is.Not.Null);
            Assert.That(session1, Is.Not.Null);

            var tx1 = session1.GetCurrentTransaction();
            Assert.That(tx1, Is.Not.Null,
                        "After requesting compatible session, first session is enlisted in transaction too.");
            Assert.That(tx1.IsActive, Is.True,
                        "After requesting compatible session, first session is enlisted in transaction too.");

            using (var session3 = manager.OpenSession())
            {
                Assert.That(session3, Is.Not.Null);

                var tx3 = session3.GetCurrentTransaction();
                Assert.That(tx3, Is.Not.Null);
                Assert.That(tx3.IsActive, Is.True);
            }

            var sessionDelegate1 = (StatelessSessionDelegate) session1;
            var sessionDelegate2 = (StatelessSessionDelegate) session2;
            Assert.That(sessionDelegate2.InnerSession, Is.SameAs(sessionDelegate1.InnerSession));
        }

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);
        Assert.That(session1.IsConnected, Is.True);

        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    /// <summary>
    /// This test ensures that the transaction takes ownership of the session
    /// and disposes it at the end of the transaction.
    /// </summary>
    [Test]
    public void NewTransactionBeforeUsingSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        var session = manager.OpenSession();

        Assert.That(session, Is.Not.Null);
        Assert.That(session.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    /// <summary>
    /// This test ensures that the transaction enlists the sessions of both database connections.
    /// </summary>
    [Test]
    public void NewTransactionBeforeUsingSessionWithTwoDatabases()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();
        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);

        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        var session1 = manager.OpenSession();

        Assert.That(session1, Is.Not.Null);
        Assert.That(session1.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session1.IsConnected, Is.True);

        var session2 = manager.OpenSession("db2");

        Assert.That(session2, Is.Not.Null);
        Assert.That(session2.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session2.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    /// <summary>
    /// This test ensures that the transaction takes ownership of the session
    /// and disposes it at the end of the transaction.
    /// </summary>
    [Test]
    public void NewTransactionBeforeUsingStatelessSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();
        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);

        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        var session = manager.OpenStatelessSession();

        Assert.That(session, Is.Not.Null);
        Assert.That(session.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    /// <summary>
    /// This test ensures that the transaction enlists the sessions of both database connections.
    /// </summary>
    [Test]
    public void NewTransactionBeforeUsingStatelessSessionWithTwoDatabases()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        var session1 = manager.OpenStatelessSession();

        Assert.That(session1, Is.Not.Null);
        Assert.That(session1.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session1.IsConnected, Is.True);

        var session2 = manager.OpenStatelessSession("db2");

        Assert.That(session2, Is.Not.Null);
        Assert.That(session2.GetCurrentTransaction(), Is.Not.Null);
        Assert.That(session2.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    [Test]
    public void NonExistentAliasSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        Assert.Throws<FacilityException>(
            () => manager.OpenSession("something in the way she moves"));
    }

    [Test]
    public void NonExistentAliasStatelessSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        Assert.Throws<FacilityException>(
            () => manager.OpenStatelessSession("something in the way she moves"));
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

        Assert.That(interceptor, Is.Not.Null);
        Assert.That(interceptor.ConfirmOnSaveCall(), Is.False);
        Assert.That(interceptor.ConfirmInstantiationCall(), Is.False);

        interceptor.ResetState();
    }

    /// <summary>
    /// This test ensures that the session is enlisted only once
    /// in actual transaction for second database session.
    /// </summary>
    [Test]
    public void SecondDatabaseSessionEnlistedOnlyOnceInActualTransaction()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        // Open connection to first database and enlist session in running transaction.
        var session1 = manager.OpenSession();

        // Open connection to second database and enlist session in running transaction.
        using (var session2 = manager.OpenSession("db2"))
        {
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2.GetCurrentTransaction(), Is.Not.Null);
        }
        // "real" NH session2 was not disposed because its in active transaction.

        // Request compatible session for db2 --> we must get existing NH session to db2 which should be already enlisted in active transaction.
        using (var session3 = manager.OpenSession("db2"))
        {
            Assert.That(session3, Is.Not.Null);

            var tx3 = session3.GetCurrentTransaction();
            Assert.That(tx3, Is.Not.Null);
            Assert.That(tx3.IsActive, Is.True);
        }

        Assert.That(session1.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    /// <summary>
    /// This test ensures that the session is enlisted only once
    /// in actual transaction for second database session.
    /// </summary>
    [Test]
    public void SecondDatabaseStatelessSessionEnlistedOnlyOnceInActualTransaction()
    {
        var manager = Container.Resolve<ISessionManager>();

        var txManager = Container.Resolve<ITransactionManager>();

        var tx = txManager.CreateTransaction(System.Transactions.TransactionScopeOption.Required,
                                             System.Transactions.IsolationLevel.Serializable);
        Assert.That(tx, Is.Not.Null);

        tx.Begin();

        // Open connection to first database and enlist session in running transaction.
        var session1 = manager.OpenStatelessSession();

        // Open connection to second database and enlist session in running transaction.
        using (var session2 = manager.OpenStatelessSession("db2"))
        {
            Assert.That(session2, Is.Not.Null);
            Assert.That(session2.GetCurrentTransaction(), Is.Not.Null);
        }
        // "real" NH session2 was not disposed because its in active transaction.

        // Request compatible session for db2 --> we must get existing NH session to db2 which should be already enlisted in active transaction.
        using (var session3 = manager.OpenStatelessSession("db2"))
        {
            Assert.That(session3, Is.Not.Null);

            var tx3 = session3.GetCurrentTransaction();
            Assert.That(tx3, Is.Not.Null);
            Assert.That(tx3.IsActive, Is.True);
        }

        Assert.That(session1.IsConnected, Is.True);

        tx.Commit();

        Assert.That(tx.Status == TransactionStatus.Committed, Is.True);

        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
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
        Assert.That(SessionDelegate.AreEqual(session1, session2), Is.True);
        Assert.That(SessionDelegate.AreEqual(session1, session3), Is.True);

        session3.Dispose();
        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
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
        Assert.That(StatelessSessionDelegate.AreEqual(session1, session2), Is.True);
        Assert.That(StatelessSessionDelegate.AreEqual(session1, session3), Is.True);

        session3.Dispose();
        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    [Test]
    public void TwoDatabasesUsingSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        var session1 = manager.OpenSession();
        var session2 = manager.OpenSession("db2");

        Assert.That(session1, Is.Not.Null);
        Assert.That(session2, Is.Not.Null);
        Assert.That(ReferenceEquals(session1, session2), Is.False);

        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }

    [Test]
    public void TwoDatabasesUsingStatelessSession()
    {
        var manager = Container.Resolve<ISessionManager>();

        var session1 = manager.OpenStatelessSession();
        var session2 = manager.OpenStatelessSession("db2");

        Assert.That(session1, Is.Not.Null);
        Assert.That(session2, Is.Not.Null);
        Assert.That(ReferenceEquals(session1, session2), Is.False);

        session2.Dispose();
        session1.Dispose();

        Assert.That(Container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias), Is.True);
    }
}
