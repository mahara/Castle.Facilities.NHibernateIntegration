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
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;

	using NHibernate;
	using NHibernate.Engine;
	using NHibernate.Stat;
	using NHibernate.Type;

	#endregion

	/// <summary>
	///     Proxies an ISession so the user cannot close a session which
	///     is controlled by a transaction, or, when this is not the case,
	///     make sure to remove the session from the storage.
	///     <seealso cref="ISessionStore" />
	///     <seealso cref="ISessionManager" />
	/// </summary>
	/// <remarks>
	///     https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618
	///     #pragma warning disable 0618, 0612
	///     #pragma warning restore 0618, 0612
	/// </remarks>
	[Serializable]
	public class SessionDelegate : MarshalByRefObject, ISession
	{
		private readonly bool _canClose;
		private readonly ISessionStore _sessionStore;
		private object _cookie;
		private bool _disposed;

		/// <summary>
		///     Initializes a new instance of the <see cref="SessionDelegate" /> class.
		/// </summary>
		/// <param name="canClose">if set to <c>true</c> [can close].</param>
		/// <param name="inner">The inner.</param>
		/// <param name="sessionStore">The session store.</param>
		public SessionDelegate(bool canClose, ISession inner, ISessionStore sessionStore)
		{
			this.InnerSession = inner;
			this._sessionStore = sessionStore;
			this._canClose = canClose;
		}

		/// <summary>
		///     Gets the inner session.
		/// </summary>
		/// <value>The inner session.</value>
		public ISession InnerSession { get; }

		/// <summary>
		///     Gets or sets the session store cookie.
		/// </summary>
		/// <value>The session store cookie.</value>
		public object SessionStoreCookie
		{
			get => this._cookie;
			set => this._cookie = value;
		}

		#region Dispose delegation

		/// <summary>
		///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
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
				conn = this.InnerSession.Close();
			}

			this.InnerSession.Dispose();

			this._disposed = true;

			return conn;
		}

		/// <summary>
		///     Returns <see langword="true" /> if the supplied sessions are equal, <see langword="false" /> otherwise.
		/// </summary>
		/// <param name="left">The left.</param>
		/// <param name="right">The right.</param>
		/// <returns></returns>
		public static bool AreEqual(ISession left, ISession right)
		{
			if (left is SessionDelegate sdLeft
			    && right is SessionDelegate sdRight)
			{
				return ReferenceEquals(sdLeft.InnerSession, sdRight.InnerSession);
			}

			throw new NotSupportedException($"AreEqual: left is {left.GetType().Name} and right is {right.GetType().Name}");
		}

		#region ISession delegation

		/// <inheritdoc />
		public IQueryable<T> Query<T>(string entityName)
		{
			return this.InnerSession.Query<T>(entityName);
		}

		/// <inheritdoc />
		public FlushMode FlushMode
		{
			get => this.InnerSession.FlushMode;
			set => this.InnerSession.FlushMode = value;
		}

		/// <summary>
		///     Get the <see cref="T:NHibernate.ISessionFactory" /> that created this instance.
		/// </summary>
		/// <value></value>
		public ISessionFactory SessionFactory => this.InnerSession.SessionFactory;

		/// <inheritdoc />
		public DbConnection Connection => this.InnerSession.Connection;

		/// <inheritdoc />
		public bool IsOpen => this.InnerSession.IsOpen;

		/// <inheritdoc />
		public bool IsConnected => this.InnerSession.IsConnected;

		/// <inheritdoc />
		public bool DefaultReadOnly
		{
			get => this.InnerSession.DefaultReadOnly;
			set => this.InnerSession.DefaultReadOnly = value;
		}

		/// <inheritdoc />
		/// <remarks>
		///     This method is implemented explicitly, as opposed to simply calling
		///     <see cref="SessionExtensions.GetCurrentTransaction(ISession)" />,
		///     because <see cref="ISession.GetSessionImplementation()" /> can return <see langword="null" />.
		/// </remarks>
		public ITransaction Transaction =>
			this.InnerSession?
				.GetSessionImplementation()?
				.ConnectionManager?
				.CurrentTransaction;

		/// <inheritdoc />
		DbConnection ISession.Close()
		{
			return this.InnerSession.Close();
		}

		/// <inheritdoc />
		public void CancelQuery()
		{
			this.InnerSession.CancelQuery();
		}

		/// <inheritdoc />
		public bool IsDirty()
		{
			return this.InnerSession.IsDirty();
		}

		/// <inheritdoc />
		public bool IsReadOnly(object entityOrProxy)
		{
			return this.InnerSession.IsReadOnly(entityOrProxy);
		}

		/// <inheritdoc />
		public void SetReadOnly(object entityOrProxy, bool readOnly)
		{
			this.InnerSession.SetReadOnly(entityOrProxy, readOnly);
		}

		/// <inheritdoc />
		public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.FlushAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task<bool> IsDirtyAsync(CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.IsDirtyAsync(cancellationToken);
		}

		/// <inheritdoc />
		public Task EvictAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.EvictAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(Type theType, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync(theType, id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync(entityName, id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(Type theType, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync(theType, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync<T>(id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync<T>(id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync(entityName, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task LoadAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LoadAsync(obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task ReplicateAsync(object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.ReplicateAsync(obj, replicationMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> SaveAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task SaveAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveAsync(obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> SaveAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task SaveAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveAsync(entityName, obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task SaveOrUpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveOrUpdateAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task SaveOrUpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveOrUpdateAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task SaveOrUpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task UpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.UpdateAsync(entityName, obj, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> MergeAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.MergeAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> MergeAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.MergeAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
		{
			return this.InnerSession.MergeAsync(entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task<T> MergeAsync<T>(string entityName, T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
		{
			return this.InnerSession.MergeAsync(entityName, entity, cancellationToken);
		}

		/// <inheritdoc />
		public Task PersistAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.PersistAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task PersistAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.PersistAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task DeleteAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task DeleteAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(entityName, obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task<int> DeleteAsync(string query, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(query, cancellationToken);
		}

		/// <inheritdoc />
		public Task<int> DeleteAsync(string query, object value, IType type, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(query, value, type, cancellationToken);
		}

		/// <inheritdoc />
		public Task<int> DeleteAsync(string query, object[] values, IType[] types, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.DeleteAsync(query, values, types, cancellationToken);
		}

		/// <inheritdoc />
		public Task LockAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LockAsync(obj, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task LockAsync(string entityName, object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.LockAsync(entityName, obj, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public Task RefreshAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.RefreshAsync(obj, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.CreateFilterAsync(collection, queryString, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> GetAsync(Type clazz, object id, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync(clazz, id, cancellationToken);
		}

		/// <inheritdoc />
		public Task<object> GetAsync(Type clazz, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync(clazz, id, lockMode, cancellationToken);
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
		public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetAsync<T>(id, lockMode, cancellationToken);
		}

		/// <inheritdoc />
		public Task<string> GetEntityNameAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
		{
			return this.InnerSession.GetEntityNameAsync(obj, cancellationToken);
		}

		/// <inheritdoc />
		public ISharedSessionBuilder SessionWithOptions()
		{
			return this.InnerSession.SessionWithOptions();
		}

		/// <inheritdoc />
		public void Flush()
		{
			this.InnerSession.Flush();
		}

		/// <inheritdoc />
		DbConnection ISession.Disconnect()
		{
			return this.InnerSession.Disconnect();
		}

		/// <summary>
		///     Disconnect the <c>ISession</c> from the current ADO.NET connection.
		/// </summary>
		/// <returns>
		///     The connection provided by the application or <see langword="null" />
		/// </returns>
		/// <remarks>
		///     If the connection was obtained by Hibernate, close it or return it to the connection
		///     pool. Otherwise return it to the application. This is used by applications which require
		///     long transactions.
		/// </remarks>
		public IDbConnection Disconnect()
		{
			return this.InnerSession.Disconnect();
		}

		/// <inheritdoc />
		public void Reconnect()
		{
			this.InnerSession.Reconnect();
		}

		/// <inheritdoc />
		public void Reconnect(DbConnection connection)
		{
			this.InnerSession.Reconnect(connection);
		}

		/// <inheritdoc />
		public object GetIdentifier(object obj)
		{
			return this.InnerSession.GetIdentifier(obj);
		}

		/// <inheritdoc />
		public bool Contains(object obj)
		{
			return this.InnerSession.Contains(obj);
		}

		/// <inheritdoc />
		public void Evict(object obj)
		{
			this.InnerSession.Evict(obj);
		}

		/// <inheritdoc />
		public object Load(Type theType, object id, LockMode lockMode)
		{
			return this.InnerSession.Load(theType, id, lockMode);
		}

		/// <inheritdoc />
		public object Load(string entityName, object id, LockMode lockMode)
		{
			return this.InnerSession.Load(entityName, id, lockMode);
		}

		/// <inheritdoc />
		public object Load(Type theType, object id)
		{
			return this.InnerSession.Load(theType, id);
		}

		/// <inheritdoc />
		public T Load<T>(object id, LockMode lockMode)
		{
			return this.InnerSession.Load<T>(id, lockMode);
		}

		/// <inheritdoc />
		public T Load<T>(object id)
		{
			return this.InnerSession.Load<T>(id);
		}

		/// <inheritdoc />
		public object Load(string entityName, object id)
		{
			return this.InnerSession.Load(entityName, id);
		}

		/// <inheritdoc />
		public void Load(object obj, object id)
		{
			this.InnerSession.Load(obj, id);
		}

		/// <inheritdoc />
		public object Get(Type clazz, object id)
		{
			return this.InnerSession.Get(clazz, id);
		}

		/// <inheritdoc />
		public object Get(Type clazz, object id, LockMode lockMode)
		{
			return this.InnerSession.Get(clazz, id, lockMode);
		}

		/// <inheritdoc />
		public ISessionImplementor GetSessionImplementation()
		{
			return this.InnerSession.GetSessionImplementation();
		}

#pragma warning disable 0618, 0612
		/// <inheritdoc />
		public ISession GetSession(EntityMode entityMode)
		{
			return this.InnerSession.GetSession(entityMode);
		}
#pragma warning restore 0618, 0612

		/// <inheritdoc />
		public IQueryable<T> Query<T>()
		{
			return this.InnerSession.Query<T>();
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
		public T Get<T>(object id, LockMode lockMode)
		{
			return this.InnerSession.Get<T>(id, lockMode);
		}

		/// <inheritdoc />
		public IFilter EnableFilter(string filterName)
		{
			return this.InnerSession.EnableFilter(filterName);
		}

		/// <inheritdoc />
		public IFilter GetEnabledFilter(string filterName)
		{
			return this.InnerSession.GetEnabledFilter(filterName);
		}

		/// <inheritdoc />
		public void DisableFilter(string filterName)
		{
			this.InnerSession.DisableFilter(filterName);
		}

#pragma warning disable 0618, 0612
		/// <inheritdoc />
		public IMultiQuery CreateMultiQuery()
		{
			return this.InnerSession.CreateMultiQuery();
		}
#pragma warning restore 0618, 0612

		/// <inheritdoc />
		public void Replicate(object obj, ReplicationMode replicationMode)
		{
			this.InnerSession.Replicate(obj, replicationMode);
		}

		/// <inheritdoc />
		public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
		{
			this.InnerSession.Replicate(entityName, obj, replicationMode);
		}

		/// <inheritdoc />
		public object Save(object obj)
		{
			return this.InnerSession.Save(obj);
		}

		/// <inheritdoc />
		public void Save(object obj, object id)
		{
			this.InnerSession.Save(obj, id);
		}

		/// <inheritdoc />
		public object Save(string entityName, object obj)
		{
			return this.InnerSession.Save(entityName, obj);
		}

		/// <inheritdoc />
		public void Save(string entityName, object obj, object id)
		{
			this.InnerSession.Save(entityName, obj, id);
		}

		/// <inheritdoc />
		public void SaveOrUpdate(object obj)
		{
			this.InnerSession.SaveOrUpdate(obj);
		}

		/// <inheritdoc />
		public void SaveOrUpdate(string entityName, object obj)
		{
			this.InnerSession.SaveOrUpdate(entityName, obj);
		}

		/// <inheritdoc />
		public void SaveOrUpdate(string entityName, object obj, object id)
		{
			this.InnerSession.SaveOrUpdate(entityName, obj, id);
		}

		/// <inheritdoc />
		public void Update(object obj)
		{
			this.InnerSession.Update(obj);
		}

		/// <inheritdoc />
		public void Update(object obj, object id)
		{
			this.InnerSession.Update(obj, id);
		}

		/// <inheritdoc />
		public void Update(string entityName, object obj)
		{
			this.InnerSession.Update(entityName, obj);
		}

		/// <inheritdoc />
		public void Update(string entityName, object obj, object id)
		{
			this.InnerSession.Update(entityName, obj, id);
		}

		/// <inheritdoc />
		public object Merge(object obj)
		{
			return this.InnerSession.Merge(obj);
		}

		/// <inheritdoc />
		public object Merge(string entityName, object obj)
		{
			return this.InnerSession.Merge(entityName, obj);
		}

		/// <inheritdoc />
		public T Merge<T>(T entity) where T : class
		{
			return this.InnerSession.Merge(entity);
		}

		/// <inheritdoc />
		public T Merge<T>(string entityName, T entity) where T : class
		{
			return this.InnerSession.Merge(entityName, entity);
		}

		/// <inheritdoc />
		public void Persist(object obj)
		{
			this.InnerSession.Persist(obj);
		}

		/// <inheritdoc />
		public void Persist(string entityName, object obj)
		{
			this.InnerSession.Persist(entityName, obj);
		}

		/// <inheritdoc />
		public void Delete(object obj)
		{
			this.InnerSession.Delete(obj);
		}

		/// <inheritdoc />
		public void Delete(string entityName, object obj)
		{
			this.InnerSession.Delete(entityName, obj);
		}

		/// <inheritdoc />
		public int Delete(string query)
		{
			return this.InnerSession.Delete(query);
		}

		/// <inheritdoc />
		public int Delete(string query, object value, IType type)
		{
			return this.InnerSession.Delete(query, value, type);
		}

		/// <inheritdoc />
		public int Delete(string query, object[] values, IType[] types)
		{
			return this.InnerSession.Delete(query, values, types);
		}

		/// <inheritdoc />
		public void Lock(object obj, LockMode lockMode)
		{
			this.InnerSession.Lock(obj, lockMode);
		}

		/// <inheritdoc />
		public void Lock(string entityName, object obj, LockMode lockMode)
		{
			this.InnerSession.Lock(entityName, obj, lockMode);
		}

		/// <inheritdoc />
		public void Refresh(object obj)
		{
			this.InnerSession.Refresh(obj);
		}

		/// <inheritdoc />
		public void Refresh(object obj, LockMode lockMode)
		{
			this.InnerSession.Refresh(obj, lockMode);
		}

		/// <inheritdoc />
		public LockMode GetCurrentLockMode(object obj)
		{
			return this.InnerSession.GetCurrentLockMode(obj);
		}

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
		public ICriteria CreateCriteria<T>() where T : class
		{
			return this.InnerSession.CreateCriteria(typeof(T));
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria<T>(string alias) where T : class
		{
			return this.InnerSession.CreateCriteria(typeof(T), alias);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(Type persistentClass)
		{
			return this.InnerSession.CreateCriteria(persistentClass);
		}

		/// <inheritdoc />
		public ICriteria CreateCriteria(Type persistentClass, string alias)
		{
			return this.InnerSession.CreateCriteria(persistentClass, alias);
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
		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
		{
			return this.InnerSession.QueryOver<T>(entityName);
		}

		/// <inheritdoc />
		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
		{
			return this.InnerSession.QueryOver(entityName, alias);
		}

		/// <inheritdoc />
		public IQuery CreateQuery(string queryString)
		{
			return this.InnerSession.CreateQuery(queryString);
		}

		/// <inheritdoc />
		public IQuery CreateFilter(object collection, string queryString)
		{
			return this.InnerSession.CreateFilter(collection, queryString);
		}

		/// <inheritdoc />
		public IQuery GetNamedQuery(string queryName)
		{
			return this.InnerSession.GetNamedQuery(queryName);
		}

		/// <inheritdoc />
		public ISQLQuery CreateSQLQuery(string queryString)
		{
			return this.InnerSession.CreateSQLQuery(queryString);
		}

		/// <inheritdoc />
		public void Clear()
		{
			this.InnerSession.Clear();
		}

		/// <summary>
		///     End the <c>ISession</c> by disconnecting from the ADO.NET connection and cleaning up.
		/// </summary>
		/// <returns>
		///     The connection provided by the application or <see langword="null" />
		/// </returns>
		/// <remarks>
		///     It is not strictly necessary to <c>Close()</c> the <c>ISession</c> but you must
		///     at least <c>Disconnect()</c> it.
		/// </remarks>
		public IDbConnection Close()
		{
			return this.DoClose(true);
		}

		/// <inheritdoc />
		public string GetEntityName(object obj)
		{
			return this.InnerSession.GetEntityName(obj);
		}

		/// <inheritdoc />
		public ISession SetBatchSize(int batchSize)
		{
			return this.InnerSession.SetBatchSize(batchSize);
		}

#pragma warning disable 0618, 0612
		/// <inheritdoc />
		public IMultiCriteria CreateMultiCriteria()
		{
			return this.InnerSession.CreateMultiCriteria();
		}
#pragma warning restore 0618, 0612

		/// <inheritdoc />
		public CacheMode CacheMode
		{
			get => this.InnerSession.CacheMode;
			set => this.InnerSession.CacheMode = value;
		}

		/// <inheritdoc />
		public ISessionStatistics Statistics => this.InnerSession.Statistics;

		#endregion
	}
}