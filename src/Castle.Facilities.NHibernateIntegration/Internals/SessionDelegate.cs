#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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
using System.Collections;
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

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Proxies an <see cref="ISession" /> so the user cannot close a session which is controlled by a transaction,
    /// or, when this is not the case, make sure to remove the session from the storage.
    /// </summary>
    /// <remarks>
    /// <seealso cref="ISessionStore" />
    /// <seealso cref="ISessionManager" />
    /// </remarks>
    [Serializable]
    public class SessionDelegate : MarshalByRefObject, ISession
    {
        private readonly ISession _innerSession;
        private readonly ISessionStore _sessionStore;
        private readonly bool _canClose;

        private bool _isDisposed;

        public SessionDelegate(ISession inner, ISessionStore sessionStore, bool canClose)
        {
            _innerSession = inner;
            _sessionStore = sessionStore;
            _canClose = canClose;
        }

        public void Dispose()
        {
            CloseConnection(false);

            GC.SuppressFinalize(this);
        }

        protected DbConnection CloseConnection(bool closing)
        {
            if (_isDisposed)
            {
                return null;
            }

            if (_canClose)
            {
                return CloseConnectionCore(closing);
            }

            return null;
        }

        internal DbConnection CloseConnectionCore(bool closing)
        {
            DbConnection connection = null;

            _sessionStore.Remove(this);

            if (closing)
            {
                connection = _innerSession.Close();
            }

            _innerSession.Dispose();

            _isDisposed = true;

            return connection;
        }

        /// <summary>
        /// Gets the inner session.
        /// </summary>
        /// <value>The inner session.</value>
        public ISession InnerSession => _innerSession;

        /// <summary>
        /// Gets or sets the session store cookie.
        /// </summary>
        /// <value>The session store cookie.</value>
        public object SessionStoreCookie { get; set; }

        #region ISession Members

        public ISessionImplementor GetSessionImplementation()
        {
            return _innerSession.GetSessionImplementation();
        }

        public FlushMode FlushMode
        {
            get => _innerSession.FlushMode;
            set => _innerSession.FlushMode = value;
        }

        public ISessionFactory SessionFactory => _innerSession.SessionFactory;

        //
        //  TODO:   Update implementation in future version (5.3.x),
        //          following NHibernate 5.3.x.
        //
        //  NOTE:   SessionDelegate.Transaction, with slightly-modified implementation of ISession.GetCurrentTransaction(),
        //          is used here to workaround a mocking issue (in Facilities103 issue) of ISession.GetSessionImplementation().
        //
        /// <summary>
        /// Gets the current Unit of Work and returns the associated <see cref="ITransaction" /> instance.
        /// </summary>
        public ITransaction Transaction => _innerSession.Transaction;

        public DbConnection Connection => _innerSession.Connection;

        public bool IsConnected => _innerSession.IsConnected;

        public bool IsOpen => _innerSession.IsOpen;

        public bool DefaultReadOnly
        {
            get => _innerSession.DefaultReadOnly;
            set => _innerSession.DefaultReadOnly = value;
        }

        public CacheMode CacheMode
        {
            get => _innerSession.CacheMode;
            set => _innerSession.CacheMode = value;
        }

        public bool IsDirty()
        {
            return _innerSession.IsDirty();
        }

        public bool IsReadOnly(object entityOrProxy)
        {
            return _innerSession.IsReadOnly(entityOrProxy);
        }

        public void SetReadOnly(object entityOrProxy, bool readOnly)
        {
            _innerSession.SetReadOnly(entityOrProxy, readOnly);
        }

        public ISessionStatistics Statistics => _innerSession.Statistics;

        public DbConnection Close()
        {
            return CloseConnection(true);
        }

        public DbConnection Disconnect()
        {
            return _innerSession.Disconnect();
        }

        public void Reconnect()
        {
            _innerSession.Reconnect();
        }

        public void Reconnect(DbConnection connection)
        {
            _innerSession.Reconnect(connection);
        }

        public void Flush()
        {
            _innerSession.Flush();
        }

        public void Clear()
        {
            _innerSession.Clear();
        }

        public void Evict(object obj)
        {
            _innerSession.Evict(obj);
        }

        public bool Contains(object obj)
        {
            return _innerSession.Contains(obj);
        }

        public object GetIdentifier(object obj)
        {
            return _innerSession.GetIdentifier(obj);
        }

        public string GetEntityName(object obj)
        {
            return _innerSession.GetEntityName(obj);
        }

        public ISharedSessionBuilder SessionWithOptions()
        {
            return _innerSession.SessionWithOptions();
        }

        public ISession SetBatchSize(int batchSize)
        {
            return _innerSession.SetBatchSize(batchSize);
        }

        public ITransaction BeginTransaction()
        {
            return _innerSession.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return _innerSession.BeginTransaction(isolationLevel);
        }

        public void JoinTransaction()
        {
            _innerSession.JoinTransaction();
        }

        public LockMode GetCurrentLockMode(object obj)
        {
            return _innerSession.GetCurrentLockMode(obj);
        }

        public void Lock(object obj, LockMode lockMode)
        {
            _innerSession.Lock(obj, lockMode);
        }

        public void Lock(string entityName, object obj, LockMode lockMode)
        {
            _innerSession.Lock(entityName, obj, lockMode);
        }

        public void Refresh(object obj)
        {
            _innerSession.Refresh(obj);
        }

        public void Refresh(object obj, LockMode lockMode)
        {
            _innerSession.Refresh(obj, lockMode);
        }

        public T Load<T>(object id)
        {
            return _innerSession.Load<T>(id);
        }

        public T Load<T>(object id, LockMode lockMode)
        {
            return _innerSession.Load<T>(id, lockMode);
        }

        public object Load(Type theType, object id)
        {
            return _innerSession.Load(theType, id);
        }

        public object Load(Type theType, object id, LockMode lockMode)
        {
            return _innerSession.Load(theType, id, lockMode);
        }

        public object Load(string entityName, object id)
        {
            return _innerSession.Load(entityName, id);
        }

        public object Load(string entityName, object id, LockMode lockMode)
        {
            return _innerSession.Load(entityName, id, lockMode);
        }

        public void Load(object obj, object id)
        {
            _innerSession.Load(obj, id);
        }

        public T Get<T>(object id)
        {
            return _innerSession.Get<T>(id);
        }

        public T Get<T>(object id, LockMode lockMode)
        {
            return _innerSession.Get<T>(id, lockMode);
        }

        public object Get(Type clazz, object id)
        {
            return _innerSession.Get(clazz, id);
        }

        public object Get(Type clazz, object id, LockMode lockMode)
        {
            return _innerSession.Get(clazz, id, lockMode);
        }

        public object Get(string entityName, object id)
        {
            return _innerSession.Get(entityName, id);
        }

        [Obsolete("TODO: NHibernate: Remove in future version.")]
        public ISession GetSession(EntityMode entityMode)
        {
            return _innerSession.GetSession(entityMode);
        }

        public IFilter GetEnabledFilter(string filterName)
        {
            return _innerSession.GetEnabledFilter(filterName);
        }

        public IFilter EnableFilter(string filterName)
        {
            return _innerSession.EnableFilter(filterName);
        }

        public void DisableFilter(string filterName)
        {
            _innerSession.DisableFilter(filterName);
        }

        public T Merge<T>(T entity) where T : class
        {
            return _innerSession.Merge(entity);
        }

        public T Merge<T>(string entityName, T entity) where T : class
        {
            return _innerSession.Merge(entityName, entity);
        }

        public object Merge(object obj)
        {
            return _innerSession.Merge(obj);
        }

        public object Merge(string entityName, object obj)
        {
            return _innerSession.Merge(entityName, obj);
        }

        public void Replicate(object obj, ReplicationMode replicationMode)
        {
            _innerSession.Replicate(obj, replicationMode);
        }

        public void Replicate(string entityName, object obj, ReplicationMode replicationMode)
        {
            _innerSession.Replicate(entityName, obj, replicationMode);
        }

        public void Persist(object obj)
        {
            _innerSession.Persist(obj);
        }

        public void Persist(string entityName, object obj)
        {
            _innerSession.Persist(entityName, obj);
        }

        public object Save(object obj)
        {
            return _innerSession.Save(obj);
        }

        public void Save(object obj, object id)
        {
            _innerSession.Save(obj, id);
        }

        public object Save(string entityName, object obj)
        {
            return _innerSession.Save(entityName, obj);
        }

        public void Save(string entityName, object obj, object id)
        {
            _innerSession.Save(entityName, obj, id);
        }

        public void SaveOrUpdate(object obj)
        {
            _innerSession.SaveOrUpdate(obj);
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
            _innerSession.SaveOrUpdate(entityName, obj);
        }

        public void SaveOrUpdate(string entityName, object obj, object id)
        {
            _innerSession.SaveOrUpdate(entityName, obj, id);
        }

        public void Update(object obj)
        {
            _innerSession.Update(obj);
        }

        public void Update(object obj, object id)
        {
            _innerSession.Update(obj, id);
        }

        public void Update(string entityName, object obj)
        {
            _innerSession.Update(entityName, obj);
        }

        public void Update(string entityName, object obj, object id)
        {
            _innerSession.Update(entityName, obj, id);
        }

        public void Delete(object obj)
        {
            _innerSession.Delete(obj);
        }

        public int Delete(string query)
        {
            return _innerSession.Delete(query);
        }

        public int Delete(string query, object value, IType type)
        {
            return _innerSession.Delete(query, value, type);
        }

        public int Delete(string query, object[] values, IType[] types)
        {
            return _innerSession.Delete(query, values, types);
        }

        public void Delete(string entityName, object obj)
        {
            _innerSession.Delete(entityName, obj);
        }

        public ICollection Filter(object collection, string filter)
        {
            return _innerSession.CreateFilter(collection, filter).List();
        }

        public ICollection Filter(object collection, string filter, object value, IType type)
        {
            var q = _innerSession.CreateFilter(collection, filter);

            q.SetParameter(0, value, type);

            return q.List();
        }

        public ICollection Filter(object collection, string filter, object[] values, IType[] types)
        {
            var q = _innerSession.CreateFilter(collection, filter);

            for (var i = 0; i < values.Length; i++)
            {
                q.SetParameter(0, values[i], types[i]);
            }

            return q.List();
        }

        public IQuery CreateFilter(object collection, string queryString)
        {
            return _innerSession.CreateFilter(collection, queryString);
        }

        public IQueryable<T> Query<T>()
        {
            return _innerSession.Query<T>();
        }

        public IQueryable<T> Query<T>(string entityName)
        {
            return _innerSession.Query<T>(entityName);
        }

        public IQueryOver<T, T> QueryOver<T>() where T : class
        {
            return _innerSession.QueryOver<T>();
        }

        public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class
        {
            return _innerSession.QueryOver(alias);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
        {
            return _innerSession.QueryOver<T>(entityName);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class
        {
            return _innerSession.QueryOver(entityName, alias);
        }

        public IQuery CreateQuery(string queryString)
        {
            return _innerSession.CreateQuery(queryString);
        }

        public void CancelQuery()
        {
            _innerSession.CancelQuery();
        }

        public IQuery GetNamedQuery(string queryName)
        {
            return _innerSession.GetNamedQuery(queryName);
        }

        public ICriteria CreateCriteria<T>() where T : class
        {
            return _innerSession.CreateCriteria<T>();
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return _innerSession.CreateCriteria<T>(alias);
        }

        public ICriteria CreateCriteria(Type persistentClass)
        {
            return _innerSession.CreateCriteria(persistentClass);
        }

        public ICriteria CreateCriteria(Type persistentClass, string alias)
        {
            return _innerSession.CreateCriteria(persistentClass, alias);
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return _innerSession.CreateCriteria(entityName);
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return _innerSession.CreateCriteria(entityName, alias);
        }

        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return _innerSession.CreateSQLQuery(queryString);
        }

        public IQuery CreateSQLQuery(string sql, string returnAlias, Type returnClass)
        {
            return _innerSession.CreateSQLQuery(sql).AddEntity(returnAlias, returnClass);
        }

        public IQuery CreateSQLQuery(string sql, string[] returnAliases, Type[] returnClasses)
        {
            var query = _innerSession.CreateSQLQuery(sql);

            for (var i = 0; i < returnAliases.Length; i++)
            {
                query.AddEntity(returnAliases[i], returnClasses[i]);
            }

            return query;
        }

        [Obsolete("TODO: NHibernate: Remove in future version.")]
        public IMultiQuery CreateMultiQuery()
        {
            return _innerSession.CreateMultiQuery();
        }

        [Obsolete("TODO: NHibernate: Remove in future version.")]
        public IMultiCriteria CreateMultiCriteria()
        {
            return _innerSession.CreateMultiCriteria();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return _innerSession.FlushAsync(cancellationToken);
        }

        public Task EvictAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.EvictAsync(obj, cancellationToken);
        }

        public Task<bool> IsDirtyAsync(CancellationToken cancellationToken = default)
        {
            return _innerSession.IsDirtyAsync(cancellationToken);
        }

        public Task LockAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.LockAsync(obj, lockMode, cancellationToken);
        }

        public Task LockAsync(string entityName, object obj, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.LockAsync(entityName, obj, lockMode, cancellationToken);
        }

        public Task RefreshAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(obj, cancellationToken);
        }

        public Task RefreshAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(obj, lockMode, cancellationToken);
        }

        public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync<T>(id, cancellationToken);
        }

        public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync<T>(id, lockMode, cancellationToken);
        }

        public Task<object> LoadAsync(Type theType, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync(theType, id, cancellationToken);
        }

        public Task<object> LoadAsync(Type theType, object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync(theType, id, lockMode, cancellationToken);
        }

        public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync(entityName, id, cancellationToken);
        }

        public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync(entityName, id, lockMode, cancellationToken);
        }

        public Task LoadAsync(object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.LoadAsync(obj, id, cancellationToken);
        }

        public Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync<T>(id, cancellationToken);
        }

        public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync<T>(id, lockMode, cancellationToken);
        }

        public Task<object> GetAsync(Type clazz, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync(clazz, id, cancellationToken);
        }

        public Task<object> GetAsync(Type clazz, object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync(clazz, id, lockMode, cancellationToken);
        }

        public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync(entityName, id, cancellationToken);
        }

        public Task<string> GetEntityNameAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetEntityNameAsync(obj, cancellationToken);
        }

        public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            return _innerSession.MergeAsync(entity, cancellationToken);
        }

        public Task<T> MergeAsync<T>(string entityName, T entity, CancellationToken cancellationToken = default) where T : class
        {
            return _innerSession.MergeAsync(entityName, entity, cancellationToken);
        }

        public Task<object> MergeAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.MergeAsync(obj, cancellationToken);
        }

        public Task<object> MergeAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.MergeAsync(entityName, obj, cancellationToken);
        }

        public Task ReplicateAsync(object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.ReplicateAsync(obj, replicationMode, cancellationToken);
        }

        public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);
        }

        public Task PersistAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.PersistAsync(obj, cancellationToken);
        }

        public Task PersistAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.PersistAsync(entityName, obj, cancellationToken);
        }

        public Task SaveAsync(object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveAsync(obj, id, cancellationToken);
        }

        public Task<object> SaveAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveAsync(obj, cancellationToken);
        }

        public Task SaveAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveAsync(entityName, obj, id, cancellationToken);
        }

        public Task<object> SaveAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveAsync(entityName, obj, cancellationToken);
        }

        public Task SaveOrUpdateAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveOrUpdateAsync(obj, cancellationToken);
        }

        public Task SaveOrUpdateAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveOrUpdateAsync(entityName, obj, cancellationToken);
        }

        public Task SaveOrUpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);
        }

        public Task UpdateAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(obj, cancellationToken);
        }

        public Task UpdateAsync(object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(obj, id, cancellationToken);
        }

        public Task UpdateAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(entityName, obj, cancellationToken);
        }

        public Task UpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(entityName, obj, id, cancellationToken);
        }

        public Task DeleteAsync(object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(obj, cancellationToken);
        }

        public Task DeleteAsync(string entityName, object obj, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(entityName, obj, cancellationToken);
        }

        public Task<int> DeleteAsync(string query, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(query, cancellationToken);
        }

        public Task<int> DeleteAsync(string query, object value, IType type, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(query, value, type, cancellationToken);
        }

        public Task<int> DeleteAsync(string query, object[] values, IType[] types, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(query, values, types, cancellationToken);
        }

        public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = default)
        {
            return _innerSession.CreateFilterAsync(collection, queryString, cancellationToken);
        }

        #endregion

        /// <summary>
        /// Returns <see langword="true" /> if the supplied sessions are equal, <see langword="false" /> otherwise.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool AreEqual(ISession left, ISession right)
        {
            if (left is SessionDelegate sdLeft && right is SessionDelegate sdRight)
            {
                return ReferenceEquals(sdLeft._innerSession, sdRight._innerSession);
            }
            else
            {
                var message = $"'{nameof(AreEqual)}': left is '{left.GetType().Name}' and right is '{right.GetType().Name}'.";
                throw new NotSupportedException(message);
            }
        }
    }
}
