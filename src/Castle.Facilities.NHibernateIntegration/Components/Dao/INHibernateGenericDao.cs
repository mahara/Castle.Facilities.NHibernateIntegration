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

using NHibernate;
using NHibernate.Criterion;

namespace Castle.Facilities.NHibernateIntegration.Components.Dao
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Contributed by Steve Degosserie &lt;steve.degosserie@vn.netika.com&gt;.
    /// </remarks>
    public interface INHibernateGenericDao : IGenericDao
    {
        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>(ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>(ICriterion[] criteria, int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>(ICriterion[] criteria, Order[] sortItems) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>(ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Finds all with custom query using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="queryString">The query string.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithCustomQuery<T>(string queryString);

        /// <summary>
        /// Finds all with custom HQL query using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="queryString">The query string.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithCustomQuery<T>(string queryString, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with named HQL query using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="namedQuery">The named query.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithNamedQuery<T>(string namedQuery);

        /// <summary>
        /// Finds all with named HQL query using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="namedQuery">The named query.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithNamedQuery<T>(string namedQuery, int firstRow, int maxRows);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>(ICriterion[] criteria) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>(ICriterion[] criteria, int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>(ICriterion[] criteria, Order[] sortItems) where T : class;

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>(ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Finds all with custom query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="queryString">The query string.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithCustomQueryStateless<T>(string queryString);

        /// <summary>
        /// Finds all with custom HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="queryString">The query string.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithCustomQueryStateless<T>(string queryString, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with named HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="namedQuery">The named query.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithNamedQueryStateless<T>(string namedQuery);

        /// <summary>
        /// Finds all with named HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="namedQuery">The named query.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllWithNamedQueryStateless<T>(string namedQuery, int firstRow, int maxRows);

        /// <summary>
        /// Initializes the lazy properties of the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        void InitializeLazyProperties(object instance);

        /// <summary>
        /// Initializes the lazy property of the instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyName">The name of the property.</param>
        void InitializeLazyProperty(object instance, string propertyName);



        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAll(Type type, ICriterion[] criteria);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAll(Type type, ICriterion[] criteria, int firstRow, int maxRows);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAll(Type type, ICriterion[] criteria, Order[] sortItems);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="ISession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAll(Type type, ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with custom query using <see cref="ISession" />.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithCustomQuery(string queryString);

        /// <summary>
        /// Finds all with custom HQL query using <see cref="ISession" />.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithCustomQuery(string queryString, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with named HQL query using <see cref="ISession" />.
        /// </summary>
        /// <param name="namedQuery">The named query.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithNamedQuery(string namedQuery);

        /// <summary>
        /// Finds all with named HQL query using <see cref="ISession" />.
        /// </summary>
        /// <param name="namedQuery">The named query.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithNamedQuery(string namedQuery, int firstRow, int maxRows);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllStateless(Type type, ICriterion[] criteria);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllStateless(Type type, ICriterion[] criteria, int firstRow, int maxRows);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllStateless(Type type, ICriterion[] criteria, Order[] sortItems);

        /// <summary>
        /// Returns all instances found for the specified type using criteria using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="type">The instance type.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="sortItems">An <see cref="Array" /> of <see cref="Order" /> objects.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllStateless(Type type, ICriterion[] criteria, Order[] sortItems, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with custom query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithCustomQueryStateless(string queryString);

        /// <summary>
        /// Finds all with custom HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithCustomQueryStateless(string queryString, int firstRow, int maxRows);

        /// <summary>
        /// Finds all with named HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="namedQuery">The named query.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithNamedQueryStateless(string namedQuery);

        /// <summary>
        /// Finds all with named HQL query using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="namedQuery">The named query.</param>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>An <see cref="Array" /> of instances.</returns>
        [Obsolete("Use generic method overloads instead.")]
        Array FindAllWithNamedQueryStateless(string namedQuery, int firstRow, int maxRows);
    }
}
