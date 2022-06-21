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

namespace Castle.Facilities.NHibernateIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Castle.Facilities.NHibernateIntegration.Internal;
    using Castle.MicroKernel;
    using Castle.MicroKernel.Facilities;
    using Castle.Services.Transaction;

    using NHibernate;

    using ITransaction = Services.Transaction.ITransaction;

    /// <summary>
    /// Default session manager implementation.
    /// </summary>
    public class DefaultSessionManager : MarshalByRefObject, ISessionManager
    {
        /// <summary>
        /// Format string for NHibernate interceptor components.
        /// </summary>
        public const string InterceptorFormatString = "nhibernate.session.interceptor.{0}";

        /// <summary>
        /// Name for NHibernate Interceptor componentInterceptorName.
        /// </summary>
        public const string InterceptorName = "nhibernate.session.interceptor";

        private readonly IKernel _kernel;
        private readonly ISessionStore _sessionStore;
        private readonly ISessionFactoryResolver _factoryResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSessionManager" /> class.
        /// </summary>
        /// <param name="sessionStore">The session store.</param>
        /// <param name="kernel">The kernel.</param>
        /// <param name="factoryResolver">The factory resolver.</param>
        public DefaultSessionManager(ISessionStore sessionStore, IKernel kernel, ISessionFactoryResolver factoryResolver)
        {
            _kernel = kernel;
            _sessionStore = sessionStore;
            _factoryResolver = factoryResolver;
        }

        /// <summary>
        /// The default flush mode.
        /// </summary>
        public FlushMode DefaultFlushMode { get; set; } = FlushMode.Auto;

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
        public ISession OpenSession(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var transaction = ObtainCurrentTransaction();

            var wrapped = _sessionStore.FindCompatibleSession(alias);

            if (wrapped == null)
            {
                var session = CreateSession(alias);

                wrapped = WrapSession(transaction != null, session);
                EnlistIfNecessary(true, transaction, wrapped);
                _sessionStore.Store(alias, wrapped);
            }
            else
            {
                EnlistIfNecessary(false, transaction, wrapped);
                wrapped = WrapSession(true, wrapped.InnerSession);
            }

            return wrapped;
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
        public IStatelessSession OpenStatelessSession(string alias)
        {
            if (alias == null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            var transaction = ObtainCurrentTransaction();

            var wrapped = _sessionStore.FindCompatibleStatelessSession(alias);

            if (wrapped == null)
            {
                var session = CreateStatelessSession(alias);

                wrapped = WrapStatelessSession(transaction != null, session);
                EnlistIfNecessary(true, transaction, wrapped);
                _sessionStore.Store(alias, wrapped);
            }
            else
            {
                EnlistIfNecessary(false, transaction, wrapped);
                wrapped = WrapStatelessSession(true, wrapped.InnerSession);
            }

            return wrapped;
        }

        /// <summary>
        /// Enlists if necessary.
        /// </summary>
        /// <param name="weAreSessionOwner">if set to <c>true</c> [we are session owner].</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="session">The session.</param>
        /// <returns></returns>
        protected bool EnlistIfNecessary(bool weAreSessionOwner,
                                         ITransaction transaction,
                                         SessionDelegate session)
        {
            if (transaction == null)
            {
                return false;
            }

            var list = (IList<ISession>) transaction.Context["nh.session.enlisted"];

            bool shouldEnlist;

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
                if (session.Transaction == null || !session.Transaction.IsActive)
                {
                    transaction.Context["nh.session.enlisted"] = list;

                    var level = TranslateIsolationLevel(transaction.IsolationMode);
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
        protected bool EnlistIfNecessary(bool weAreSessionOwner,
                                         ITransaction transaction,
                                         StatelessSessionDelegate statelessSession)
        {
            if (transaction == null)
            {
                return false;
            }

            var list = (IList<IStatelessSession>) transaction.Context["nh.statelessSession.enlisted"];

            bool shouldEnlist;

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
                if (statelessSession.Transaction == null || !statelessSession.Transaction.IsActive)
                {
                    transaction.Context["nh.statelessSession.enlisted"] = list;

                    var level = TranslateIsolationLevel(transaction.IsolationMode);
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

        private static IsolationLevel TranslateIsolationLevel(IsolationMode mode)
        {
            switch (mode)
            {
                case IsolationMode.Chaos:
                    return IsolationLevel.Chaos;

                case IsolationMode.ReadCommitted:
                    return IsolationLevel.ReadCommitted;

                case IsolationMode.ReadUncommitted:
                    return IsolationLevel.ReadUncommitted;

                case IsolationMode.RepeatableRead:
                    return IsolationLevel.RepeatableRead;

                case IsolationMode.Serializable:
                    return IsolationLevel.Serializable;

                case IsolationMode.Snapshot:
                    return IsolationLevel.Snapshot;

                default:
                    return IsolationLevel.Unspecified;
            }
        }

        private ITransaction ObtainCurrentTransaction()
        {
            var transactionManager = _kernel.Resolve<ITransactionManager>();

            return transactionManager.CurrentTransaction;
        }

        private SessionDelegate WrapSession(bool hasTransaction, ISession session)
        {
            return new SessionDelegate(!hasTransaction, session, _sessionStore);
        }

        private StatelessSessionDelegate WrapStatelessSession(bool hasTransaction, IStatelessSession statelessSession)
        {
            return new StatelessSessionDelegate(!hasTransaction, statelessSession, _sessionStore);
        }

        private ISession CreateSession(string alias)
        {
            var sessionFactory = _factoryResolver.GetSessionFactory(alias);

            if (sessionFactory == null)
            {
                throw new FacilityException("No ISessionFactory implementation " +
                                            "associated with the given ISession alias: " + alias);
            }

            ISession session;

            var aliasedInterceptorId = string.Format(InterceptorFormatString, alias);

            if (_kernel.HasComponent(aliasedInterceptorId))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(aliasedInterceptorId);

                session = sessionFactory.WithOptions()
                                        .Interceptor(interceptor)
                                        .OpenSession();
            }
            else if (_kernel.HasComponent(InterceptorName))
            {
                var interceptor = _kernel.Resolve<IInterceptor>(InterceptorName);

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
            var sessionFactory = _factoryResolver.GetSessionFactory(alias);

            if (sessionFactory == null)
            {
                throw new FacilityException("No ISessionFactory implementation " +
                                            "associated with the given IStatelessSession alias: " + alias);
            }

            var session = sessionFactory.OpenStatelessSession();

            return session;
        }
    }
}