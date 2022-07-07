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

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
    public abstract class AbstractSessionStore : MarshalByRefObject, ISessionStore
    {
        public bool IsCurrentActivityEmptyFor(string alias)
        {
            var sessionStack = GetSessionStackFor(alias);
            var statelessSessionStack = GetStatelessSessionStackFor(alias);

            return sessionStack.Count == 0 && statelessSessionStack.Count == 0;
        }

        /// <summary>
        /// Gets the stack of <see cref="SessionDelegate" /> objects for the specified <paramref name="alias" />.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        protected abstract Stack<SessionDelegate> GetSessionStackFor(string alias);

        public SessionDelegate FindCompatibleSession(string alias)
        {
            var stack = GetSessionStackFor(alias);

            return stack.Count > 0 ?
                   stack.Peek() :
                   null;
        }

        public void Store(string alias, SessionDelegate session)
        {
            var stack = GetSessionStackFor(alias);

            stack.Push(session);

            session.SessionStoreCookie = stack;
        }

        public void Remove(SessionDelegate session)
        {
            var stack = (Stack<SessionDelegate>) session.SessionStoreCookie;

            if (stack is null)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(SessionDelegate)})' called with no cookie - no pun intended.";
                throw new InvalidProgramException(message);
            }

            if (stack.Count == 0)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(SessionDelegate)})' called for an empty stack.";
                throw new InvalidProgramException(message);
            }

            var currentSession = stack.Peek() as ISession;

            if (session != currentSession)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(SessionDelegate)})' tried to remove a session which is not on the top or not in the stack at all.";
                throw new InvalidProgramException(message);
            }

            stack.Pop();
        }

        /// <summary>
        /// Gets the stack of <see cref="StatelessSessionDelegate" /> objects
        /// for the specified <paramref name="alias" />.
        /// </summary>
        /// <param name="alias">The alias.</param>
        /// <returns></returns>
        protected abstract Stack<StatelessSessionDelegate> GetStatelessSessionStackFor(string alias);

        public StatelessSessionDelegate FindCompatibleStatelessSession(string alias)
        {
            var stack = GetStatelessSessionStackFor(alias);

            return stack.Count > 0 ?
                   stack.Peek() :
                   null;
        }

        public void Store(string alias, StatelessSessionDelegate session)
        {
            var stack = GetStatelessSessionStackFor(alias);

            stack.Push(session);

            session.SessionStoreCookie = stack;
        }

        public void Remove(StatelessSessionDelegate session)
        {
            var stack = (Stack<StatelessSessionDelegate>) session.SessionStoreCookie;

            if (stack is null)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(StatelessSessionDelegate)})' called with no cookie - no pun intended.";
                throw new InvalidProgramException(message);
            }

            if (stack.Count == 0)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(StatelessSessionDelegate)})' called for an empty stack.";
                throw new InvalidProgramException(message);
            }

            var currentSession = stack.Peek() as IStatelessSession;

            if (session != currentSession)
            {
                var message = $"'{nameof(AbstractSessionStore)}.{nameof(Remove)}({nameof(StatelessSessionDelegate)})' tried to remove a session which is not on the top or not in the stack at all.";
                throw new InvalidProgramException(message);
            }

            stack.Pop();
        }
    }
}
