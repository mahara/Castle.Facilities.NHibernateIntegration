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

namespace Castle.Facilities.NHibernateIntegration.Components.Dao
{
	using NHibernate;
	using NHibernate.Collection;
	using NHibernate.Criterion;
	using NHibernate.Proxy;

	using System;

	using Util;

	/// <summary>
	/// Summary description for GenericDao.
	/// </summary>
	/// <remarks>
	/// Contributed by Steve Degosserie &lt;steve.degosserie@vn.netika.com&gt;.
	/// </remarks>
	public class NHibernateGenericDao : INHibernateGenericDao
	{
		private readonly ISessionManager _sessionManager;
		private string _sessionFactoryAlias = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="NHibernateGenericDao" /> class.
		/// </summary>
		/// <param name="sessionManager">The session manager.</param>
		public NHibernateGenericDao(ISessionManager sessionManager)
		{
			_sessionManager = sessionManager;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NHibernateGenericDao" /> class.
		/// </summary>
		/// <param name="sessionManager">The session manager.</param>
		/// <param name="sessionFactoryAlias">The session factory alias.</param>
		public NHibernateGenericDao(ISessionManager sessionManager, string sessionFactoryAlias) : this(sessionManager)
		{
			_sessionFactoryAlias = sessionFactoryAlias;
		}

		/// <summary>
		/// Gets the session manager.
		/// </summary>
		/// <value>The session manager.</value>
		protected ISessionManager SessionManager
		{
			get { return _sessionManager; }
		}

		/// <summary>
		/// Gets or sets the session factory alias.
		/// </summary>
		/// <value>The session factory alias.</value>
		public string SessionFactoryAlias
		{
			get { return _sessionFactoryAlias; }
			set { _sessionFactoryAlias = value; }
		}

		#region IGenericDAO Members

		/// <summary>
		/// Returns all instances found for the specified type.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <returns>The <see cref="Array" /> of results</returns>
		public virtual Array FindAll(Type type)
		{
			return FindAll(type, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced).
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAll(Type type, int firstRow, int maxRows)
		{
			using (var session = GetSession())
			{
				try
				{
					var criteria = session.CreateCriteria(type);

					if (firstRow != int.MinValue) criteria.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) criteria.SetMaxResults(maxRows);
					var result = criteria.List();

					var array = Array.CreateInstance(type, result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAll for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Finds an object instance by an unique ID.
		/// </summary>
		/// <param name="type">The AR subclass type.</param>
		/// <param name="id">ID value.</param>
		/// <returns>The object instance.</returns>
		public virtual object FindById(Type type, object id)
		{
			using (var session = GetSession())
			{
				try
				{
					return session.Load(type, id);
				}
				catch (ObjectNotFoundException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindById for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Creates (Saves) a new instance to the database.
		/// </summary>
		/// <param name="instance">The instance to be created on the database.</param>
		/// <returns>The instance</returns>
		public virtual object Create(object instance)
		{
			using (var session = GetSession())
			{
				try
				{
					return session.Save(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Create for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Persists the modification on the instance state to the database.
		/// </summary>
		/// <param name="instance">The instance to be updated on the database.</param>
		public virtual void Update(object instance)
		{
			using (var session = GetSession())
			{
				try
				{
					session.Update(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Update for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Deletes the instance from the database.
		/// </summary>
		/// <param name="instance">The instance to be deleted from the database.</param>
		public virtual void Delete(object instance)
		{
			using (var session = GetSession())
			{
				try
				{
					session.Delete(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Delete for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Deletes all rows for the specified type.
		/// </summary>
		/// <param name="type">type on which the rows on the database should be deleted.</param>
		public virtual void DeleteAll(Type type)
		{
			using (var session = GetSession())
			{
				try
				{
					session.Delete(string.Format("from {0}", type.Name));
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform DeleteAll for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Saves the instance to the database.
		/// If the primary key is uninitialized, it creates the instance on the database.
		/// Otherwise, it updates it.
		/// <para>
		/// If the primary key is assigned, then you must invoke <see cref="Create" />
		/// or <see cref="Update" /> instead.
		/// </para>
		/// </summary>
		/// <param name="instance">The instance to be saved.</param>
		public virtual void Save(object instance)
		{
			using (var session = GetSession())
			{
				try
				{
					session.SaveOrUpdate(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Save for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Returns all instances found for the specified type using IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type)
		{
			return FindAllStateless(type, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns a portion of the query results (sliced) using IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type, int firstRow, int maxRows)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					var criteria = session.CreateCriteria(type);

					if (firstRow != int.MinValue) criteria.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) criteria.SetMaxResults(maxRows);
					var result = criteria.List();

					var array = Array.CreateInstance(type, result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAllStateless for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Finds an object instance by an unique ID using IStatelessSession.
		/// </summary>
		/// <param name="type">The AR subclass type.</param>
		/// <param name="id">ID value.</param>
		/// <returns>The object instance.</returns>
		public object FindByIdStateless(Type type, object id)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					return session.Get(type.FullName, id);
				}
				catch (ObjectNotFoundException)
				{
					throw;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindByIdStateless for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Creates (saves or inserts) a new instance to the database using IStatelessSession.
		/// </summary>
		/// <param name="instance">The instance to be created on the database.</param>
		/// <returns>The instance.</returns>
		public object CreateStateless(object instance)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					return session.Insert(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform CreateStateless for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Persists the modification on the instance state to the database using IStatelessSession.
		/// </summary>
		/// <param name="instance">The instance to be updated on the database.</param>
		public void UpdateStateless(object instance)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					session.Update(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform UpdateStateless for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Deletes the instance from the database using IStatelessSession.
		/// </summary>
		/// <param name="instance">The instance to be deleted from the database.</param>
		public void DeleteStateless(object instance)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					session.Delete(instance);
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform DeleteStateless for " + instance.GetType().Name, ex);
				}
			}
		}

		/// <summary>
		/// Deletes all rows for the specified type using IStatelessSession.
		/// </summary>
		/// <param name="type">type on which the rows on the database should be deleted.</param>
		public void DeleteAllStateless(Type type)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					session.Delete(string.Format("from {0}", type.Name));
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform DeleteAllStateless for " + type.Name, ex);
				}
			}
		}

		#endregion

		#region INHibernateGenericDAO Members

		/// <summary>
		/// Returns all instances found for the specified type using criteria.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAll(Type type, ICriterion[] criterias)
		{
			return FindAll(type, criterias, null, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns all instances found for the specified type using criteria.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAll(Type type, ICriterion[] criterias, int firstRow, int maxRows)
		{
			return FindAll(type, criterias, null, firstRow, maxRows);
		}

		/// <summary>
		/// Returns all instances found for the specified type using criteria.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAll(Type type, ICriterion[] criterias, Order[] sortItems)
		{
			return FindAll(type, criterias, sortItems, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns all instances found for the specified type using criteria.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAll(Type type, ICriterion[] criterias, Order[] sortItems, int firstRow, int maxRows)
		{
			using (var session = GetSession())
			{
				try
				{
					var criteria = session.CreateCriteria(type);

					if (criterias != null)
					{
						foreach (var cond in criterias)
						{
							criteria.Add(cond);
						}
					}

					if (sortItems != null)
					{
						foreach (var order in sortItems)
						{
							criteria.AddOrder(order);
						}
					}

					if (firstRow != int.MinValue) criteria.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) criteria.SetMaxResults(maxRows);
					var result = criteria.List();

					var array = Array.CreateInstance(type, result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAll for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Finds all with custom query.
		/// </summary>
		/// <param name="queryString">The query string.</param>
		/// <returns></returns>
		public virtual Array FindAllWithCustomQuery(string queryString)
		{
			return FindAllWithCustomQuery(queryString, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Finds all with custom HQL query.
		/// </summary>
		/// <param name="queryString">The query string.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns></returns>
		public virtual Array FindAllWithCustomQuery(string queryString, int firstRow, int maxRows)
		{
			if (string.IsNullOrEmpty(queryString)) throw new ArgumentNullException("queryString");

			using (var session = GetSession())
			{
				try
				{
					var query = session.CreateQuery(queryString);

					if (firstRow != int.MinValue) query.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) query.SetMaxResults(maxRows);
					var result = query.List();
					if (result == null || result.Count == 0) return null;

					var array = Array.CreateInstance(result[0].GetType(), result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Find for custom query : " + queryString, ex);
				}
			}
		}

		/// <summary>
		/// Finds all with named HQL query.
		/// </summary>
		/// <param name="namedQuery">The named query.</param>
		/// <returns></returns>
		public virtual Array FindAllWithNamedQuery(string namedQuery)
		{
			return FindAllWithNamedQuery(namedQuery, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Finds all with named HQL query.
		/// </summary>
		/// <param name="namedQuery">The named query.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns></returns>
		public virtual Array FindAllWithNamedQuery(string namedQuery, int firstRow, int maxRows)
		{
			if (string.IsNullOrEmpty(namedQuery)) throw new ArgumentNullException("namedQuery");

			using (var session = GetSession())
			{
				try
				{
					var query = session.GetNamedQuery(namedQuery);
					if (query == null) throw new ArgumentException("Cannot find named query", "namedQuery");

					if (firstRow != int.MinValue) query.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) query.SetMaxResults(maxRows);
					var result = query.List();
					if (result == null || result.Count == 0) return null;

					var array = Array.CreateInstance(result[0].GetType(), result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform Find for named query : " + namedQuery, ex);
				}
			}
		}

		/// <summary>
		/// Initializes the lazy properties.
		/// </summary>
		/// <param name="instance">The instance.</param>
		public void InitializeLazyProperties(object instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			using (var session = GetSession())
			{
				foreach (var val in ReflectionUtility.GetPropertiesDictionary(instance).Values)
				{
					if (val is INHibernateProxy || val is IPersistentCollection)
					{
						if (!NHibernateUtil.IsInitialized(val))
						{
							session.Lock(instance, LockMode.None);
							NHibernateUtil.Initialize(val);
						}
					}
				}
			}
		}

		/// <summary>
		/// Initializes the lazy property.
		/// </summary>
		/// <param name="instance">The instance.</param>
		/// <param name="propertyName">The name of the property.</param>
		public void InitializeLazyProperty(object instance, string propertyName)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

			var properties = ReflectionUtility.GetPropertiesDictionary(instance);
			if (!properties.ContainsKey(propertyName))
				throw new ArgumentOutOfRangeException(nameof(propertyName),
													  "Property " + propertyName +
													  " doest not exist for type " + instance.GetType() + ".");

			using (var session = GetSession())
			{
				var value = properties[propertyName];

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

		/// <summary>
		/// Returns all instances found for the specified type
		/// using criteria and IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type, ICriterion[] criterias)
		{
			return FindAllStateless(type, criterias, null, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns all instances found for the specified type
		/// using criteria and IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type, ICriterion[] criterias, int firstRow, int maxRows)
		{
			return FindAllStateless(type, criterias, null, firstRow, maxRows);
		}

		/// <summary>
		/// Returns all instances found for the specified type
		/// using criteria and IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type, ICriterion[] criterias, Order[] sortItems)
		{
			return FindAllStateless(type, criterias, sortItems, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Returns all instances found for the specified type
		/// using criteria and IStatelessSession.
		/// </summary>
		/// <param name="type">The target type.</param>
		/// <param name="criterias">The criteria expression.</param>
		/// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns>The <see cref="Array" /> of results.</returns>
		public virtual Array FindAllStateless(Type type, ICriterion[] criterias, Order[] sortItems, int firstRow, int maxRows)
		{
			using (var session = GetStatelessSession())
			{
				try
				{
					var criteria = session.CreateCriteria(type);

					if (criterias != null)
					{
						foreach (var cond in criterias)
						{
							criteria.Add(cond);
						}
					}

					if (sortItems != null)
					{
						foreach (var order in sortItems)
						{
							criteria.AddOrder(order);
						}
					}

					if (firstRow != int.MinValue) criteria.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) criteria.SetMaxResults(maxRows);
					var result = criteria.List();

					var array = Array.CreateInstance(type, result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAllStateless for " + type.Name, ex);
				}
			}
		}

		/// <summary>
		/// Finds all with custom query using IStatelessSession.
		/// </summary>
		/// <param name="queryString">The query string.</param>
		/// <returns></returns>
		public virtual Array FindAllWithCustomQueryStateless(string queryString)
		{
			return FindAllWithCustomQueryStateless(queryString, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Finds all with custom HQL query using IStatelessSession.
		/// </summary>
		/// <param name="queryString">The query string.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns></returns>
		public virtual Array FindAllWithCustomQueryStateless(string queryString, int firstRow, int maxRows)
		{
			if (string.IsNullOrEmpty(queryString)) throw new ArgumentNullException("queryString");

			using (var session = GetStatelessSession())
			{
				try
				{
					var query = session.CreateQuery(queryString);

					if (firstRow != int.MinValue) query.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) query.SetMaxResults(maxRows);
					var result = query.List();
					if (result == null || result.Count == 0) return null;

					var array = Array.CreateInstance(result[0].GetType(), result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAllWithCustomQueryStateless: " + queryString, ex);
				}
			}
		}

		/// <summary>
		/// Finds all with named HQL query using IStatelessSession.
		/// </summary>
		/// <param name="namedQuery">The named query.</param>
		/// <returns></returns>
		public virtual Array FindAllWithNamedQueryStateless(string namedQuery)
		{
			return FindAllWithNamedQueryStateless(namedQuery, int.MinValue, int.MinValue);
		}

		/// <summary>
		/// Finds all with named HQL query using IStatelessSession.
		/// </summary>
		/// <param name="namedQuery">The named query.</param>
		/// <param name="firstRow">The number of the first row to retrieve.</param>
		/// <param name="maxRows">The maximum number of results retrieved.</param>
		/// <returns></returns>
		public virtual Array FindAllWithNamedQueryStateless(string namedQuery, int firstRow, int maxRows)
		{
			if (string.IsNullOrEmpty(namedQuery)) throw new ArgumentNullException("namedQuery");

			using (var session = GetStatelessSession())
			{
				try
				{
					var query = session.GetNamedQuery(namedQuery);
					if (query == null) throw new ArgumentException("Cannot find named query", "namedQuery");

					if (firstRow != int.MinValue) query.SetFirstResult(firstRow);
					if (maxRows != int.MinValue) query.SetMaxResults(maxRows);
					var result = query.List();
					if (result == null || result.Count == 0) return null;

					var array = Array.CreateInstance(result[0].GetType(), result.Count);
					result.CopyTo(array, 0);

					return array;
				}
				catch (Exception ex)
				{
					throw new DataException("Could not perform FindAllWithNamedQueryStateless: " + namedQuery, ex);
				}
			}
		}

		#endregion

		private ISession GetSession()
		{
			if (string.IsNullOrEmpty(_sessionFactoryAlias))
			{
				return _sessionManager.OpenSession();
			}
			else
			{
				return _sessionManager.OpenSession(_sessionFactoryAlias);
			}
		}

		private IStatelessSession GetStatelessSession()
		{
			if (string.IsNullOrEmpty(_sessionFactoryAlias))
			{
				return _sessionManager.OpenStatelessSession();
			}
			else
			{
				return _sessionManager.OpenStatelessSession(_sessionFactoryAlias);
			}
		}
	}
}