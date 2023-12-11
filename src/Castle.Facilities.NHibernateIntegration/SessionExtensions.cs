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

namespace Castle.Facilities.NHibernateIntegration
{
    public static class SessionExtensions
    {
        /// <summary>
        /// Get current transaction if any is ongoing; else, <see langword="null" />.
        /// </summary>
        /// <param name="session">The <see cref="ISession" />.</param>
        /// <returns>Current transaction or <see langword="null" />.</returns>
        /// <remarks>
        /// This is needed, as opposed to simply calling <see cref="NHibernate.SessionExtensions.GetCurrentTransaction(ISession)" />,
        /// because <see cref="ISession.GetSessionImplementation()" /> can be <see langword="null" />.
        /// </remarks>
        public static ITransaction? GetCurrentTransaction(this ISession session) =>
            session.GetSessionImplementation()?
                   .ConnectionManager?
                   .CurrentTransaction;
    }

    public static class StatelessSessionExtensions
    {
        /// <summary>
        /// Get current transaction if any is ongoing; else, <see langword="null" />.
        /// </summary>
        /// <param name="session">The <see cref="IStatelessSession" />.</param>
        /// <returns>Current transaction or <see langword="null" />.</returns>
        /// <remarks>
        /// This is needed, as opposed to simply calling <see cref="NHibernate.StatelessSessionExtensions.GetCurrentTransaction(IStatelessSession)" />,
        /// because <see cref="IStatelessSession.GetSessionImplementation()" /> can be <see langword="null" />.
        /// </remarks>
        public static ITransaction? GetCurrentTransaction(this IStatelessSession session) =>
            session.GetSessionImplementation()?
                   .ConnectionManager?
                   .CurrentTransaction;
    }
}
