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
#if NETFRAMEWORK
    using System.Runtime.Remoting.Messaging;
#endif

#if NETFRAMEWORK
    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on logical <see cref="CallContext" />.
    /// </summary>
#else
    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on .NET Framework logical CallContext.
    /// </summary>
    /// <exception cref="PlatformNotSupportedException">
    /// This is not supported in .NET.
    /// </exception>
#endif

    public class LogicalCallContextSessionStore : AbstractDictStackSessionStore
    {
        protected override IDictionary GetDictionary()
        {
#if NETFRAMEWORK
            return CallContext.LogicalGetData(SlotKey) as IDictionary;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        protected override void StoreDictionary(IDictionary dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(SlotKey, dictionary);
#else
            throw new PlatformNotSupportedException();
#endif
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
#if NETFRAMEWORK
            return CallContext.LogicalGetData(StatelessSessionSlotKey) as IDictionary;
#else
            throw new PlatformNotSupportedException();
#endif
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
#if NETFRAMEWORK
            CallContext.LogicalSetData(StatelessSessionSlotKey, dictionary);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}