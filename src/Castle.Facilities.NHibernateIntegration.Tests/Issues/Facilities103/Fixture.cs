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

using System;
using System.Collections.Generic;
using System.Transactions;

using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.MicroKernel;
using Castle.Services.Transaction;

using NHibernate;

using NUnit.Framework;

using Rhino.Mocks;

using ITransaction = Castle.Services.Transaction.ITransaction;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities103
{
    [TestFixture]
    public class DefaultSessionManagerTestCase : IssueTestCase
    {
        private const string Alias = "myAlias";
        private const IsolationLevel DefaultIsolationMode = IsolationLevel.ReadCommitted;
        private const System.Data.IsolationLevel DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted;

        private IKernel _kernel;
        private IDictionary<string, object> _transactionContext;
        private ITransaction _transaction;
        private ITransactionManager _transactionManager;
        private ISessionFactoryResolver _sessionFactoryResolver;
        private ISessionFactory _sessionFactory;
        private ISessionStore _sessionStore;
        private ISessionManager _sessionManager;
        private ISession _session;
        private IStatelessSession _statelessSession;

        protected override string ConfigurationFilePath =>
            "EmptyConfiguration.xml";

        protected override void OnSetUp()
        {
            _kernel = MockRepository.DynamicMock<IKernel>();
            _transactionContext = new Dictionary<string, object>();
            _transaction = MockRepository.DynamicMock<ITransaction>();
            _transactionManager = MockRepository.DynamicMock<ITransactionManager>();
            _sessionFactoryResolver = MockRepository.DynamicMock<ISessionFactoryResolver>();
            _sessionFactory = MockRepository.DynamicMock<ISessionFactory>();
            _sessionStore = new AsyncLocalSessionStore();
            _sessionManager = new DefaultSessionManager(_kernel, _sessionFactoryResolver, _sessionStore);
            _session = MockRepository.DynamicMock<ISession>();
            _statelessSession = MockRepository.DynamicMock<IStatelessSession>();
        }

        [Test]
        public void WhenBeginTransactionFails_SessionIsRemovedFromSessionStore()
        {
            using (MockRepository.Record())
            {
                Expect.Call(_kernel.Resolve<ITransactionManager>()).Return(_transactionManager);
                Expect.Call(_transaction.Context).Return(_transactionContext).Repeat.Any();
                Expect.Call(_transaction.IsolationLevel).Return(DefaultIsolationMode).Repeat.Any();
                Expect.Call(_transactionManager.CurrentTransaction).Return(_transaction);
                Expect.Call(_sessionFactoryResolver.GetSessionFactory(Alias)).Return(_sessionFactory);
                Expect.Call(_kernel.HasComponent(string.Format(Constants.SessionInterceptor_ComponentNameFormat, Alias))).Return(false);
                Expect.Call(_kernel.HasComponent(Constants.SessionInterceptor_ComponentName)).Return(false).Repeat.Any();
                Expect.Call(_sessionFactory.OpenSession()).Return(_session);
                _session.FlushMode = _sessionManager.DefaultFlushMode;
                Expect.Call(_session.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
            }

            using (MockRepository.Playback())
            {
                try
                {
                    _sessionManager.OpenSession(Alias);

                    Assert.Fail("Exception not thrown.");
                }
                catch (Exception ex)
                {
                    // Expected.
                    Console.WriteLine(ex.ToString());
                }

                Assert.That(_sessionStore.FindCompatibleSession(Alias), Is.Null,
                            "The session store shouldn't contain compatible session if the session creation fails.");
            }
        }

        [Test]
        public void WhenBeginTransactionFails_StatelessSessionIsRemovedFromSessionStore()
        {
            using (MockRepository.Record())
            {
                Expect.Call(_kernel.Resolve<ITransactionManager>()).Return(_transactionManager);
                Expect.Call(_transaction.Context).Return(_transactionContext).Repeat.Any();
                Expect.Call(_transaction.IsolationLevel).Return(DefaultIsolationMode).Repeat.Any();
                Expect.Call(_transactionManager.CurrentTransaction).Return(_transaction);
                Expect.Call(_sessionFactoryResolver.GetSessionFactory(Alias)).Return(_sessionFactory);
                Expect.Call(_sessionFactory.OpenStatelessSession()).Return(_statelessSession);
                Expect.Call(_statelessSession.BeginTransaction(DefaultIsolationLevel)).Throw(new Exception());
            }

            using (MockRepository.Playback())
            {
                try
                {
                    _sessionManager.OpenStatelessSession(Alias);

                    Assert.Fail("Exception not thrown.");
                }
                catch (Exception ex)
                {
                    // Expected.
                    Console.WriteLine(ex.ToString());
                }

                Assert.That(_sessionStore.FindCompatibleStatelessSession(Alias), Is.Null,
                            "The session store shouldn't contain compatible stateless session if the session creation fails.");
            }
        }
    }
}
