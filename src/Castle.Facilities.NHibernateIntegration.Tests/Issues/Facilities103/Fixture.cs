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

    using NHibernate;

    using NUnit.Framework;

    using Rhino.Mocks;

    using ITransaction = Services.Transaction.ITransaction;

    [TestFixture]
    public class DefaultSessionManagerTestCase : IssueTestCase
    {
        protected override string ConfigurationFile =>
            "EmptyConfiguration.xml";

        public override void OnSetUp()
        {
            _sessionStore = new CallContextSessionStore();
            _kernel = MockRepository.DynamicMock<IKernel>();
            _factoryResolver = MockRepository.DynamicMock<ISessionFactoryResolver>();
            _transactionManager = MockRepository.DynamicMock<ITransactionManager>();
            _transaction = MockRepository.DynamicMock<ITransaction>();
            _sessionFactory = MockRepository.DynamicMock<ISessionFactory>();
            _session = MockRepository.DynamicMock<ISession>();
            _statelessSession = MockRepository.DynamicMock<IStatelessSession>();
            _contextDictionary = new Hashtable();
            _sessionManager = new DefaultSessionManager(_sessionStore, _kernel, _factoryResolver);
        }

        private const string Alias = "myAlias";
        private const string InterceptorFormatString = DefaultSessionManager.InterceptorFormatString;
        private const string InterceptorName = DefaultSessionManager.InterceptorName;
        private const System.Transactions.IsolationLevel DefaultIsolationMode = System.Transactions.IsolationLevel.ReadUncommitted;
        private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadUncommitted;

        private ISessionStore _sessionStore;
        private IKernel _kernel;
        private ISessionFactoryResolver _factoryResolver;
        private ITransactionManager _transactionManager;
        private ITransaction _transaction;
        private ISessionFactory _sessionFactory;
        private ISession _session;
        private IStatelessSession _statelessSession;
        private IDictionary _contextDictionary;
        private ISessionManager _sessionManager;

        [Test]
        public void WhenBeginTransactionFailsSessionIsRemovedFromSessionStore()
        {
            using (MockRepository.Record())
            {
                Expect.Call(_kernel.Resolve<ITransactionManager>()).Return(_transactionManager);
                Expect.Call(_transactionManager.CurrentTransaction).Return(_transaction);
                Expect.Call(_factoryResolver.GetSessionFactory(Alias)).Return(_sessionFactory);
                Expect.Call(_kernel.HasComponent(string.Format(InterceptorFormatString, Alias))).Return(false);
                Expect.Call(_kernel.HasComponent(InterceptorName)).Return(false).Repeat.Any();
                Expect.Call(_sessionFactory.OpenSession()).Return(_session);
                _session.FlushMode = _sessionManager.DefaultFlushMode;
                Expect.Call(_transaction.Context).Return(_contextDictionary).Repeat.Any();
                Expect.Call(_transaction.IsolationMode).Return(DefaultIsolationMode).Repeat.Any();
                Expect.Call(_session.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
            }

            using (MockRepository.Playback())
            {
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

                Assert.IsNull(_sessionStore.FindCompatibleSession(Alias),
                              "The sessionStore shouldn't contain compatible session if the session creation fails");
            }
        }

        [Test]
        public void WhenBeginTransactionFailsStatelessSessionIsRemovedFromSessionStore()
        {
            using (MockRepository.Record())
            {
                Expect.Call(_kernel.Resolve<ITransactionManager>()).Return(_transactionManager);
                Expect.Call(_transactionManager.CurrentTransaction).Return(_transaction);
                Expect.Call(_factoryResolver.GetSessionFactory(Alias)).Return(_sessionFactory);
                Expect.Call(_sessionFactory.OpenStatelessSession()).Return(_statelessSession);
                Expect.Call(_transaction.Context).Return(_contextDictionary).Repeat.Any();
                Expect.Call(_transaction.IsolationMode).Return(DefaultIsolationMode).Repeat.Any();
                Expect.Call(_statelessSession.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
            }

            using (MockRepository.Playback())
            {
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

                Assert.IsNull(_sessionStore.FindCompatibleStatelessSession(Alias),
                              "The sessionStore shouldn't contain compatible session if the session creation fails");
            }
        }
    }
}