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

namespace Castle.Facilities.NHibernateIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Castle.Facilities.NHibernateIntegration.Internal;
    using Castle.Facilities.NHibernateIntegration.Util;
    using Castle.MicroKernel;
    using Castle.MicroKernel.Facilities;
    using Castle.Services.Transaction;

    using NHibernate;

    using ITransaction = Castle.Services.Transaction.ITransaction;

    /// <summary>
    /// Default session manager implementation.
    /// </summary>
    public class DefaultSessionManager : MarshalByRefObject, ISessionManager
    {
        /// <summary>
        /// Default <see cref="IInterceptor" /> component key.
        /// </summary>
        public const string InterceptorKey = "nhibernate.session.interceptor";

        /// <summary>
        /// Format string for <see cref="IInterceptor" /> component key.
        /// </summary>
        public const string InterceptorKeyFormat = "nhibernate.session.interceptor.{0}";

        internal const string SessionEnlistedContextKey = "nh.session.enlisted";
        internal const string StatelessSessionEnlistedContextKey = "nh.statelessSession.enlisted";

        private readonly IKernel _kernel;
        private readonly ISessionStore _sessionStore;
        private readonly ISessionFactoryResolver _sessionFactoryResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSessionManager" /> class.
        /// </summary>
        /// <param name="kernel">The <see cref="IKernel" />.</param>
        /// <param name="sessionStore">The <see cref="ISessionStore" />.</param>
        /// <param name="sessionFactoryResolver">The <see cref="ISessionFactoryResolver" />.</param>
        public DefaultSessionManager(IKernel kernel,
                                     ISessionStore sessionStore,
                                     ISessionFactoryResolver sessionFactoryResolver)
        {
            _kernel = kernel;
            _sessionStore = sessionStore;
            _sessionFactoryResolver = sessionFactoryResolver;
        }

        /// <summary>
        /// The default <see cref="ISession" /> flush mode.
        /// </summary>
        public FlushMode DefaultFlushMode { get; set; } =
            FlushMode.Auto;

        /// <summary>
        /// Returns a valid opened and connected <see cref="ISession" /> instance.
        /// </summary>
        /// <returns></returns>
        public ISession OpenSession()
        {
            return OpenSession(Constants.DefaultAlias);
        }

        /// <summary>
        /// Returns a valid opened and connected <see cref="ISession" /> instance for the given connection alias.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public ISession OpenSession(string? alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var transaction = GetCurrentTransaction();

            var wrappedSession = _sessionStore.FindCompatibleSession(alias);
            if (wrappedSession == null)
            {
                var session = CreateSession(alias);

                wrappedSession = WrapSession(transaction != null, session);
                EnlistIfNecessary(true, transaction, wrappedSession);
                _sessionStore.Store(alias, wrappedSession);
            }
            else
            {
                EnlistIfNecessary(false, transaction, wrappedSession);
                wrappedSession = WrapSession(true, wrappedSession.InnerSession);
            }

            return wrappedSession;
        }

        /// <summary>
        /// Returns a valid opened and connected <see cref="IStatelessSession" /> instance.
        /// </summary>
        /// <returns></returns>
        public IStatelessSession OpenStatelessSession()
        {
            return OpenStatelessSession(Constants.DefaultAlias);
        }

        /// <summary>
        /// Returns a valid opened and connected <see cref="IStatelessSession" /> instance for the given connection alias.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public IStatelessSession OpenStatelessSession(string? alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var transaction = GetCurrentTransaction();

            var wrappedSession = _sessionStore.FindCompatibleStatelessSession(alias);
            if (wrappedSession == null)
            {
                var session = CreateStatelessSession(alias);

                wrappedSession = WrapStatelessSession(transaction != null, session);
                EnlistIfNecessary(true, transaction, wrappedSession);
                _sessionStore.Store(alias, wrappedSession);
            }
            else
            {
                EnlistIfNecessary(false, transaction, wrappedSession);
                wrappedSession = WrapStatelessSession(true, wrappedSession.InnerSession);
            }

            return wrappedSession;
        }

        /// <summary>
        /// Enlists if necessary.
        /// </summary>
        /// <param name="weAreSessionOwner">if set to <c>true</c> [we are session owner].</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        protected static bool EnlistIfNecessary(bool weAreSessionOwner,
                                                ITransaction? transaction,
                                                SessionDelegate session)
        {
            if (transaction == null)
            {
                return false;
            }

            bool shouldEnlist;

            transaction.Context.TryGetValueAs(SessionEnlistedContextKey,
                                              out IList<ISession>? list);
            if (list == null)
            {
                list = new List<ISession>();

                shouldEnlist = true;
            }
            else
            {
                shouldEnlist = true;

                foreach (var s in list)
                {
                    if (SessionDelegate.AreEqual(session, s))
                    {
                        shouldEnlist = false;

                        break;
                    }
                }
            }

            if (shouldEnlist)
            {
                //
                // NOTE:    SessionDelegate.Transaction, with slightly-modified implementation of ISession.GetCurrentTransaction(),
                //          is used here to workaround a mocking issue (in Facilities103 issue) of ISession.GetSessionImplementation().
                //
                var sessionTransaction = session.Transaction;
                //var sessionTransaction = session.GetCurrentTransaction();
                if (sessionTransaction == null || !sessionTransaction.IsActive)
                {
                    transaction.Context[SessionEnlistedContextKey] = list;

                    var level = TranslateTransactionIsolationLevel(transaction.IsolationLevel);
                    transaction.Enlist(new ResourceAdapter(session.BeginTransaction(level), transaction.IsAmbient));

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
        /// <param name="weAreSessionOwner">If set to <c>true</c> [we are session owner].</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="statelessSession">The stateless session.</param>
        /// <returns></returns>
        protected static bool EnlistIfNecessary(bool weAreSessionOwner,
                                                ITransaction? transaction,
                                                StatelessSessionDelegate statelessSession)
        {
            if (transaction == null)
            {
                return false;
            }

            bool shouldEnlist;

            transaction.Context.TryGetValueAs(StatelessSessionEnlistedContextKey,
                                              out IList<IStatelessSession>? list);
            if (list == null)
            {
                list = new List<IStatelessSession>();

                shouldEnlist = true;
            }
            else
            {
                shouldEnlist = true;

                foreach (var s in list)
                {
                    if (StatelessSessionDelegate.AreEqual(statelessSession, s))
                    {
                        shouldEnlist = false;

                        break;
                    }
                }
            }

            if (shouldEnlist)
            {
                //
                // NOTE:    StatelessSessionDelegate.Transaction, with slightly-modified implementation of IStatelessSession.GetCurrentTransaction(),
                //          is used here to workaround a mocking issue (in Facilities103 issue) of IStatelessSession.GetSessionImplementation().
                //
                var sessionTransaction = statelessSession.Transaction;
                //var sessionTransaction = statelessSession.GetCurrentTransaction();
                if (sessionTransaction == null || !sessionTransaction.IsActive)
                {
                    transaction.Context[StatelessSessionEnlistedContextKey] = list;

                    var level = TranslateTransactionIsolationLevel(transaction.IsolationLevel);
                    transaction.Enlist(new ResourceAdapter(statelessSession.BeginTransaction(level), transaction.IsAmbient));

                    list.Add(statelessSession);
                }

                if (weAreSessionOwner)
                {
                    transaction.RegisterSynchronization(new StatelessSessionDisposeSynchronization(statelessSession));
                }
            }

            return true;
        }

        private static IsolationLevel TranslateTransactionIsolationLevel(
            System.Transactions.IsolationLevel isolationLevel)
        {
            return isolationLevel switch
            {
                System.Transactions.IsolationLevel.Serializable => IsolationLevel.Serializable,
                System.Transactions.IsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
                System.Transactions.IsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
                System.Transactions.IsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
                System.Transactions.IsolationLevel.Snapshot => IsolationLevel.Snapshot,
                System.Transactions.IsolationLevel.Chaos => IsolationLevel.Chaos,
                _ => IsolationLevel.Unspecified,
            };
        }

        private ITransaction? GetCurrentTransaction()
        {
            var transactionManager = _kernel.Resolve<ITransactionManager>();
            return transactionManager.CurrentTransaction;
        }

        private SessionDelegate WrapSession(bool hasTransaction, ISession session)
        {
            return new SessionDelegate(session, _sessionStore, !hasTransaction);
        }

        private StatelessSessionDelegate WrapStatelessSession(bool hasTransaction, IStatelessSession statelessSession)
        {
            return new StatelessSessionDelegate(statelessSession, _sessionStore, !hasTransaction);
        }

        private ISession CreateSession(string alias)
        {
            var sessionFactory = _sessionFactoryResolver.GetSessionFactory(alias);

            if (sessionFactory == null)
            {
                throw new FacilityException(
                    $"No '{nameof(ISessionFactory)}' implementation " +
                    $"associated with the given '{nameof(ISession)}' alias: '{alias}'.");
            }

            ISession session;

            var aliasedInterceptorId = string.Format(InterceptorKeyFormat, alias);

            if (_kernel.HasComponent(aliasedInterceptorId))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(aliasedInterceptorId);

                session = sessionFactory.WithOptions()
                                        .Interceptor(interceptor)
                                        .OpenSession();
            }
            else if (_kernel.HasComponent(InterceptorKey))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(InterceptorKey);

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

            if (sessionFactory == null)
            {
                throw new FacilityException(
                    $"No '{nameof(ISessionFactory)}' implementation " +
                    $"associated with the given '{nameof(IStatelessSession)}' alias: '{alias}'.");
            }

            var session = sessionFactory.OpenStatelessSession();

            return session;
        }
    }
}
