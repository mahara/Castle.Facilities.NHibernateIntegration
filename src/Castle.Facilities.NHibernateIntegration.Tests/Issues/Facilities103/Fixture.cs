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

using System.Transactions;

using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.MicroKernel;
using Castle.Services.Transaction;

using Moq;

using NHibernate;

using NUnit.Framework;

using ITransaction = Castle.Services.Transaction.ITransaction;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities103
{
    [TestFixture]
    public class DefaultSessionManagerTestCase : IssueTestCase
    {
        private const string Alias = "myAlias";
        private const IsolationLevel DefaultIsolationLevel = IsolationLevel.ReadCommitted;
        private const System.Data.IsolationLevel DefaultDataIsolationLevel = System.Data.IsolationLevel.ReadCommitted;

        private IKernel _kernel = null!;
        private IDictionary<string, object> _transactionContext = null!;
        private ITransaction _transaction = null!;
        private ITransactionManager _transactionManager = null!;
        private ISessionFactoryResolver _sessionFactoryResolver = null!;
        private ISessionFactory _sessionFactory = null!;
        private ISessionStore _sessionStore = null!;
        private ISessionManager _sessionManager = null!;
        private ISession _session = null!;
        private IStatelessSession _statelessSession = null!;

        protected override string ConfigurationFilePath =>
            "EmptyConfiguration.xml";

        protected override void OnSetUp()
        {
            _kernel = new Mock<IKernel>().Object;
            _transactionContext = new Dictionary<string, object>();
            _transaction = new Mock<ITransaction>().Object;
            _transactionManager = new Mock<ITransactionManager>().Object;
            _sessionFactoryResolver = new Mock<ISessionFactoryResolver>().Object;
            _sessionFactory = new Mock<ISessionFactory>().Object;
            _sessionStore = new AsyncLocalSessionStore();
            _sessionManager = new DefaultSessionManager(_kernel, _sessionFactoryResolver, _sessionStore);
            _session = new Mock<ISession>().Object;
            _statelessSession = new Mock<IStatelessSession>().Object;
        }

        [Test]
        public void WhenBeginTransactionFails_SessionIsRemovedFromSessionStore()
        {
            Mock.Get(_kernel).Setup(x => x.Resolve<ITransactionManager>()).Returns(_transactionManager);
            Mock.Get(_transaction).Setup(x => x.Context).Returns(_transactionContext);
            Mock.Get(_transaction).Setup(x => x.IsolationLevel).Returns(DefaultIsolationLevel);
            Mock.Get(_transactionManager).Setup(x => x.CurrentTransaction).Returns(_transaction);
            Mock.Get(_sessionFactoryResolver).Setup(x => x.GetSessionFactory(Alias)).Returns(_sessionFactory);
            Mock.Get(_kernel).Setup(x => x.HasComponent(string.Format(Constants.SessionInterceptor_ComponentNameFormat, Alias))).Returns(false);
            Mock.Get(_kernel).Setup(x => x.HasComponent(Constants.SessionInterceptor_ComponentName)).Returns(false);
            Mock.Get(_sessionFactory).Setup(x => x.OpenSession()).Returns(_session);
            Mock.Get(_session).Setup(x => x.BeginTransaction(DefaultDataIsolationLevel)).Throws(new Exception());

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

        [Test]
        public void WhenBeginTransactionFails_StatelessSessionIsRemovedFromSessionStore()
        {
            Mock.Get(_kernel).Setup(x => x.Resolve<ITransactionManager>()).Returns(_transactionManager);
            Mock.Get(_transaction).Setup(x => x.Context).Returns(_transactionContext);
            Mock.Get(_transaction).Setup(x => x.IsolationLevel).Returns(DefaultIsolationLevel);
            Mock.Get(_transactionManager).Setup(x => x.CurrentTransaction).Returns(_transaction);
            Mock.Get(_sessionFactoryResolver).Setup(x => x.GetSessionFactory(Alias)).Returns(_sessionFactory);
            Mock.Get(_sessionFactory).Setup(x => x.OpenStatelessSession()).Returns(_statelessSession);
            Mock.Get(_statelessSession).Setup(x => x.BeginTransaction(DefaultDataIsolationLevel)).Throws(new Exception());

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
