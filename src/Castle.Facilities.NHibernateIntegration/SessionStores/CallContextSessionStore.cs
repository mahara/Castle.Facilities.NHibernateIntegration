#region License
// Copyright 2004-2025 Castle Project - https://www.castleproject.org/
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
    /// which relies on <see cref="CallContext" />.
    /// </summary>
#else
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on .NET Framework <c>CallContext</c>.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// <c>CallContext</c> is not supported on .NET (Core).
    /// </exception>
    /// <remarks>
    /// Use <see cref="ThreadLocalSessionStore" /> instead.
    /// </remarks>
    [Obsolete($"'CallContext' is not supported on .NET (Core). Use '{nameof(ThreadLocalSessionStore)}' instead.")]
#endif
    public class CallContextSessionStore : AbstractDictionaryStackSessionStore
    {
        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<SessionDelegate>>) CallContext.GetData(SessionStacks_SlotName);
#else
            var message = "'CallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.SetData(SessionStacks_SlotName, dictionary);
#else
            var message = "'CallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<StatelessSessionDelegate>>) CallContext.GetData(StatelessSessionStacks_SlotName);
#else
            var message = "'CallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.SetData(StatelessSessionStacks_SlotName, dictionary);
#else
            var message = "'CallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }
    }
}
