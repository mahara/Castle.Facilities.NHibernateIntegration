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
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using NHibernate;
using NHibernate.Engine;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Proxies an <see cref="IStatelessSession" /> so the user cannot close a stateless session which is controlled by a transaction,
    /// or, when this is not the case, make sure to remove the session from the storage.
    /// </summary>
    /// <remarks>
    /// <seealso cref="ISessionStore" />
    /// <seealso cref="ISessionManager" />
    /// </remarks>
    [Serializable]
    public class StatelessSessionDelegate : MarshalByRefObject, IStatelessSession
    {
        private readonly IStatelessSession _innerSession;
        private readonly ISessionStore _sessionStore;
        private readonly bool _canClose;

        private bool _isDisposed;

        public StatelessSessionDelegate(IStatelessSession innerSession, ISessionStore sessionStore, bool canClose)
        {
            _innerSession = innerSession;
            _sessionStore = sessionStore;
            _canClose = canClose;
        }

        #region IDisposable Members

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
                connection = _innerSession.Connection;
                _innerSession.Close();
            }

            _innerSession.Dispose();

            _isDisposed = true;

            return connection;
        }

        #endregion

        /// <summary>
        /// Gets the inner session.
        /// </summary>
        /// <value>The inner session.</value>
        public IStatelessSession InnerSession => _innerSession;

        /// <summary>
        /// Gets or sets the session store cookie.
        /// </summary>
        /// <value>The session store cookie.</value>
        public object SessionStoreCookie { get; set; }

        #region IStatelessSession Members

        public ISessionImplementor GetSessionImplementation()
        {
            return _innerSession.GetSessionImplementation();
        }

        //
        // TODO:    Update implementation in future version (5.3.x),
        //          following NHibernate 5.3.x.
        //
        // NOTE:    StatelessSessionDelegate.Transaction, with slightly-modified implementation of IStatelessSessionDelegate.GetCurrentTransaction(),
        //          is used here to workaround a mocking issue (in Facilities103 issue) of IStatelessSessionDelegate.GetSessionImplementation().
        //
        ///// <summary>
        ///// Gets the current Unit of Work and returns the associated <see cref="ITransaction" /> object.
        ///// </summary>
        ///// <remarks>
        ///// This property getter is implemented explicitly in <see cref="SessionExtensions.GetCurrentTransaction(IStatelessSession)" />.
        ///// </remarks>
        //public ITransaction Transaction =>
        //    _innerSession.GetSessionImplementation()?
        //                 .ConnectionManager?
        //                 .CurrentTransaction;
        public ITransaction Transaction => _innerSession.Transaction;

        public DbConnection Connection => _innerSession.Connection;

        public bool IsConnected => _innerSession.IsConnected;

        public bool IsOpen => _innerSession.IsOpen;

        public void Close()
        {
            _innerSession.Close();
        }

        public IStatelessSession SetBatchSize(int batchSize)
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

        public void Refresh(object entity)
        {
            _innerSession.Refresh(entity);
        }

        public void Refresh(string entityName, object entity)
        {
            _innerSession.Refresh(entityName, entity);
        }

        public void Refresh(object entity, LockMode lockMode)
        {
            _innerSession.Refresh(entity, lockMode);
        }

        public void Refresh(string entityName, object entity, LockMode lockMode)
        {
            _innerSession.Refresh(entityName, entity, lockMode);
        }

        public T Get<T>(object id)
        {
            return _innerSession.Get<T>(id);
        }

        public T Get<T>(object id, LockMode lockMode)
        {
            return _innerSession.Get<T>(id, lockMode);
        }

        public object Get(string entityName, object id)
        {
            return _innerSession.Get(entityName, id);
        }

        public object Get(string entityName, object id, LockMode lockMode)
        {
            return _innerSession.Get(entityName, id, lockMode);
        }

        public object Insert(object entity)
        {
            return _innerSession.Insert(entity);
        }

        public object Insert(string entityName, object entity)
        {
            return _innerSession.Insert(entityName, entity);
        }

        public void Update(object entity)
        {
            _innerSession.Update(entity);
        }

        public void Update(string entityName, object entity)
        {
            _innerSession.Update(entityName, entity);
        }

        public void Delete(object entity)
        {
            _innerSession.Delete(entity);
        }

        public void Delete(string entityName, object entity)
        {
            _innerSession.Delete(entityName, entity);
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

        public ICriteria CreateCriteria(Type entityType)
        {
            return _innerSession.CreateCriteria(entityType);
        }

        public ICriteria CreateCriteria(Type entityType, string alias)
        {
            return _innerSession.CreateCriteria(entityType, alias);
        }

        public ICriteria CreateCriteria(string entityName)
        {
            return _innerSession.CreateCriteria(entityName);
        }

        public ICriteria CreateCriteria(string entityName, string alias)
        {
            return _innerSession.CreateCriteria(entityName, alias);
        }

        public IQuery CreateQuery(string queryString)
        {
            return _innerSession.CreateQuery(queryString);
        }

        public ISQLQuery CreateSQLQuery(string queryString)
        {
            return _innerSession.CreateSQLQuery(queryString);
        }

        public Task RefreshAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(entity, cancellationToken);
        }

        public Task RefreshAsync(object entity, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(entity, lockMode, cancellationToken);
        }

        public Task RefreshAsync(string entityName, object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(entityName, entity, cancellationToken);
        }

        public Task RefreshAsync(string entityName, object entity, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.RefreshAsync(entityName, entity, lockMode, cancellationToken);
        }

        public Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync<T>(id, cancellationToken);
        }

        public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync<T>(id, lockMode, cancellationToken);
        }

        public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync(entityName, id, cancellationToken);
        }

        public Task<object> GetAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = default)
        {
            return _innerSession.GetAsync(entityName, id, lockMode, cancellationToken);
        }

        public Task<object> InsertAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.InsertAsync(entity, cancellationToken);
        }

        public Task<object> InsertAsync(string entityName, object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.InsertAsync(entityName, entity, cancellationToken);
        }

        public Task UpdateAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(entity, cancellationToken);
        }

        public Task UpdateAsync(string entityName, object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.UpdateAsync(entityName, entity, cancellationToken);
        }

        public Task DeleteAsync(object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(entity, cancellationToken);
        }

        public Task DeleteAsync(string entityName, object entity, CancellationToken cancellationToken = default)
        {
            return _innerSession.DeleteAsync(entityName, entity, cancellationToken);
        }

        #endregion

        /// <summary>
        /// Returns <see langword="true" /> if the supplied stateless sessions are equal, <see langword="false" /> otherwise.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns></returns>
        public static bool AreEqual(IStatelessSession left, IStatelessSession right)
        {
            if (left is StatelessSessionDelegate ssdLeft && right is StatelessSessionDelegate ssdRight)
            {
                return ReferenceEquals(ssdLeft._innerSession, ssdRight._innerSession);
            }
            else
            {
                throw new NotSupportedException(
                    $"'{nameof(AreEqual)}': left is '{left.GetType().Name}' and right is '{right.GetType().Name}'.");
            }
        }
    }
}
