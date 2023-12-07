#region License
// Copyright 2004-2023 Castle Project - https://www.castleproject.org/
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

using Castle.Facilities.NHibernateIntegration.Internals;
using Castle.Facilities.NHibernateIntegration.Utilities;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.Services.Transaction;

using NHibernate;

using ITransaction = Castle.Services.Transaction.ITransaction;

namespace Castle.Facilities.NHibernateIntegration
{
    public class DefaultSessionManager : MarshalByRefObject, ISessionManager
    {
        private readonly IKernel _kernel;
        private readonly ITransactionManager _transactionManager;
        private readonly ISessionFactoryResolver _sessionFactoryResolver;
        private readonly ISessionStore _sessionStore;

        public DefaultSessionManager(
            IKernel kernel,
            ITransactionManager transactionManager,
            ISessionFactoryResolver sessionFactoryResolver,
            ISessionStore sessionStore)
        {
            _kernel = kernel;
            _transactionManager = transactionManager;
            _sessionFactoryResolver = sessionFactoryResolver;
            _sessionStore = sessionStore;
        }

        public FlushMode DefaultFlushMode { get; set; } = FlushMode.Auto;

        public ISession OpenSession()
        {
            return OpenSession(Constants.DefaultAlias);
        }

        public ISession OpenSession(string alias)
        {
            if (alias is null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var currentTransaction = _transactionManager.CurrentTransaction;

            var wrappedSession = _sessionStore.FindCompatibleSession(alias);

            if (wrappedSession is null)
            {
                var session = CreateSession(alias);

                wrappedSession = WrapSession(session, currentTransaction is not null);
                EnlistIfNecessary(currentTransaction, wrappedSession, true);
                _sessionStore.Store(alias, wrappedSession);
            }
            else
            {
                EnlistIfNecessary(currentTransaction, wrappedSession, false);
                wrappedSession = WrapSession(wrappedSession.InnerSession, true);
            }

            return wrappedSession;
        }

        public IStatelessSession OpenStatelessSession()
        {
            return OpenStatelessSession(Constants.DefaultAlias);
        }

        public IStatelessSession OpenStatelessSession(string alias)
        {
            if (alias is null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var currentTransaction = _transactionManager.CurrentTransaction;

            var wrappedSession = _sessionStore.FindCompatibleStatelessSession(alias);

            if (wrappedSession is null)
            {
                var session = CreateStatelessSession(alias);

                wrappedSession = WrapStatelessSession(session, currentTransaction is not null);
                EnlistIfNecessary(currentTransaction, wrappedSession, true);
                _sessionStore.Store(alias, wrappedSession);
            }
            else
            {
                EnlistIfNecessary(currentTransaction, wrappedSession, false);
                wrappedSession = WrapStatelessSession(wrappedSession.InnerSession, true);
            }

            return wrappedSession;
        }

        /// <summary>
        /// Enlists if necessary.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="session">The session.</param>
        /// <param name="weAreSessionOwner">If set to <see langword="true" />, then we are the session owner.</param>
        /// <returns></returns>
        protected static bool EnlistIfNecessary(
            ITransaction transaction,
            SessionDelegate session,
            bool weAreSessionOwner)
        {
            if (transaction is null)
            {
                return false;
            }

            bool shouldEnlist;

            transaction.Context.TryGetValueAs(Constants.Session_TransactionEnlistment_TransactionContextKey,
                                              out List<ISession> list);

            if (list is null)
            {
                list = new List<ISession>();

                shouldEnlist = true;
            }
            else
            {
                shouldEnlist = true;

                foreach (var item in list)
                {
                    if (SessionDelegate.AreEqual(session, item))
                    {
                        shouldEnlist = false;

                        break;
                    }
                }
            }

            if (shouldEnlist)
            {
                //
                //  NOTE:   SessionDelegate.Transaction, with slightly-modified implementation of ISession.GetCurrentTransaction(),
                //          is used here to workaround a mocking issue (in Facilities103 issue) of ISession.GetSessionImplementation().
                //
                //var sessionTransaction = session.GetCurrentTransaction();
                var sessionTransaction = session.Transaction;
                if (sessionTransaction is null || !sessionTransaction.IsActive)
                {
                    transaction.Context[Constants.Session_TransactionEnlistment_TransactionContextKey] = list;

                    var isolationLevel = TranslateIsolationLevel(transaction.IsolationLevel);
                    transaction.Enlist(new ResourceAdapter(session.BeginTransaction(isolationLevel), transaction.IsAmbient));

                    list.Add(session);
                }

                if (weAreSessionOwner)
                {
                    transaction.RegisterSynchronization(new SessionDisposeSynchronization(session));
                }
            }

            return true;
        }

        /// <summary>
        /// Enlists if necessary.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        /// <param name="session">The session.</param>
        /// <param name="weAreSessionOwner">If set to <see langword="true" />, then we are the session owner.</param>
        /// <returns></returns>
        protected static bool EnlistIfNecessary(
            ITransaction transaction,
            StatelessSessionDelegate session,
            bool weAreSessionOwner)
        {
            if (transaction is null)
            {
                return false;
            }

            bool shouldEnlist;

            transaction.Context.TryGetValueAs(Constants.StatelessSession_TransactionEnlistment_TransactionContextKey,
                                              out List<IStatelessSession> list);

            if (list is null)
            {
                list = new List<IStatelessSession>();

                shouldEnlist = true;
            }
            else
            {
                shouldEnlist = true;

                foreach (var item in list)
                {
                    if (StatelessSessionDelegate.AreEqual(session, item))
                    {
                        shouldEnlist = false;

                        break;
                    }
                }
            }

            if (shouldEnlist)
            {
                //
                //  NOTE:   StatelessSessionDelegate.Transaction, with slightly-modified implementation of IStatelessSessionDelegate.GetCurrentTransaction(),
                //          is used here to workaround a mocking issue (in Facilities103 issue) of IStatelessSessionDelegate.GetSessionImplementation().
                //
                //var sessionTransaction = session.GetCurrentTransaction();
                var sessionTransaction = session.Transaction;
                if (sessionTransaction is null || !sessionTransaction.IsActive)
                {
                    transaction.Context[Constants.StatelessSession_TransactionEnlistment_TransactionContextKey] = list;

                    var isolationLevel = TranslateIsolationLevel(transaction.IsolationLevel);
                    transaction.Enlist(new ResourceAdapter(session.BeginTransaction(isolationLevel), transaction.IsAmbient));

                    list.Add(session);
                }

                if (weAreSessionOwner)
                {
                    transaction.RegisterSynchronization(new StatelessSessionDisposeSynchronization(session));
                }
            }

            return true;
        }

        private static System.Data.IsolationLevel TranslateIsolationLevel(IsolationLevel isolationLevel)
        {
            return isolationLevel switch
            {
                IsolationLevel.Chaos =>
                System.Data.IsolationLevel.Chaos,

                IsolationLevel.ReadCommitted =>
                System.Data.IsolationLevel.ReadCommitted,

                IsolationLevel.ReadUncommitted =>
                System.Data.IsolationLevel.ReadUncommitted,

                IsolationLevel.RepeatableRead =>
                System.Data.IsolationLevel.RepeatableRead,

                IsolationLevel.Serializable =>
                System.Data.IsolationLevel.Serializable,

                IsolationLevel.Snapshot =>
                System.Data.IsolationLevel.Snapshot,

                _ => System.Data.IsolationLevel.Unspecified,
            };
        }

        private SessionDelegate WrapSession(ISession session, bool hasTransaction)
        {
            return new SessionDelegate(session, _sessionStore, !hasTransaction);
        }

        private StatelessSessionDelegate WrapStatelessSession(IStatelessSession session, bool hasTransaction)
        {
            return new StatelessSessionDelegate(session, _sessionStore, !hasTransaction);
        }

        private ISession CreateSession(string alias)
        {
            var sessionFactory = _sessionFactoryResolver.GetSessionFactory(alias);

            if (sessionFactory is null)
            {
                var message = $"No '{nameof(ISessionFactory)}' implementation associated with the given '{nameof(ISession)}' alias: '{alias}'.";
                throw new FacilityException(message);
            }

            ISession session;

            var aliasedInterceptorName = string.Format(Constants.SessionInterceptor_ComponentNameFormat, alias);

            if (_kernel.HasComponent(aliasedInterceptorName))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(aliasedInterceptorName);

                session = sessionFactory.WithOptions()
                                        .Interceptor(interceptor)
                                        .OpenSession();
            }
            else if (_kernel.HasComponent(Constants.SessionInterceptor_ComponentName))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(Constants.SessionInterceptor_ComponentName);

                session = sessionFactory.WithOptions()
                                        .Interceptor(interceptor)
                                        .OpenSession();
            }
            else
            {
                session = sessionFactory.OpenSession();
            }

            session.FlushMode = DefaultFlushMode;

            return session;
        }

        private IStatelessSession CreateStatelessSession(string alias)
        {
            var sessionFactory = _sessionFactoryResolver.GetSessionFactory(alias);

            if (sessionFactory is null)
            {
                var message = $"No '{nameof(ISessionFactory)}' implementation associated with the given '{nameof(IStatelessSession)}' alias: '{alias}'.";
                throw new FacilityException(message);
            }

            var session = sessionFactory.OpenStatelessSession();

            return session;
        }
    }
}
