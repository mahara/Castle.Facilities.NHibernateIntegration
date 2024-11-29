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
    /// <summary>
    /// A contract for implementors who want to store valid session
    /// so they can be reused in a invocation chain.
    /// </summary>
    public interface ISessionStore
    {
        /// <summary>
        /// Returns <see langword="true" /> if the current activity
        /// (which is an execution activity context) has no sessions available.
        /// </summary>
        bool IsCurrentActivityEmptyFor(string? alias);

        /// <summary>
        /// Returns a previously stored session for the given alias if available;
        /// otherwise, <see langword="null" />.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        SessionDelegate? FindCompatibleSession(string? alias);

        /// <summary>
        /// Stores the specified session in the store.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="session"></param>
        void Store(string? alias, SessionDelegate session);

        /// <summary>
        /// Removes the session from the store.
        /// </summary>
        /// <param name="session"></param>
        void Remove(SessionDelegate session);

        /// <summary>
        /// Return a previously stored stateless session for the given alias if available;
        /// otherwise, <see langword="null" />.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        StatelessSessionDelegate? FindCompatibleStatelessSession(string? alias);

        /// <summary>
        /// Stores the specified stateless session in the store.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="session"></param>
        void Store(string? alias, StatelessSessionDelegate session);

        /// <summary>
        /// Removes the stateless session from the store.
        /// </summary>
        /// <param name="session"></param>
        void Remove(StatelessSessionDelegate session);
    }
}
