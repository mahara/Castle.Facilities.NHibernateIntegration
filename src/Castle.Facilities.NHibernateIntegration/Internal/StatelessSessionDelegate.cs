namespace Castle.Facilities.NHibernateIntegration
{
	#region Using Directives

	using System;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;

	using NHibernate;
	using NHibernate.Engine;

	#endregion

	/// <summary>
	///     Proxies an IStatelessSession so the user cannot close a stateless session
	///     which is controlled by a transaction, or, when this is not the case,
	///     make sure to remove the session from the storage.
	///     <seealso cref="ISessionStore" />
	///     <seealso cref="ISessionManager" />
	/// </summary>
	[Serializable]
	public class StatelessSessionDelegate : MarshalByRefObject, IStatelessSession
	{
		private readonly bool _canClose;
		private readonly ISessionStore _sessionStore;
		private object _cookie;
		private bool _disposed;

		/// <summary>
		///     Initializes a new instance of the <see cref="StatelessSessionDelegate" /> class.
		/// </summary>
		/// <param name="canClose">if set to <c>true</c> [can close].</param>
		/// <param name="innerSession">The inner session.</param>
		/// <param name="sessionStore">The session store.</param>
		/// <remarks>
		///     https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618
		///     #pragma warning disable 0618, 0612
		///     #pragma warning restore 0618, 0612
		/// </remarks>
		public StatelessSessionDelegate(bool canClose, IStatelessSession innerSession, ISessionStore sessionStore)
		{
			this.InnerSession = innerSession;
			this._sessionStore = sessionStore;
			this._canClose = canClose;
		}

		/// <summary>
		///     Gets the inner session.
		/// </summary>
		/// <value>The inner session.</value>
		public IStatelessSession InnerSession { get; }

		/// <summary>
		///     Gets or sets the session store cookie.
		/// </summary>
		/// <value>The session store cookie.</value>
		public object SessionStoreCookie
		{
			get => this._cookie;
			set => this._cookie = value;
		}

		#region IDisposable delegation

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		/// <filterpriority>2</filterpriority>
		public void Dispose()
		{
			this.DoClose(false);
		}

		#endregion

		/// <summary>
		///     Does the close.
		/// </summary>
		/// <param name="closing">if set to <c>true</c> [closing].</param>
		/// <returns></returns>
		protected IDbConnection DoClose(bool closing)
		{
			if (this._disposed)
			{
				return null;
			}

			if (this._canClose)
			{
				return this.InternalClose(closing);
			}

			return null;
		}

		internal IDbConnection InternalClose(bool closing)
		{
			IDbConnection conn = null;

			this._sessionStore.Remove(this);

			if (closing)
			{
				conn = this.InnerSession.Connection;
				this.InnerSession.Close();
			}

			this.InnerSession.Dispose();

			this._disposed = true;

			return conn;
		}

		/// <summary>
		///     Returns <see langword="true" /> if the supplied stateless sessions are equal, <see langword="false" /> otherwise.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool AreEqual(IStatelessSession left, IStatelessSession right)
		{
			if (left is StatelessSessionDelegate ssdLeft
			    && right is StatelessSessionDelegate ssdRight)
			{
				return ReferenceEquals(ssdLeft.InnerSession, ssdRight.InnerSession);
			}

			throw new NotSupportedException("AreEqual: left is " +
			                                left.GetType().Name + " and right is " + right.GetType().Name);
		}

		#region IStatelessSession delegation

		/// <inheritdoc />
		public IQueryable<T> Query<T>(string entityName)
		{
			return this.InnerSession.Query<T>(entityName);
		}

		/// <summary>
		///     Returns the current ADO.NET connection associated with this instance.
		/// </summary>
		/// <remarks>
		///     If the session is using aggressive connection release (as in a
		///     CMT environment), it is the application's responsibility to
		///     close the connection returned by this call. Otherwise, the
		///     application should not close the connection.
		/// </remarks>
		public DbConnection Connection => this.InnerSession.Connection;

		/// <inheritdoc />
		public bool IsConnected => this.InnerSession.IsConnected;

		/// <inheritdoc />
		public bool IsOpen => this.InnerSession.IsOpen;

		/// <inheritdoc />
		public ITransaction Transaction => this.InnerSession.Transaction;

		/// <inheritdoc />
		public ITransaction BeginTransaction()
		{
			return this.InnerSession.BeginTransaction();
		}

		/// <inheritdoc />
		public ITransaction BeginTransaction(IsolationLevel isolationLevel)
		{
			return this.InnerSession.BeginTransaction(isolationLevel);
		}

		/// <inheritdoc />
		public void JoinTransaction()
		{
			this.InnerSession.JoinTransaction();
		}

		/// <inheritdoc />
		public void Close()
		{
			this.InnerSession.Close();
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria<T>() where T : class
		{
			return this.InnerSession.CreateCriteria<T>();
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			return this.InnerSession.CreateCriteria<T>(alias);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(Type entityType)
		{
			return this.InnerSession.CreateCriteria(entityType);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(Type entityType, string alias)
		{
			return this.InnerSession.CreateCriteria(entityType, alias);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(string entityName)
		{
			return this.InnerSession.CreateCriteria(entityName);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(string entityName, string alias)
		{
			return this.InnerSession.CreateCriteria(entityName, alias);
		}

		/// <inheritdoc />
		public IQuery CreateQuery(string queryString)
		{
			return this.InnerSession.CreateQuery(queryString);
		}

		/// <inheritdoc />
		public ISQLQuery CreateSQLQuery(string queryString)
		{
			return this.InnerSession.CreateSQLQuery(queryString);
		}

		/// <inheritdoc />
		public void Delete(object entity)
		{
			this.InnerSession.Delete(entity);
		}

		/// <inheritdoc />
		public void Delete(string entityName, object entity)
		{
			this.InnerSession.Delete(entityName, entity);
		}

		/// <inheritdoc />
		public object Get(string entityName, object id)
		{
			return this.InnerSession.Get(entityName, id);
		}

		/// <inheritdoc />
		public T Get<T>(object id)
		{
			return this.InnerSession.Get<T>(id);
		}

		/// <inheritdoc />
		public object Get(string entityName, object id, LockMode lockMode)
		{
			return this.InnerSession.Get(entityName, id, lockMode);
		}

		/// <inheritdoc />
		public T Get<T>(object id, LockMode lockMode)
		{
			return this.InnerSession.Get<T>(id, lockMode);
		}

		/// <inheritdoc />
		public IQuery GetNamedQuery(string queryName)
		{
			return this.InnerSession.GetNamedQuery(queryName);
		}

		/// <inheritdoc />
		public Task<object> InsertAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.InsertAsync(entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> InsertAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.InsertAsync(entityName, entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(entityName, entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task DeleteAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task DeleteAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(entityName, entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync(entityName, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync<T>(id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> GetAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync(entityName, id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync<T>(id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(entityName, entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(entity, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(string entityName, object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(entityName, entity, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public ISessionImplementor GetSessionImplementation()
		{
			return this.InnerSession.GetSessionImplementation();
		}

		/// <inheritdoc />
		public object Insert(object entity)
		{
			return this.InnerSession.Insert(entity);
		}

		/// <inheritdoc />
		public object Insert(string entityName, object entity)
		{
			return this.InnerSession.Insert(entityName, entity);
		}

		/// <inheritdoc />
		public IQueryOver<T, T> QueryOver<T>() where T : class
		{
			return this.InnerSession.QueryOver<T>();
		}

		/// <inheritdoc />
		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
		{
			return this.InnerSession.QueryOver(alias);
		}

		/// <inheritdoc />
		public void Refresh(object entity)
		{
			this.InnerSession.Refresh(entity);
		}

		/// <inheritdoc />
		public void Refresh(string entityName, object entity)
		{
			this.InnerSession.Refresh(entityName, entity);
		}

		/// <inheritdoc />
		public void Refresh(object entity, LockMode lockMode)
		{
			this.InnerSession.Refresh(entity, lockMode);
		}

		/// <inheritdoc />
		public void Refresh(string entityName, object entity, LockMode lockMode)
		{
			this.InnerSession.Refresh(entityName, entity, lockMode);
		}

		/// <inheritdoc />
		public IStatelessSession SetBatchSize(int batchSize)
		{
			return this.InnerSession.SetBatchSize(batchSize);
		}

		/// <inheritdoc />
		public IQueryable<T> Query<T>()
		{
			return this.InnerSession.Query<T>();
		}

		/// <inheritdoc />
		public void Update(object entity)
		{
			this.InnerSession.Update(entity);
		}

		/// <inheritdoc />
		public void Update(string entityName, object entity)
		{
			this.InnerSession.Update(entityName, entity);
		}

		#endregion
	}
}