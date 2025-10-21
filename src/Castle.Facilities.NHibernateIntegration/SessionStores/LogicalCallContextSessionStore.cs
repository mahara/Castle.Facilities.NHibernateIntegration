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

#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#endif

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
#if NETFRAMEWORK
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="LogicalCallContext" />.
    /// </summary>
#else
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on .NET Framework <c>LogicalCallContext</c>.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// <c>LogicalCallContext</c> is not supported on .NET (Core).
    /// </exception>
    /// <remarks>
    /// Use <see cref="AsyncLocalSessionStore" /> instead.
    /// </remarks>
    [Obsolete($"'LogicalCallContext' is not supported on .NET (Core). Use '{nameof(AsyncLocalSessionStore)}' instead.")]
#endif
    public class LogicalCallContextSessionStore : AbstractDictionaryStackSessionStore
    {
        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<SessionDelegate>>) CallContext.LogicalGetData(SessionStacks_SlotName);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(SessionStacks_SlotName, dictionary);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<StatelessSessionDelegate>>) CallContext.LogicalGetData(StatelessSessionStacks_SlotName);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(StatelessSessionStacks_SlotName, dictionary);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }
    }
}
