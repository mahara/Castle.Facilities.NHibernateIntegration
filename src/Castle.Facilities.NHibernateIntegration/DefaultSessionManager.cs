#region License

//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

#endregion

namespace Castle.Facilities.NHibernateIntegration
{
	#region Using Directives

	using System;
	using System.Collections.Generic;
	using System.Data;

	using Castle.Facilities.NHibernateIntegration.Internal;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Facilities;

	using NHibernate;

	using Castle.Services.Transaction;

	using ITransaction = Castle.Services.Transaction.ITransaction;

	#endregion

	/// <summary>
	/// </summary>
	public class DefaultSessionManager : MarshalByRefObject, ISessionManager
	{
		private readonly ISessionFactoryResolver _factoryResolver;

		private readonly IKernel _kernel;
		private readonly ISessionStore _sessionStore;

		/// <summary>
		///     Initializes a new instance of the <see cref="DefaultSessionManager" /> class.
		/// </summary>
		/// <param name="sessionStore">The session store.</param>
		/// <param name="kernel">The kernel.</param>
		/// <param name="factoryResolver">The factory resolver.</param>
		public DefaultSessionManager(ISessionStore sessionStore, IKernel kernel, ISessionFactoryResolver factoryResolver)
		{
			this._kernel = kernel;
			this._sessionStore = sessionStore;
			this._factoryResolver = factoryResolver;
		}

		/// <summary>
		///     Gets the default flush mode.
		/// </summary>
		/// <value></value>
		public FlushMode DefaultFlushMode { get; set; } = FlushMode.Auto;

		/// <summary>
		///     Returns a valid opened and connected ISession instance.
		/// </summary>
		/// <returns></returns>
		public ISession OpenSession()
		{
			return this.OpenSession(Constants.DefaultAlias);
		}

		/// <summary>
		///     Returns a valid opened and connected ISession instance
		///     for the given connection alias.
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public ISession OpenSession(string alias)
		{
			if (alias == null)
			{
				throw new ArgumentNullException(nameof(alias));
			}

			var transaction = this.ObtainCurrentTransaction();

			var wrapped = this._sessionStore.FindCompatibleSession(alias);

			if (wrapped == null)
			{
				var session = this.CreateSession(alias);

				wrapped = this.WrapSession(transaction != null, session);
				this.EnlistIfNecessary(true, transaction, wrapped);
				this._sessionStore.Store(alias, wrapped);
			}
			else
			{
				this.EnlistIfNecessary(false, transaction, wrapped);
				wrapped = this.WrapSession(true, wrapped.InnerSession);
			}

			return wrapped;
		}

		/// <summary>
		///     Returns a valid opened and connected IStatelessSession instance
		/// </summary>
		/// <returns></returns>
		public IStatelessSession OpenStatelessSession()
		{
			return this.OpenStatelessSession(Constants.DefaultAlias);
		}

		/// <summary>
		///     Returns a valid opened and connected IStatelessSession instance
		///     for the given connection alias.
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public IStatelessSession OpenStatelessSession(string alias)
		{
			if (alias == null)
			{
				throw new ArgumentNullException(nameof(alias));
			}

			var transaction = this.ObtainCurrentTransaction();

			var wrapped = this._sessionStore.FindCompatibleStatelessSession(alias);

			if (wrapped == null)
			{
				var session = this.CreateStatelessSession(alias);

				wrapped = this.WrapSession(transaction != null, session);
				this.EnlistIfNecessary(true, transaction, wrapped);
				this._sessionStore.Store(alias, wrapped);
			}
			else
			{
				this.EnlistIfNecessary(false, transaction, wrapped);
				wrapped = this.WrapSession(true, wrapped.InnerSession);
			}

			return wrapped;
		}

		/// <summary>
		///     Enlists if necessary.
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
		///     Enlists if necessary.
		/// </summary>
		/// <param name="weAreSessionOwner">if set to <c>true</c> [we are session owner].</param>
		/// <param name="transaction">The transaction.</param>
		/// <param name="session">The session.</param>
		/// <returns></returns>
		protected bool EnlistIfNecessary(bool weAreSessionOwner,
		                                 ITransaction transaction,
		                                 StatelessSessionDelegate session)
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
					if (StatelessSessionDelegate.AreEqual(session, s))
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
					transaction.Context["nh.statelessSession.enlisted"] = list;

					var level = TranslateIsolationLevel(transaction.IsolationMode);
					transaction.Enlist(new ResourceAdapter(session.BeginTransaction(level), transaction.IsAmbient));

					list.Add(session);
				}

				if (weAreSessionOwner)
				{
					transaction.RegisterSynchronization(new StatelessSessionDisposeSynchronization(session));
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
			var transactionManager = this._kernel.Resolve<ITransactionManager>();

			return transactionManager.CurrentTransaction;
		}

		private SessionDelegate WrapSession(bool hasTransaction, ISession session)
		{
			return new SessionDelegate(!hasTransaction, session, this._sessionStore);
		}

		private StatelessSessionDelegate WrapSession(bool hasTransaction, IStatelessSession session)
		{
			return new StatelessSessionDelegate(!hasTransaction, session, this._sessionStore);
		}

		private ISession CreateSession(string alias)
		{
			var sessionFactory = this._factoryResolver.GetSessionFactory(alias);

			if (sessionFactory == null)
			{
				throw new FacilityException("No ISessionFactory implementation " +
				                            "associated with the given alias: " + alias);
			}

			ISession session;

			var aliasedInterceptorId = string.Format(InterceptorFormatString, alias);

			if (this._kernel.HasComponent(aliasedInterceptorId))
			{
				var interceptor = this._kernel.Resolve<IInterceptor>(aliasedInterceptorId);

				//session = sessionFactory.OpenSession(interceptor);
				session = sessionFactory.WithOptions()
				                        .Interceptor(interceptor)
				                        .OpenSession();
			}
			else if (this._kernel.HasComponent(InterceptorName))
			{
				var interceptor = this._kernel.Resolve<IInterceptor>(InterceptorName);

				//session = sessionFactory.OpenSession(interceptor);
				session = sessionFactory.WithOptions()
				                        .Interceptor(interceptor)
				                        .OpenSession();
			}
			else
			{
				session = sessionFactory.OpenSession();
			}

			session.FlushMode = this.DefaultFlushMode;

			return session;
		}

		private IStatelessSession CreateStatelessSession(string alias)
		{
			var sessionFactory = this._factoryResolver.GetSessionFactory(alias);

			if (sessionFactory == null)
			{
				throw new FacilityException("No ISessionFactory implementation " +
				                            "associated with the given alias: " + alias);
			}

			var session = sessionFactory.OpenStatelessSession();

			return session;
		}

		#region constants

		/// <summary>
		///     Format string for NHibernate interceptor components
		/// </summary>
		public const string InterceptorFormatString = "nhibernate.session.interceptor.{0}";

		/// <summary>
		///     Name for NHibernate Interceptor componentInterceptorName
		/// </summary>
		public const string InterceptorName = "nhibernate.session.interceptor";

		#endregion
	}
}