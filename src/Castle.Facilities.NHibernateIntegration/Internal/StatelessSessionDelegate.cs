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

    /// <summary>
    /// Proxies an IStatelessSession so the user cannot close a stateless session which is controlled by a transaction,
    /// or, when this is not the case, make sure to remove the session from the storage.
    /// <seealso cref="ISessionStore" />
    /// <seealso cref="ISessionManager" />
    /// </summary>
    [Serializable]
    public class StatelessSessionDelegate : MarshalByRefObject, IStatelessSession
    {
        private readonly bool _canClose;
        private readonly ISessionStore _sessionStore;
        private object _cookie;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatelessSessionDelegate" /> class.
        /// </summary>
        /// <param name="canClose">Set to <c>true</c> if can close the session.</param>
        /// <param name="innerSession">The inner session.</param>
        /// <param name="sessionStore">The session store.</param>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0618
        /// <code>
        /// #pragma warning disable 0618, 0612
        /// #pragma warning restore 0618, 0612
        /// </code>
        /// </remarks>
        public StatelessSessionDelegate(bool canClose, IStatelessSession innerSession, ISessionStore sessionStore)
        {
            InnerSession = innerSession;
            _sessionStore = sessionStore;
            _canClose = canClose;
        }

        /// <summary>
        /// Gets the inner session.
        /// </summary>
        /// <value>The inner session.</value>
        public IStatelessSession InnerSession { get; }

        /// <summary>
        /// Gets or sets the session store cookie.
        /// </summary>
        /// <value>The session store cookie.</value>
        public object SessionStoreCookie
        {
            get => _cookie;
            set => _cookie = value;
        }

        #region IDisposable Members

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
                connection = InnerSession.Connection;
                InnerSession.Close();
            }

            InnerSession.Dispose();

            _disposed = true;

            return connection;
        }

        /// <summary>
        /// Returns <see langword="true" /> if the supplied stateless sessions are equal, <see langword="false" /> otherwise.
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

            throw new NotSupportedException($"{nameof(AreEqual)}: left is {left.GetType().Name} and right is {right.GetType().Name}.");
        }

        #region IStatelessSession Members

        /// <summary>
        /// Returns the current ADO.NET connection associated with this instance.
        /// </summary>
        /// <remarks>
        /// If the session is using aggressive connection release (as in a CMT environment),
        /// it is the application's responsibility to close the connection returned by this call.
        /// Otherwise, the application should not close the connection.
        /// </remarks>
        public DbConnection Connection =>
            InnerSession.Connection;

        /// <inheritdoc />
        public bool IsOpen =>
            InnerSession.IsOpen;

        /// <inheritdoc />
        public bool IsConnected =>
            InnerSession.IsConnected;

        /// <inheritdoc />
        /// <remarks>
        /// This method is implemented explicitly, as opposed to simply calling
        /// <see cref="StatelessSessionExtensions.GetCurrentTransaction(IStatelessSession)" />,
        /// because <see cref="IStatelessSession.GetSessionImplementation()" /> can return <see langword="null" />.
        /// </remarks>
        public ITransaction Transaction =>
            InnerSession?.GetSessionImplementation()?
                         .ConnectionManager?
                         .CurrentTransaction;

        /// <inheritdoc />
        public ISessionImplementor GetSessionImplementation()
        {
            return InnerSession.GetSessionImplementation();
        }

        /// <inheritdoc />
        public void Close()
        {
            DoClose(true);
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
        public IStatelessSession SetBatchSize(int batchSize)
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
        public ICriteria CreateCriteria<T>() where T : class
        {
            return InnerSession.CreateCriteria<T>();
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria<T>(string alias) where T : class
        {
            return InnerSession.CreateCriteria<T>(alias);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(Type entityType)
        {
            return InnerSession.CreateCriteria(entityType);
        }

        /// <inheritdoc />
        public ICriteria CreateCriteria(Type entityType, string alias)
        {
            return InnerSession.CreateCriteria(entityType, alias);
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
        public object Get(string entityName, object id)
        {
            return InnerSession.Get(entityName, id);
        }

        /// <inheritdoc />
        public object Get(string entityName, object id, LockMode lockMode)
        {
            return InnerSession.Get(entityName, id, lockMode);
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
        public Task<object> GetAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.GetAsync(entityName, id, lockMode, cancellationToken);
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
        public void Refresh(string entityName, object entity)
        {
            InnerSession.Refresh(entityName, entity);
        }

        /// <inheritdoc />
        public void Refresh(string entityName, object entity, LockMode lockMode)
        {
            InnerSession.Refresh(entityName, entity, lockMode);
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
        public Task RefreshAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task RefreshAsync(string entityName, object entity, LockMode lockMode, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.RefreshAsync(entityName, entity, lockMode, cancellationToken);
        }

        /// <inheritdoc />
        public object Insert(object entity)
        {
            return InnerSession.Insert(entity);
        }

        /// <inheritdoc />
        public object Insert(string entityName, object entity)
        {
            return InnerSession.Insert(entityName, entity);
        }

        /// <inheritdoc />
        public Task<object> InsertAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.InsertAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task<object> InsertAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.InsertAsync(entityName, entity, cancellationToken);
        }

        /// <inheritdoc />
        public void Update(object entity)
        {
            InnerSession.Update(entity);
        }

        /// <inheritdoc />
        public void Update(string entityName, object entity)
        {
            InnerSession.Update(entityName, entity);
        }

        /// <inheritdoc />
        public Task UpdateAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task UpdateAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.UpdateAsync(entityName, entity, cancellationToken);
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
        public Task DeleteAsync(object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(entity, cancellationToken);
        }

        /// <inheritdoc />
        public Task DeleteAsync(string entityName, object entity, CancellationToken cancellationToken = new CancellationToken())
        {
            return InnerSession.DeleteAsync(entityName, entity, cancellationToken);
        }

        #endregion
    }
}