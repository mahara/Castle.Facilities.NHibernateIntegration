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

    /// <summary>
    /// Proxies an ISession so the user cannot close a session which is controlled by a transaction,
    /// or, when this is not the case, make sure to remove the session from the storage.
    /// <seealso cref="ISessionStore" />
    /// <seealso cref="ISessionManager" />
    /// </summary>
    /// <remarks>
    /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618
    /// <code>
    /// #pragma warning disable 0618, 0612
    /// #pragma warning restore 0618, 0612
    /// </code>
    /// </remarks>
    [Serializable]
    public class SessionDelegate : MarshalByRefObject, ISession
    {
        private readonly bool _canClose;
        private readonly ISessionStore _sessionStore;
        private object _cookie;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionDelegate" /> class.
        /// </summary>
        /// <param name="canClose">if set to <c>true</c> [can close].</param>
        /// <param name="inner">The inner.</param>
        /// <param name="sessionStore">The session store.</param>
        public SessionDelegate(bool canClose, ISession inner, ISessionStore sessionStore)
        {
            InnerSession = inner;
            _sessionStore = sessionStore;
            _canClose = canClose;
        }

        /// <summary>
        /// Gets the inner session.
        /// </summary>
        /// <value>The inner session.</value>
        public ISession InnerSession { get; }

        /// <summary>
        /// Gets or sets the session store cookie.
        /// </summary>
        /// <value>The session store cookie.</value>
        public object SessionStoreCookie
        {
            get => _cookie;
            set => _cookie = value;
        }

        #region Dispose delegation

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DoClose(false);
        }

        #endregion

        /// <summary>
        /// Does the close.
        /// </summary>
        /// <param name="closing">if set to <c>true</c> [closing].</param>
        /// <returns></returns>
        protected IDbConnection DoClose(bool closing)
        {
            if (_disposed)
            {
                return null;
            }

            if (_canClose)
            {
                return InternalClose(closing);
            }

            return null;
        }

        internal IDbConnection InternalClose(bool closing)
        {
            IDbConnection connection = null;

            _sessionStore.Remove(this);

            if (closing)
            {
                connection = InnerSession.Close();
            }

            InnerSession.Dispose();

            _disposed = true;

            return connection;
        }

        /// <summary>
        /// Returns <see langword="true" /> if the supplied sessions are equal, <see langword="false" /> otherwise.
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
            return InnerSession.Query<T>(entityName);
        }

        /// <inheritdoc />
        public FlushMode FlushMode
        {
            get => InnerSession.FlushMode;
            set => InnerSession.FlushMode = value;
        }

        /// <summary>
        /// Get the <see cref="T:NHibernate.ISessionFactory" /> that created this instance.
        /// </summary>
        /// <value></value>
        public ISessionFactory SessionFactory =>
            InnerSession.SessionFactory;

        /// <inheritdoc />
        public DbConnection Connection =>
            InnerSession.Connection;

        /// <inheritdoc />
        public bool IsOpen =>
            InnerSession.IsOpen;

        /// <inheritdoc />
        public bool IsConnected =>
            InnerSession.IsConnected;

        /// <inheritdoc />
        public bool DefaultReadOnly
        {
            get => InnerSession.DefaultReadOnly;
            set => InnerSession.DefaultReadOnly = value;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method is implemented explicitly, as opposed to simply calling
        /// <see cref="SessionExtensions.GetCurrentTransaction(ISession)" />,
        /// because <see cref="ISession.GetSessionImplementation()" /> can return <see langword="null" />.
        /// </remarks>
        public ITransaction Transaction =>
            InnerSession?.GetSessionImplementation()?
                         .ConnectionManager?
                         .CurrentTransaction;

        /// <inheritdoc />
        DbConnection ISession.Close()
        {
            return (DbConnection) DoClose(true);
        }

        /// <inheritdoc />
        public void CancelQuery()
        {
            InnerSession.CancelQuery();
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            return InnerSession.IsDirty();
        }

        /// <inheritdoc />
        public bool IsReadOnly(object entityOrProxy)
        {
            return InnerSession.IsReadOnly(entityOrProxy);
        }

        /// <inheritdoc />
        public void SetReadOnly(object entityOrProxy, bool readOnly)
        {
            InnerSession.SetReadOnly(entityOrProxy, readOnly);
        }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsDirtyAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.IsDirtyAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task EvictAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.EvictAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(Type theType, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(theType, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(entityName, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(Type theType, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(theType, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync<T>(id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync<T>(id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(entityName, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task LoadAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task ReplicateAsync(object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.ReplicateAsync(obj, replicationMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> SaveAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> SaveAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entityName, obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entityName, obj, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> MergeAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.MergeAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> MergeAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.MergeAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            return InnerSession.MergeAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> MergeAsync<T>(string entityName, T entity, CancellationToken cancellationToken = new CancellationToken()) where T : class
        {
            return InnerSession.MergeAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task PersistAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.PersistAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task PersistAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.PersistAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string entityName, object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(entityName, obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> DeleteAsync(string query, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(query, cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> DeleteAsync(string query, object value, IType type, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(query, value, type, cancellationToken);
        }

        /// <inheritdoc />
        public Task<int> DeleteAsync(string query, object[] values, IType[] types, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(query, values, types, cancellationToken);
        }

        /// <inheritdoc />
        public Task LockAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LockAsync(obj, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task LockAsync(string entityName, object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LockAsync(entityName, obj, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task RefreshAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public Task RefreshAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(obj, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.CreateFilterAsync(collection, queryString, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> GetAsync(Type clazz, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(clazz, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> GetAsync(Type clazz, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(clazz, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(entityName, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync<T>(id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync<T>(id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> GetEntityNameAsync(object obj, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetEntityNameAsync(obj, cancellationToken);
        }

        /// <inheritdoc />
        public ISharedSessionBuilder SessionWithOptions()
        {
            return InnerSession.SessionWithOptions();
        }

        /// <inheritdoc />
        public void Flush()
        {
            InnerSession.Flush();
        }

        /// <inheritdoc />
        DbConnection ISession.Disconnect()
        {
            return InnerSession.Disconnect();
        }

        /// <summary>
        /// Disconnect the <c>ISession</c> from the current ADO.NET connection.
        /// </summary>
        /// <returns>
        /// The connection provided by the application or <see langword="null" />.
        /// </returns>
        /// <remarks>
        /// If the connection was obtained by Hibernate, close it or return it to the connection pool.
        /// Otherwise return it to the application. This is used by applications which require long transactions.
        /// </remarks>
        public IDbConnection Disconnect()
        {
            return InnerSession.Disconnect();
        }

        /// <inheritdoc />
        public void Reconnect()
        {
            InnerSession.Reconnect();
        }

        /// <inheritdoc />
        public void Reconnect(DbConnection connection)
        {
            InnerSession.Reconnect(connection);
        }

        /// <inheritdoc />
        public object GetIdentifier(object obj)
        {
            return InnerSession.GetIdentifier(obj);
        }

        /// <inheritdoc />
        public bool Contains(object obj)
        {
            return InnerSession.Contains(obj);
        }

        /// <inheritdoc />
        public void Evict(object obj)
        {
            InnerSession.Evict(obj);
        }

        /// <inheritdoc />
        public object Load(Type theType, object id, LockMode lockMode)
        {
            return InnerSession.Load(theType, id, lockMode);
        }

        /// <inheritdoc />
        public object Load(string entityName, object id, LockMode lockMode)
        {
            return InnerSession.Load(entityName, id, lockMode);
        }

        /// <inheritdoc />
        public object Load(Type theType, object id)
        {
            return InnerSession.Load(theType, id);
        }

        /// <inheritdoc />
        public T Load<T>(object id, LockMode lockMode)
        {
            return InnerSession.Load<T>(id, lockMode);
        }

        /// <inheritdoc />
        public T Load<T>(object id)
        {
            return InnerSession.Load<T>(id);
        }

        /// <inheritdoc />
        public object Load(string entityName, object id)
        {
            return InnerSession.Load(entityName, id);
        }

        /// <inheritdoc />
        public void Load(object obj, object id)
        {
            InnerSession.Load(obj, id);
        }

        /// <inheritdoc />
        public object Get(Type clazz, object id)
        {
            return InnerSession.Get(clazz, id);
        }

        /// <inheritdoc />
        public object Get(Type clazz, object id, LockMode lockMode)
        {
            return InnerSession.Get(clazz, id, lockMode);
        }

        /// <inheritdoc />
        public ISessionImplementor GetSessionImplementation()
        {
            return InnerSession.GetSessionImplementation();
        }

#pragma warning disable 0618, 0612
        /// <inheritdoc />
        public ISession GetSession(EntityMode entityMode)
        {
            return InnerSession.GetSession(entityMode);
        }
#pragma warning restore 0618, 0612

        /// <inheritdoc />
        public IQueryable<T> Query<T>()
        {
            return InnerSession.Query<T>();
        }

        /// <inheritdoc />
        public object Get(string entityName, object id)
        {
            return InnerSession.Get(entityName, id);
        }

        /// <inheritdoc />
        public T Get<T>(object id)
        {
            return InnerSession.Get<T>(id);
        }

        /// <inheritdoc />
        public T Get<T>(object id, LockMode lockMode)
        {
            return InnerSession.Get<T>(id, lockMode);
        }

        /// <inheritdoc />
        public IFilter EnableFilter(string filterName)
        {
            return InnerSession.EnableFilter(filterName);
        }

        /// <inheritdoc />
        public IFilter GetEnabledFilter(string filterName)
        {
            return InnerSession.GetEnabledFilter(filterName);
        }

        /// <inheritdoc />
        public void DisableFilter(string filterName)
        {
            InnerSession.DisableFilter(filterName);
        }

#pragma warning disable 0618, 0612
        /// <inheritdoc />
        public IMultiQuery CreateMultiQuery()
        {
            return InnerSession.CreateMultiQuery();
        }
#pragma warning restore 0618, 0612

        /// <inheritdoc />
        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            InnerSession.Replicate(obj, replicationMode);
        }

        /// <inheritdoc />
        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            InnerSession.Replicate(entityName, obj, replicationMode);
        }

        /// <inheritdoc />
        public object Save(object obj)
        {
            return InnerSession.Save(obj);
        }

        /// <inheritdoc />
        public void Save(object obj, object id)
        {
            InnerSession.Save(obj, id);
        }

        /// <inheritdoc />
        public object Save(string entityName, object obj)
        {
            return InnerSession.Save(entityName, obj);
        }

        /// <inheritdoc />
        public void Save(string entityName, object obj, object id)
        {
            InnerSession.Save(entityName, obj, id);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(object obj)
        {
            InnerSession.SaveOrUpdate(obj);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(string entityName, object obj)
        {
            InnerSession.SaveOrUpdate(entityName, obj);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(string entityName, object obj, object id)
        {
            InnerSession.SaveOrUpdate(entityName, obj, id);
        }

        /// <inheritdoc />
        public void Update(object obj)
        {
            InnerSession.Update(obj);
        }

        /// <inheritdoc />
        public void Update(object obj, object id)
        {
            InnerSession.Update(obj, id);
        }

        /// <inheritdoc />
        public void Update(string entityName, object obj)
        {
            InnerSession.Update(entityName, obj);
        }

        /// <inheritdoc />
        public void Update(string entityName, object obj, object id)
        {
            InnerSession.Update(entityName, obj, id);
        }

        /// <inheritdoc />
        public object Merge(object obj)
        {
            return InnerSession.Merge(obj);
        }

        /// <inheritdoc />
        public object Merge(string entityName, object obj)
        {
            return InnerSession.Merge(entityName, obj);
        }

        /// <inheritdoc />
        public T Merge<T>(T entity) where T : class
        {
            return InnerSession.Merge(entity);
        }

        /// <inheritdoc />
        public T Merge<T>(string entityName, T entity) where T : class
        {
            return InnerSession.Merge(entityName, entity);
        }

        /// <inheritdoc />
        public void Persist(object obj)
        {
            InnerSession.Persist(obj);
        }

        /// <inheritdoc />
        public void Persist(string entityName, object obj)
        {
            InnerSession.Persist(entityName, obj);
        }

        /// <inheritdoc />
        public void Delete(object obj)
        {
            InnerSession.Delete(obj);
        }

        /// <inheritdoc />
        public void Delete(string entityName, object obj)
        {
            InnerSession.Delete(entityName, obj);
        }

        /// <inheritdoc />
        public int Delete(string query)
        {
            return InnerSession.Delete(query);
        }

        /// <inheritdoc />
        public int Delete(string query, object value, IType type)
        {
            return InnerSession.Delete(query, value, type);
        }

        /// <inheritdoc />
        public int Delete(string query, object[] values, IType[] types)
        {
            return InnerSession.Delete(query, values, types);
        }

        /// <inheritdoc />
        public void Lock(object obj, LockMode lockMode)
        {
            InnerSession.Lock(obj, lockMode);
        }

        /// <inheritdoc />
        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            InnerSession.Lock(entityName, obj, lockMode);
        }

        /// <inheritdoc />
        public void Refresh(object obj)
        {
            InnerSession.Refresh(obj);
        }

        /// <inheritdoc />
        public void Refresh(object obj, LockMode lockMode)
        {
            InnerSession.Refresh(obj, lockMode);
        }

        /// <inheritdoc />
        public LockMode GetCurrentLockMode(object obj)
        {
            return InnerSession.GetCurrentLockMode(obj);
        }

        /// <inheritdoc />
        public ITransaction BeginTransaction()
        {
            return InnerSession.BeginTransaction();
        }

        /// <inheritdoc />
        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return InnerSession.BeginTransaction(isolationLevel);
        }

        /// <inheritdoc />
        public void JoinTransaction()
        {
            InnerSession.JoinTransaction();
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria<T>() where T : class
        {
            return InnerSession.CreateCriteria(typeof(T));
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return InnerSession.CreateCriteria(typeof(T), alias);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(Type persistentClass)
        {
            return InnerSession.CreateCriteria(persistentClass);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(Type persistentClass, string alias)
        {
            return InnerSession.CreateCriteria(persistentClass, alias);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(string entityName)
        {
            return InnerSession.CreateCriteria(entityName);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return InnerSession.CreateCriteria(entityName, alias);
        }

        /// <inheritdoc />
        public IQueryOver<T, T> QueryOver<T>() where T : class
        {
            return InnerSession.QueryOver<T>();
        }

        /// <inheritdoc />
        public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return InnerSession.QueryOver(alias);
        }

        /// <inheritdoc />
        public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
        {
            return InnerSession.QueryOver<T>(entityName);
        }

        /// <inheritdoc />
        public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
        {
            return InnerSession.QueryOver(entityName, alias);
        }

        /// <inheritdoc />
        public IQuery CreateQuery(string queryString)
        {
            return InnerSession.CreateQuery(queryString);
        }

        /// <inheritdoc />
        public IQuery CreateFilter(object collection, string queryString)
        {
            return InnerSession.CreateFilter(collection, queryString);
        }

        /// <inheritdoc />
        public IQuery GetNamedQuery(string queryName)
        {
            return InnerSession.GetNamedQuery(queryName);
        }

        /// <inheritdoc />
        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return InnerSession.CreateSQLQuery(queryString);
        }

        /// <inheritdoc />
        public void Clear()
        {
            InnerSession.Clear();
        }

        /// <summary>
        /// End the <c>ISession</c> by disconnecting from the ADO.NET connection and cleaning up.
        /// </summary>
        /// <returns>
        /// The connection provided by the application or <see langword="null" />.
        /// </returns>
        /// <remarks>
        /// It is not strictly necessary to <c>Close()</c> the <c>ISession</c>
        /// but you must at least <c>Disconnect()</c> it.
        /// </remarks>
        public IDbConnection Close()
        {
            return DoClose(true);
        }

        /// <inheritdoc />
        public string GetEntityName(object obj)
        {
            return InnerSession.GetEntityName(obj);
        }

        /// <inheritdoc />
        public ISession SetBatchSize(int batchSize)
        {
            return InnerSession.SetBatchSize(batchSize);
        }

#pragma warning disable 0618, 0612
        /// <inheritdoc />
        public IMultiCriteria CreateMultiCriteria()
        {
            return InnerSession.CreateMultiCriteria();
        }
#pragma warning restore 0618, 0612

        /// <inheritdoc />
        public CacheMode CacheMode
        {
            get => InnerSession.CacheMode;
            set => InnerSession.CacheMode = value;
        }

        /// <inheritdoc />
        public ISessionStatistics Statistics => InnerSession.Statistics;

        #endregion
    }
}