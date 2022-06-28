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

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities103
{
    using System;
    using System.Collections;
    using System.Data;

    using Castle.Facilities.NHibernateIntegration.SessionStores;
    using Castle.MicroKernel;
    using Castle.Services.Transaction;

    using Moq;

    using NHibernate;

    using NUnit.Framework;

    using ITransaction = Services.Transaction.ITransaction;

    [TestFixture]
    public class DefaultSessionManagerTestCase : IssueTestCase
    {
        protected override string ConfigurationFile =>
            "EmptyConfiguration.xml";

        private const string Alias = "myAlias";
        private const string InterceptorFormatString = DefaultSessionManager.InterceptorFormatString;
        private const string InterceptorName = DefaultSessionManager.InterceptorName;
        private const System.Transactions.IsolationLevel DefaultTransactionIsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted;
        private const IsolationLevel DefaultDataIsolationLevel = IsolationLevel.ReadUncommitted;

        private IKernel _kernel;
        private ITransactionManager _transactionManager;
        private ITransaction _transaction;
        private ISessionStore _sessionStore;
        private ISessionFactoryResolver _factoryResolver;
        private ISessionFactory _sessionFactory;
        private ISessionManager _sessionManager;
        private ISession _session;
        private IStatelessSession _statelessSession;
        private IDictionary _contextDictionary;

        protected override void OnSetUp()
        {
            _sessionStore = new AsyncLocalSessionStore();
            _kernel = new Mock<IKernel>().Object;
            _factoryResolver = new Mock<ISessionFactoryResolver>().Object;
            _transactionManager = new Mock<ITransactionManager>().Object;
            _transaction = new Mock<ITransaction>().Object;
            _sessionFactory = new Mock<ISessionFactory>().Object;
            _session = new Mock<ISession>().Object;
            _statelessSession = new Mock<IStatelessSession>().Object;
            _contextDictionary = new Hashtable();
            _sessionManager = new DefaultSessionManager(_sessionStore, _kernel, _factoryResolver);
        }

        [Test]
        public void WhenBeginTransactionFailsSessionIsRemovedFromSessionStore()
        {
            Mock.Get(_kernel).Setup(x => x.Resolve<ITransactionManager>()).Returns(_transactionManager);
            Mock.Get(_transactionManager).Setup(x => x.CurrentTransaction).Returns(_transaction);
            Mock.Get(_factoryResolver).Setup(x => x.GetSessionFactory(Alias)).Returns(_sessionFactory);
            Mock.Get(_kernel).Setup(x => x.HasComponent(string.Format(InterceptorFormatString, Alias))).Returns(false);
            Mock.Get(_kernel).Setup(x => x.HasComponent(InterceptorName)).Returns(false);
            Mock.Get(_sessionFactory).Setup(x => x.OpenSession()).Returns(_session);
            _session.FlushMode = _sessionManager.DefaultFlushMode;
            Mock.Get(_transaction).Setup(x => x.Context).Returns(_contextDictionary);
            Mock.Get(_transaction).Setup(x => x.IsolationLevel).Returns(DefaultTransactionIsolationLevel);
            Mock.Get(_session).Setup(x => x.BeginTransaction(DefaultDataIsolationLevel)).Throws(new Exception());

            try
            {
                _sessionManager.OpenSession(Alias);

                Assert.Fail("DbException not thrown");
            }
            catch (Exception ex)
            {
                // Ignore
                Console.WriteLine(ex.ToString());
            }

            Assert.That(_sessionStore.FindCompatibleSession(Alias), Is.Null,
                          "The sessionStore shouldn't contain compatible session if the session creation fails.");
        }

        [Test]
        public void WhenBeginTransactionFailsStatelessSessionIsRemovedFromSessionStore()
        {
            Mock.Get(_kernel).Setup(x => x.Resolve<ITransactionManager>()).Returns(_transactionManager);
            Mock.Get(_transactionManager).Setup(x => x.CurrentTransaction).Returns(_transaction);
            Mock.Get(_factoryResolver).Setup(x => x.GetSessionFactory(Alias)).Returns(_sessionFactory);
            Mock.Get(_sessionFactory).Setup(x => x.OpenStatelessSession()).Returns(_statelessSession);
            Mock.Get(_transaction).Setup(x => x.Context).Returns(_contextDictionary);
            Mock.Get(_transaction).Setup(x => x.IsolationLevel).Returns(DefaultTransactionIsolationLevel);
            Mock.Get(_statelessSession).Setup(x => x.BeginTransaction(DefaultDataIsolationLevel)).Throws(new Exception());

            try
            {
                _sessionManager.OpenStatelessSession(Alias);

                Assert.Fail("DbException not thrown");
            }
            catch (Exception ex)
            {
                // Ignore
                Console.WriteLine(ex.ToString());
            }

            Assert.That(_sessionStore.FindCompatibleStatelessSession(Alias), Is.Null,
                        "The sessionStore shouldn't contain compatible session if the session creation fails.");
        }
    }
}