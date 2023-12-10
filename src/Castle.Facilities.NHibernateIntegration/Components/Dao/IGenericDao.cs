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

using NHibernate;

using Array = System.Array;

namespace Castle.Facilities.NHibernateIntegration.Components.Dao
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Contributed by Steve Degosserie &lt;steve.degosserie@vn.netika.com&gt;.
    /// </remarks>
    public interface IGenericDao
    {
        /// <summary>
        /// Returns all instances found for the specified type using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>() where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced) using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAll<T>(int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Finds an instance by an unique ID using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="id">The ID value.</param>
        /// <returns>An instance.</returns>
        T FindById<T>(object id);

        /// <summary>
        /// Creates (saves) a new instance to the database using <see cref="ISession" />.
        /// </summary>
        /// <param name="instance">The instance to be created on the database.</param>
        /// <returns>The instance.</returns>
        object Create(object instance);

        /// <summary>
        /// Saves the instance to the database using <see cref="ISession" />.
        /// If the primary key is uninitialized, it then creates the instance on the database;
        /// otherwise, it updates it.
        /// <para>
        /// If the primary key is assigned, then you must invoke <see cref="Create" />
        /// or <see cref="Update" /> instead.
        /// </para>
        /// </summary>
        /// <param name="instance">The instance to be saved.</param>
        void Save(object instance);

        /// <summary>
        /// Persists the modification on the instance state to the database using <see cref="ISession" />.
        /// </summary>
        /// <param name="instance">The instance to be updated on the database.</param>
        void Update(object instance);

        /// <summary>
        /// Deletes the instance from the database using <see cref="ISession" />.
        /// </summary>
        /// <param name="instance">The instance to be deleted from the database.</param>
        void Delete(object instance);

        /// <summary>
        /// Deletes all instances for the specified type using <see cref="ISession" />.
        /// </summary>
        /// <typeparam name="T">The type on which the instances on the database should be deleted.</typeparam>
        void DeleteAll<T>();

        /// <summary>
        /// Returns all instances found for the specified type using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>() where T : class;

        /// <summary>
        /// Returns a portion of the query results (sliced) using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="firstRow">The number of the first row to retrieve.</param>
        /// <param name="maxRows">The maximum number of results retrieved.</param>
        /// <returns>A <see cref="List{T}" /> of instances.</returns>
        List<T> FindAllStateless<T>(int firstRow, int maxRows) where T : class;

        /// <summary>
        /// Finds an instance by an unique ID using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <returns>An instance.</returns>
        T FindByIdStateless<T>(object id);

        /// <summary>
        /// Creates (saves or inserts) a new instance to the database using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="instance">The instance to be created on the database.</param>
        /// <returns>The instance.</returns>
        object CreateStateless(object instance);

        /// <summary>
        /// Persists the modification on the instance state to the database using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="instance">The instance to be updated on the database.</param>
        void UpdateStateless(object instance);

        /// <summary>
        /// Deletes the instance from the database using <see cref="IStatelessSession" />.
        /// </summary>
        /// <param name="instance">The instance to be deleted from the database.</param>
        void DeleteStateless(object instance);

        /// <summary>
        /// Deletes all instances for the specified type using <see cref="IStatelessSession" />.
        /// </summary>
        /// <typeparam name="T">The type on which the instances on the database should be deleted.</typeparam>
        void DeleteAllStateless<T>();
    }
}
