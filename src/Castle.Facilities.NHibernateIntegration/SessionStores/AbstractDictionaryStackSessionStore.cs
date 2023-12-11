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

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
    public abstract class AbstractDictionaryStackSessionStore : AbstractSessionStore
    {
        private readonly object _sessionLock = new();
        private readonly object _statelessSessionLock = new();

        /// <summary>
        /// The <see cref="SessionDelegate" /> stacks slot name.
        /// </summary>
        protected string SessionStacks_SlotName { get; } =
            string.Format(Constants.SessionStore_SessionStacks_SlotNameFormat, Guid.NewGuid());

        /// <summary>
        /// The <see cref="StatelessSessionDelegate" /> stacks slot name.
        /// </summary>
        protected string StatelessSessionStacks_SlotName { get; } =
            string.Format(Constants.SessionStore_StatelessSessionStacks_SlotNameFormat, Guid.NewGuid());

        protected override Stack<SessionDelegate> GetSessionStackFor(string? alias)
        {
            lock (_sessionLock)
            {
#if NET8_0_OR_GREATER
                ArgumentNullException.ThrowIfNull(alias);
#else
                if (alias is null)
                {
                    throw new ArgumentNullException(nameof(alias));
                }
#endif

                var dictionary = GetSessionDictionary();

                if (dictionary is null)
                {
                    dictionary = new Dictionary<string, Stack<SessionDelegate>>(StringComparer.OrdinalIgnoreCase);

                    StoreSessionDictionary(dictionary);
                }

                Stack<SessionDelegate> stack;

                var stackIsFound = dictionary.TryGetValue(alias, out stack!);
                if (!stackIsFound || (stackIsFound && stack is null))
                {
                    stack = new Stack<SessionDelegate>();

                    dictionary[alias] = stack;
                }

                return stack;
            }
        }

        /// <summary>
        /// Gets the <see cref="SessionDelegate" /> dictionary.
        /// </summary>
        /// <returns></returns>
        protected abstract IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary();

        /// <summary>
        /// Stores the <see cref="SessionDelegate" /> dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        protected abstract void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary);

        protected override Stack<StatelessSessionDelegate> GetStatelessSessionStackFor(string? alias)
        {
            lock (_statelessSessionLock)
            {
#if NET8_0_OR_GREATER
                ArgumentNullException.ThrowIfNull(alias);
#else
                if (alias is null)
                {
                    throw new ArgumentNullException(nameof(alias));
                }
#endif

                var dictionary = GetStatelessSessionDictionary();

                if (dictionary is null)
                {
                    dictionary = new Dictionary<string, Stack<StatelessSessionDelegate>>();

                    StoreStatelessSessionDictionary(dictionary);
                }

                Stack<StatelessSessionDelegate> stack;

                var stackIsFound = dictionary.TryGetValue(alias, out stack!);
                if (!stackIsFound || (stackIsFound && stack is null))
                {
                    stack = new Stack<StatelessSessionDelegate>();

                    dictionary[alias] = stack;
                }

                return stack;
            }
        }

        /// <summary>
        /// Gets the <see cref="StatelessSessionDelegate" /> dictionary.
        /// </summary>
        /// <returns>A dictionary.</returns>
        protected abstract IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary();

        /// <summary>
        /// Stores the <see cref="StatelessSessionDelegate" /> dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        protected abstract void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary);
    }
}
