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
    using System.Collections.Specialized;

    /// <summary>
    ///
    /// </summary>
    public abstract class AbstractDictionaryStackSessionStore : AbstractSessionStore
    {
        private readonly object _lock = new();

        protected AbstractDictionaryStackSessionStore()
        {
            SessionSlotKey =
                string.Format("nh.facility.stacks.session.{0}",
                              Guid.NewGuid());
            StatelessSessionSlotKey =
                string.Format("nh.facility.stacks.statelessSession.{0}",
                              Guid.NewGuid());
        }

        /// <summary>
        /// The <see cref="SessionDelegate" /> storage name.
        /// </summary>
        protected string SessionSlotKey { get; }

        /// <summary>
        /// The <see cref="StatelessSessionDelegate" /> storage name.
        /// </summary>
        protected string StatelessSessionSlotKey { get; }

        /// <inheritdoc />
        protected override Stack GetSessionStackFor(string alias)
        {
            lock (_lock)
            {
                if (alias == null)
                {
                    throw new ArgumentNullException(nameof(alias));
                }

                var alias2Stack = GetSessionDictionary();
                if (alias2Stack == null)
                {
                    alias2Stack = new HybridDictionary(true);

                    StoreSessionDictionary(alias2Stack);
                }

                if (alias2Stack[alias] is not Stack stack)
                {
                    stack = Stack.Synchronized(new Stack());

                    alias2Stack[alias] = stack;
                }

                return stack;
            }
        }

        /// <summary>
        /// Gets the <see cref="SessionDelegate" /> dictionary.
        /// </summary>
        /// <returns></returns>
        protected abstract IDictionary GetSessionDictionary();

        /// <summary>
        /// Stores the <see cref="SessionDelegate" /> dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        protected abstract void StoreSessionDictionary(IDictionary dictionary);

        /// <inheritdoc />
        protected override Stack GetStatelessSessionStackFor(string alias)
        {
            lock (_lock)
            {
                if (alias == null)
                {
                    throw new ArgumentNullException(nameof(alias));
                }

                var alias2Stack = GetStatelessSessionDictionary();
                if (alias2Stack == null)
                {
                    alias2Stack = new HybridDictionary(true);

                    StoreStatelessSessionDictionary(alias2Stack);
                }

                if (alias2Stack[alias] is not Stack stack)
                {
                    stack = Stack.Synchronized(new Stack());

                    alias2Stack[alias] = stack;
                }

                return stack;
            }
        }

        /// <summary>
        /// Gets the <see cref="StatelessSessionDelegate" /> dictionary.
        /// </summary>
        /// <returns>A dictionary.</returns>
        protected abstract IDictionary GetStatelessSessionDictionary();

        /// <summary>
        /// Stores the <see cref="StatelessSessionDelegate" /> dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        protected abstract void StoreStatelessSessionDictionary(IDictionary dictionary);
    }
}