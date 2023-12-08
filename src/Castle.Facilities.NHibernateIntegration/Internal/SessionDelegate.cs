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
        /// <param name="innerSession">The inner session.</param>
        /// <param name="sessionStore">The session store.</param>
        /// <param name="canClose">Set to <c>true</c> if can close the session.</param>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618
        /// <code>
        /// #pragma warning disable 0618, 0612
        /// #pragma warning restore 0618, 0612
        /// </code>
        /// </remarks>
        public SessionDelegate(ISession innerSession, ISessionStore sessionStore, bool canClose)
        {
            InnerSession = innerSession;
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DoClose(false);
        }

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
            if (left is SessionDelegate sdLeft &&
                right is SessionDelegate sdRight)
            {
                return ReferenceEquals(sdLeft.InnerSession, sdRight.InnerSession);
            }

            throw new NotSupportedException($"{nameof(AreEqual)}: left is {left.GetType().Name} and right is {right.GetType().Name}.");
        }

        #region ISession Members

        /// <inheritdoc />
        public ISessionFactory SessionFactory =>
            InnerSession.SessionFactory;

        /// <inheritdoc />
        public ISessionStatistics Statistics =>
            InnerSession.Statistics;

        /// <inheritdoc />
        public FlushMode FlushMode
        {
            get => InnerSession.FlushMode;
            set => InnerSession.FlushMode = value;
        }

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
        public CacheMode CacheMode
        {
            get => InnerSession.CacheMode;
            set => InnerSession.CacheMode = value;
        }

        /// <inheritdoc />
        public ISessionImplementor GetSessionImplementation()
        {
            return InnerSession.GetSessionImplementation();
        }

        /// <inheritdoc />
        public ISharedSessionBuilder SessionWithOptions()
        {
            return InnerSession.SessionWithOptions();
        }

        /// <inheritdoc />
        public DbConnection Close()
        {
            return (DbConnection) DoClose(true);
        }

        /// <inheritdoc />
        DbConnection ISession.Disconnect()
        {
            return InnerSession.Disconnect();
        }

        /// <inheritdoc />
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
        public void Flush()
        {
            InnerSession.Flush();
        }

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public bool IsDirty()
        {
            return InnerSession.IsDirty();
        }

        /// <inheritdoc />
        public Task<bool> IsDirtyAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.IsDirtyAsync(cancellationToken);
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
        public ISession SetBatchSize(int batchSize)
        {
            return InnerSession.SetBatchSize(batchSize);
        }

        /// <inheritdoc />
        public IQueryable<T> Query<T>()
        {
            return InnerSession.Query<T>();
        }

        /// <inheritdoc />
        public IQueryable<T> Query<T>(string entityName)
        {
            return InnerSession.Query<T>(entityName);
        }

        /// <inheritdoc />
        public void CancelQuery()
        {
            InnerSession.CancelQuery();
        }

        /// <inheritdoc />
        public IQuery GetNamedQuery(string queryName)
        {
            return InnerSession.GetNamedQuery(queryName);
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
        public IFilter GetEnabledFilter(string filterName)
        {
            return InnerSession.GetEnabledFilter(filterName);
        }

        /// <inheritdoc />
        public IFilter EnableFilter(string filterName)
        {
            return InnerSession.EnableFilter(filterName);
        }

        /// <inheritdoc />
        public void DisableFilter(string filterName)
        {
            InnerSession.DisableFilter(filterName);
        }

        /// <inheritdoc />
        public IQuery CreateFilter(object collection, string queryString)
        {
            return InnerSession.CreateFilter(collection, queryString);
        }

        /// <inheritdoc />
        public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.CreateFilterAsync(collection, queryString, cancellationToken);
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
        public ICriteria CreateCriteria(Type type)
        {
            return InnerSession.CreateCriteria(type);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(Type type, string alias)
        {
            return InnerSession.CreateCriteria(type, alias);
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
        public IQuery CreateQuery(string queryString)
        {
            return InnerSession.CreateQuery(queryString);
        }

        /// <inheritdoc />
        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return InnerSession.CreateSQLQuery(queryString);
        }

#pragma warning disable 0618, 0612
        /// <inheritdoc />
        public IMultiQuery CreateMultiQuery()
        {
            return InnerSession.CreateMultiQuery();
        }

        /// <inheritdoc />
        public IMultiCriteria CreateMultiCriteria()
        {
            return InnerSession.CreateMultiCriteria();
        }

        /// <inheritdoc />
        public ISession GetSession(EntityMode entityMode)
        {
            return InnerSession.GetSession(entityMode);
        }
#pragma warning restore 0618, 0612

        /// <inheritdoc />
        public LockMode GetCurrentLockMode(object entity)
        {
            return InnerSession.GetCurrentLockMode(entity);
        }

        /// <inheritdoc />
        public void Lock(object entity, LockMode lockMode)
        {
            InnerSession.Lock(entity, lockMode);
        }

        /// <inheritdoc />
        public void Lock(string entityName, object entity, LockMode lockMode)
        {
            InnerSession.Lock(entityName, entity, lockMode);
        }

        /// <inheritdoc />
        public Task LockAsync(object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LockAsync(entity, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task LockAsync(string entityName, object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LockAsync(entityName, entity, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public object GetIdentifier(object entity)
        {
            return InnerSession.GetIdentifier(entity);
        }

        /// <inheritdoc />
        public string GetEntityName(object entity)
        {
            return InnerSession.GetEntityName(entity);
        }

        /// <inheritdoc />
        public Task<string> GetEntityNameAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetEntityNameAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public bool Contains(object entity)
        {
            return InnerSession.Contains(entity);
        }

        /// <inheritdoc />
        public void Clear()
        {
            InnerSession.Clear();
        }

        /// <inheritdoc />
        public void Evict(object entity)
        {
            InnerSession.Evict(entity);
        }

        /// <inheritdoc />
        public Task EvictAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.EvictAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public T Load<T>(object id)
        {
            return InnerSession.Load<T>(id);
        }

        /// <inheritdoc />
        public T Load<T>(object id, LockMode lockMode)
        {
            return InnerSession.Load<T>(id, lockMode);
        }

        /// <inheritdoc />
        public object Load(Type type, object id)
        {
            return InnerSession.Load(type, id);
        }

        /// <inheritdoc />
        public object Load(Type type, object id, LockMode lockMode)
        {
            return InnerSession.Load(type, id, lockMode);
        }

        /// <inheritdoc />
        public object Load(string entityName, object id, LockMode lockMode)
        {
            return InnerSession.Load(entityName, id, lockMode);
        }

        /// <inheritdoc />
        public object Load(string entityName, object id)
        {
            return InnerSession.Load(entityName, id);
        }

        /// <inheritdoc />
        public void Load(object entity, object id)
        {
            InnerSession.Load(entity, id);
        }

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync<T>(id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync<T>(id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(Type type, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(type, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(Type type, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(type, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(entityName, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(entityName, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task LoadAsync(object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.LoadAsync(entity, id, cancellationToken);
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
        public object Get(Type type, object id)
        {
            return InnerSession.Get(type, id);
        }

        /// <inheritdoc />
        public object Get(Type type, object id, LockMode lockMode)
        {
            return InnerSession.Get(type, id, lockMode);
        }

        /// <inheritdoc />
        public object Get(string entityName, object id)
        {
            return InnerSession.Get(entityName, id);
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
        public Task<object> GetAsync(Type type, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(type, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> GetAsync(Type type, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(type, id, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(entityName, id, cancellationToken);
        }

        /// <inheritdoc />
        public void Refresh(object entity)
        {
            InnerSession.Refresh(entity);
        }

        /// <inheritdoc />
        public void Refresh(object entity, LockMode lockMode)
        {
            InnerSession.Refresh(entity, lockMode);
        }

        /// <inheritdoc />
        public Task RefreshAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task RefreshAsync(object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(entity, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public void Replicate(object entity, ReplicationMode replicationMode)
        {
            InnerSession.Replicate(entity, replicationMode);
        }

        /// <inheritdoc />
        public void Replicate(string entityName, object entity, ReplicationMode replicationMode)
        {
            InnerSession.Replicate(entityName, entity, replicationMode);
        }

        /// <inheritdoc />
        public Task ReplicateAsync(object entity, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.ReplicateAsync(entity, replicationMode, cancellationToken);
        }

        /// <inheritdoc />
        public Task ReplicateAsync(string entityName, object entity, ReplicationMode replicationMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.ReplicateAsync(entityName, entity, replicationMode, cancellationToken);
        }

        /// <inheritdoc />
        public object Save(object entity)
        {
            return InnerSession.Save(entity);
        }

        /// <inheritdoc />
        public void Save(object entity, object id)
        {
            InnerSession.Save(entity, id);
        }

        /// <inheritdoc />
        public object Save(string entityName, object entity)
        {
            return InnerSession.Save(entityName, entity);
        }

        /// <inheritdoc />
        public void Save(string entityName, object entity, object id)
        {
            InnerSession.Save(entityName, entity, id);
        }

        /// <inheritdoc />
        public Task<object> SaveAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveAsync(object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entity, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> SaveAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveAsync(string entityName, object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveAsync(entityName, entity, id, cancellationToken);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(object entity)
        {
            InnerSession.SaveOrUpdate(entity);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(string entityName, object entity)
        {
            InnerSession.SaveOrUpdate(entityName, entity);
        }

        /// <inheritdoc />
        public void SaveOrUpdate(string entityName, object entity, object id)
        {
            InnerSession.SaveOrUpdate(entityName, entity, id);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task SaveOrUpdateAsync(string entityName, object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.SaveOrUpdateAsync(entityName, entity, id, cancellationToken);
        }

        /// <inheritdoc />
        public void Update(object entity)
        {
            InnerSession.Update(entity);
        }

        /// <inheritdoc />
        public void Update(object entity, object id)
        {
            InnerSession.Update(entity, id);
        }

        /// <inheritdoc />
        public void Update(string entityName, object entity)
        {
            InnerSession.Update(entityName, entity);
        }

        /// <inheritdoc />
        public void Update(string entityName, object entity, object id)
        {
            InnerSession.Update(entityName, entity, id);
        }

        /// <inheritdoc />
        public Task UpdateAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entity, id, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(string entityName, object entity, object id, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entityName, entity, id, cancellationToken);
        }

        /// <inheritdoc />
        public object Merge(object entity)
        {
            return InnerSession.Merge(entity);
        }

        /// <inheritdoc />
        public object Merge(string entityName, object entity)
        {
            return InnerSession.Merge(entityName, entity);
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
        public Task<object> MergeAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.MergeAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> MergeAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.MergeAsync(entityName, entity, cancellationToken);
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
        public void Persist(object entity)
        {
            InnerSession.Persist(entity);
        }

        /// <inheritdoc />
        public void Persist(string entityName, object entity)
        {
            InnerSession.Persist(entityName, entity);
        }

        /// <inheritdoc />
        public Task PersistAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.PersistAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task PersistAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.PersistAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public void Delete(object entity)
        {
            InnerSession.Delete(entity);
        }

        /// <inheritdoc />
        public void Delete(string entityName, object entity)
        {
            InnerSession.Delete(entityName, entity);
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
        public Task DeleteAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(entityName, entity, cancellationToken);
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

        #endregion
    }
}
