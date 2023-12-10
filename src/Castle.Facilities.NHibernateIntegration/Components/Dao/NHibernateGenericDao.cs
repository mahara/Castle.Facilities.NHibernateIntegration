#region License
// Copyright 2004-2023 Castle Project - https://www.castleproject.org/
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

using Castle.Facilities.NHibernateIntegration.Utilities;

using NHibernate;
using NHibernate.Collection;
using NHibernate.Criterion;
using NHibernate.Proxy;

namespace Castle.Facilities.NHibernateIntegration.Components.Dao
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Contributed by Steve Degosserie &lt;steve.degosserie@vn.netika.com&gt;.
    /// </remarks>
    public class NHibernateGenericDao : INHibernateGenericDao
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateGenericDao" /> class.
        /// </summary>
        /// <param name="sessionManager">The <see cref="ISessionManager" />.</param>
        public NHibernateGenericDao(ISessionManager sessionManager)
        {
            SessionManager = sessionManager;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateGenericDao" /> class.
        /// </summary>
        /// <param name="sessionManager">The <see cref="ISessionManager" />.</param>
        /// <param name="sessionFactoryAlias">The <see cref="ISessionFactory" /> alias.</param>
        public NHibernateGenericDao(ISessionManager sessionManager, string sessionFactoryAlias) : this(sessionManager)
        {
            SessionFactoryAlias = sessionFactoryAlias;
        }

        /// <summary>
        /// Gets the <see cref="ISessionManager" />.
        /// </summary>
        /// <value>The <see cref="ISessionManager" />.</value>
        protected ISessionManager SessionManager { get; }

        /// <summary>
        /// Gets or sets the <see cref="ISessionFactory" /> alias.
        /// </summary>
        /// <value>The <see cref="ISessionFactory" /> alias.</value>
        public string SessionFactoryAlias { get; set; } = null;

        #region IGenericDao Members

        public List<T> FindAll<T>() where T : class
        {
            return FindAll<T>(int.MinValue, int.MinValue);
        }

        public List<T> FindAll<T>(int firstRow, int maxRows) where T : class
        {
            var type = typeof(T);

            using var session = GetSession();

            try
            {
                var sessionCriteria = session.CreateCriteria<T>();

                if (firstRow != int.MinValue)
                {
                    sessionCriteria.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    sessionCriteria.SetMaxResults(maxRows);
                }

                return (List<T>) sessionCriteria.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAll)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public T FindById<T>(object id)
        {
            var type = typeof(T);

            using var session = GetSession();

            try
            {
                return session.Load<T>(id);
            }
            catch (ObjectNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindById)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public virtual object Create(object instance)
        {
            using var session = GetSession();

            try
            {
                return session.Save(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(Create)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public virtual void Save(object instance)
        {
            using var session = GetSession();

            try
            {
                session.SaveOrUpdate(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(Save)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public virtual void Update(object instance)
        {
            using var session = GetSession();

            try
            {
                session.Update(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(Update)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public virtual void Delete(object instance)
        {
            using var session = GetSession();

            try
            {
                session.Delete(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(Delete)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public void DeleteAll<T>()
        {
            var type = typeof(T);

            using var session = GetSession();

            try
            {
                session.Delete($"from {type.Name}");
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(DeleteAll)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllStateless<T>() where T : class
        {
            return FindAllStateless<T>(int.MinValue, int.MinValue);
        }

        public List<T> FindAllStateless<T>(int firstRow, int maxRows) where T : class
        {
            var type = typeof(T);

            using var session = GetSession();

            try
            {
                var criteria = session.CreateCriteria<T>();

                if (firstRow != int.MinValue)
                {
                    criteria.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    criteria.SetMaxResults(maxRows);
                }

                return (List<T>) criteria.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllStateless)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public T FindByIdStateless<T>(object id)
        {
            var type = typeof(T);

            using var session = GetStatelessSession();

            try
            {
                return session.Get<T>(id);
            }
            catch (ObjectNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindByIdStateless)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public object CreateStateless(object instance)
        {
            using var session = GetStatelessSession();

            try
            {
                return session.Insert(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(CreateStateless)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public void UpdateStateless(object instance)
        {
            using var session = GetStatelessSession();

            try
            {
                session.Update(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(UpdateStateless)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public void DeleteStateless(object instance)
        {
            using var session = GetStatelessSession();

            try
            {
                session.Delete(instance);
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(DeleteStateless)}' for '{instance.GetType().Name}'.";
                throw new DataException(message, ex);
            }
        }

        public void DeleteAllStateless<T>()
        {
            var type = typeof(T);

            using var session = GetStatelessSession();

            try
            {
                session.Delete($"from {type.Name}");
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(DeleteAllStateless)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        #endregion

        #region INHibernateGenericDao Members

        public List<T> FindAll<T>(ICriterion[] criteria) where T : class
        {
            return FindAll<T>(criteria, null, int.MinValue, int.MinValue);
        }

        public List<T> FindAll<T>(ICriterion[] criteria, int firstRow, int maxRows) where T : class
        {
            return FindAll<T>(criteria, null, firstRow, maxRows);
        }

        public List<T> FindAll<T>(ICriterion[] criteria, Order[] sortItems) where T : class
        {
            return FindAll<T>(criteria, sortItems, int.MinValue, int.MinValue);
        }

        public List<T> FindAll<T>(ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows) where T : class
        {
            var type = typeof(T);

            using var session = GetSession();

            try
            {
                var sessionCriteria = session.CreateCriteria<T>();

                if (criteria is not null)
                {
                    foreach (var criterion in criteria)
                    {
                        sessionCriteria.Add(criterion);
                    }
                }

                if (sortItems is not null)
                {
                    foreach (var order in sortItems)
                    {
                        sessionCriteria.AddOrder(order);
                    }
                }

                if (firstRow != int.MinValue)
                {
                    sessionCriteria.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    sessionCriteria.SetMaxResults(maxRows);
                }

                return (List<T>) sessionCriteria.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAll)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllWithCustomQuery<T>(string queryString)
        {
            return FindAllWithCustomQuery<T>(queryString, int.MinValue, int.MinValue);
        }

        public List<T> FindAllWithCustomQuery<T>(string queryString, int firstRow, int maxRows)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                throw new ArgumentException($"'{nameof(queryString)}' cannot be null or empty.", nameof(queryString));
            }

            using var session = GetSession();

            try
            {
                var query = session.CreateQuery(queryString);

                if (firstRow != int.MinValue)
                {
                    query.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    query.SetMaxResults(maxRows);
                }

                return (List<T>) query.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllWithCustomQuery)}' for custom query: '{queryString}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllWithNamedQuery<T>(string namedQuery)
        {
            return FindAllWithNamedQuery<T>(namedQuery, int.MinValue, int.MinValue);
        }

        public List<T> FindAllWithNamedQuery<T>(string namedQuery, int firstRow, int maxRows)
        {
            if (string.IsNullOrEmpty(namedQuery))
            {
                throw new ArgumentException($"'{nameof(namedQuery)}' cannot be null or empty.", nameof(namedQuery));
            }

            using var session = GetSession();

            try
            {
                var query = session.GetNamedQuery(namedQuery) ??
                            throw new ArgumentException("Cannot find named query.", nameof(namedQuery));

                if (firstRow != int.MinValue)
                {
                    query.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    query.SetMaxResults(maxRows);
                }

                return (List<T>) query.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllWithNamedQuery)}' for named query: '{namedQuery}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllStateless<T>(ICriterion[] criteria) where T : class
        {
            return FindAllStateless<T>(criteria, null, int.MinValue, int.MinValue);
        }

        public List<T> FindAllStateless<T>(ICriterion[] criteria, int firstRow, int maxRows) where T : class
        {
            return FindAllStateless<T>(criteria, null, firstRow, maxRows);
        }

        public List<T> FindAllStateless<T>(ICriterion[] criteria, Order[] sortItems) where T : class
        {
            return FindAllStateless<T>(criteria, sortItems, int.MinValue, int.MinValue);
        }

        public List<T> FindAllStateless<T>(ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows) where T : class
        {
            var type = typeof(T);

            using var session = GetStatelessSession();

            try
            {
                var sessionCriteria = session.CreateCriteria<T>();

                if (criteria is not null)
                {
                    foreach (var criterion in criteria)
                    {
                        sessionCriteria.Add(criterion);
                    }
                }

                if (sortItems is not null)
                {
                    foreach (var order in sortItems)
                    {
                        sessionCriteria.AddOrder(order);
                    }
                }

                if (firstRow != int.MinValue)
                {
                    sessionCriteria.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    sessionCriteria.SetMaxResults(maxRows);
                }

                return (List<T>) sessionCriteria.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllStateless)}' for '{type.Name}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllWithCustomQueryStateless<T>(string queryString)
        {
            return FindAllWithCustomQueryStateless<T>(queryString, int.MinValue, int.MinValue);
        }

        public List<T> FindAllWithCustomQueryStateless<T>(string queryString, int firstRow, int maxRows)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                throw new ArgumentException($"'{nameof(queryString)}' cannot be null or empty.", nameof(queryString));
            }

            using var session = GetStatelessSession();

            try
            {
                var query = session.CreateQuery(queryString);

                if (firstRow != int.MinValue)
                {
                    query.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    query.SetMaxResults(maxRows);
                }

                return (List<T>) query.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllWithCustomQueryStateless)}': '{queryString}'.";
                throw new DataException(message, ex);
            }
        }

        public List<T> FindAllWithNamedQueryStateless<T>(string namedQuery)
        {
            return FindAllWithNamedQueryStateless<T>(namedQuery, int.MinValue, int.MinValue);
        }

        public List<T> FindAllWithNamedQueryStateless<T>(string namedQuery, int firstRow, int maxRows)
        {
            if (string.IsNullOrEmpty(namedQuery))
            {
                throw new ArgumentException($"'{nameof(namedQuery)}' cannot be null or empty.", nameof(namedQuery));
            }

            using var session = GetStatelessSession();

            try
            {
                var query = session.GetNamedQuery(namedQuery) ??
                            throw new ArgumentException("Cannot find named query.", nameof(namedQuery));

                if (firstRow != int.MinValue)
                {
                    query.SetFirstResult(firstRow);
                }

                if (maxRows != int.MinValue)
                {
                    query.SetMaxResults(maxRows);
                }

                return (List<T>) query.List<T>();
            }
            catch (Exception ex)
            {
                var message = $"Could not perform '{nameof(FindAllWithNamedQueryStateless)}': '{namedQuery}'.";
                throw new DataException(message, ex);
            }
        }

        public void InitializeLazyProperties(object instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            using var session = GetSession();

            foreach (var value in ReflectionUtility.GetPropertiesDictionary(instance).Values)
            {
                if (value is INHibernateProxy || value is IPersistentCollection)
                {
                    if (!NHibernateUtil.IsInitialized(value))
                    {
                        session.Lock(instance, LockMode.None);
                        NHibernateUtil.Initialize(value);
                    }
                }
            }
        }

        public void InitializeLazyProperty(object instance, string propertyName)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException($"'{nameof(propertyName)}' cannot be null or empty.", nameof(propertyName));
            }

            var properties = ReflectionUtility.GetPropertiesDictionary(instance);
            if (!properties.TryGetValue(propertyName, out var propertyValue))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(propertyName),
                    $"Property '{propertyName}' doest not exist for type '{instance.GetType()}'.");
            }

            using var session = GetSession();

            if (propertyValue is INHibernateProxy || propertyValue is IPersistentCollection)
            {
                if (!NHibernateUtil.IsInitialized(propertyValue))
                {
                    session.Lock(instance, LockMode.None);
                    NHibernateUtil.Initialize(propertyValue);
                }
            }
        }

        #endregion

        #region Helper Methods

        private ISession GetSession()
        {
            return string.IsNullOrEmpty(SessionFactoryAlias) ?
                   SessionManager.OpenSession() :
                   SessionManager.OpenSession(SessionFactoryAlias);
        }

        private IStatelessSession GetStatelessSession()
        {
            return string.IsNullOrEmpty(SessionFactoryAlias) ?
                   SessionManager.OpenStatelessSession() :
                   SessionManager.OpenStatelessSession(SessionFactoryAlias);
        }

        #endregion
    }
}
