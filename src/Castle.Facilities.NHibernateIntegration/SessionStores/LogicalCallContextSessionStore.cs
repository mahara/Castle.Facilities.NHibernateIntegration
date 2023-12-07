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
    using System.Collections.Generic;
#if NETFRAMEWORK
    using System.Runtime.Remoting.Messaging;
#endif

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
    /// This is not supported anymore in .NET (Core).
    /// </exception>
    [Obsolete("'LogicalCallContext' is not supported on .NET (Core).")]
#endif
    public class LogicalCallContextSessionStore : AbstractDictionaryStackSessionStore
    {
        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<SessionDelegate>>) CallContext.LogicalGetData(SessionSlotKey);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(SessionSlotKey, dictionary);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
#if NETFRAMEWORK
            return (IDictionary<string, Stack<StatelessSessionDelegate>>) CallContext.LogicalGetData(StatelessSessionSlotKey);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(StatelessSessionSlotKey, dictionary);
#else
            var message = "'LogicalCallContext' is not supported on .NET (Core).";
            throw new PlatformNotSupportedException(message);
#endif
        }
    }
}
