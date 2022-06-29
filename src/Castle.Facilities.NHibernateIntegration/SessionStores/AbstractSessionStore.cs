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

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
    using System;
    using System.Collections;

    using NHibernate;

    /// <summary>
    ///
    /// </summary>
    public abstract class AbstractSessionStore : MarshalByRefObject, ISessionStore
    {
        /// <summary>
        /// Returns <c>true</c> if the current activity (which is an execution activity context)
        /// has no <see cref="ISession" />/<see cref="SessionDelegate" />
        /// and/or <see cref="IStatelessSession" />/<see cref="StatelessSessionDelegate" /> available.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public bool IsCurrentActivityEmptyFor(string alias)
        {
            var sessionStack = GetSessionStackFor(alias);
            var statelessSessionStack = GetStatelessSessionStackFor(alias);

            return sessionStack.Count == 0 && statelessSessionStack.Count == 0;
        }

        /// <summary>
        /// Gets the session stack of <see cref="SessionDelegate" /> objects
        /// for the specified <paramref name="alias" />.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        protected abstract Stack GetSessionStackFor(string alias);

        /// <summary>
        /// Finds a previously stored <see cref="SessionDelegate" /> for the given alias if available.
        /// Otherwise, null.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public SessionDelegate FindCompatibleSession(string alias)
        {
            var stack = GetSessionStackFor(alias);

            if (stack.Count == 0)
            {
                return null;
            }

            return stack.Peek() as SessionDelegate;
        }

        /// <summary>
        /// Stores the specified <see cref="SessionDelegate" /> instance.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="session"></param>
        public void Store(string alias, SessionDelegate session)
        {
            var stack = GetSessionStackFor(alias);

            stack.Push(session);

            session.SessionStoreCookie = stack;
        }

        /// <summary>
        /// Removes the <see cref="SessionDelegate" /> from the store.
        /// </summary>
        /// <param name="session"></param>
        public void Remove(SessionDelegate session)
        {
            var stack = (Stack) session.SessionStoreCookie;

            if (stack == null)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} called with no cookie.");
            }

            if (stack.Count == 0)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} called for an empty stack.");
            }

            var current = stack.Peek() as ISession;
            if (session != current)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} tried to " +
                                                  "remove a session which is not on the top or not in the stack at all");
            }

            stack.Pop();
            session.SessionStoreCookie = null;
        }

        /// <summary>
        /// Gets the stack of <see cref="StatelessSessionDelegate" /> objects
        /// for the specified <paramref name="alias" />.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        protected abstract Stack GetStatelessSessionStackFor(string alias);

        /// <summary>
        /// Find a previously stored <see cref="StatelessSessionDelegate" /> for the given alias if available.
        /// Otherwise, null.
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        public StatelessSessionDelegate FindCompatibleStatelessSession(string alias)
        {
            var stack = GetStatelessSessionStackFor(alias);

            if (stack.Count == 0)
            {
                return null;
            }

            return stack.Peek() as StatelessSessionDelegate;
        }

        /// <summary>
        /// Stores the specified <see cref="StatelessSessionDelegate" /> instance.
        /// </summary>
        /// <param name="alias"></param>
        /// <param name="statelessSession"></param>
        public void Store(string alias, StatelessSessionDelegate statelessSession)
        {
            var stack = GetStatelessSessionStackFor(alias);

            stack.Push(statelessSession);

            statelessSession.SessionStoreCookie = stack;
        }

        /// <summary>
        /// Removes the <see cref="StatelessSessionDelegate" /> from the store.
        /// </summary>
        /// <param name="statelessSession"></param>
        public void Remove(StatelessSessionDelegate statelessSession)
        {
            var stack = (Stack) statelessSession.SessionStoreCookie;

            if (stack == null)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} called with no cookie.");
            }

            if (stack.Count == 0)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} called for an empty stack.");
            }

            var current = stack.Peek() as IStatelessSession;
            if (statelessSession != current)
            {
                throw new InvalidProgramException($"{nameof(AbstractSessionStore)}.{nameof(Remove)} tried to " +
                                                  "remove a session which is not on the top or not in the stack at all.");
            }

            stack.Pop();
            statelessSession.SessionStoreCookie = null;
        }
    }
}