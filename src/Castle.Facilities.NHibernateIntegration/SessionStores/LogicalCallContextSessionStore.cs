﻿#region License
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
#if NETFRAMEWORK
    using System.Collections;
    using System.Runtime.Remoting.Messaging;

    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on logical <see cref="CallContext" />.
    /// </summary>
    public class LogicalCallContextSessionStore : AbstractDictStackSessionStore
    {
        protected override IDictionary GetDictionary()
        {
            return CallContext.LogicalGetData(SlotKey) as IDictionary;
        }

        protected override void StoreDictionary(IDictionary dictionary)
        {
            CallContext.LogicalSetData(SlotKey, dictionary);
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            return CallContext.LogicalGetData(StatelessSessionSlotKey) as IDictionary;
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            CallContext.LogicalSetData(StatelessSessionSlotKey, dictionary);
        }
    }
#else
    using System.Collections;

    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on .NET Framework logical CallContext.
    /// </summary>
    public class LogicalCallContextSessionStore : AbstractDictStackSessionStore
    {
        protected override IDictionary GetDictionary()
        {
            throw new PlatformNotSupportedException();
        }

        protected override void StoreDictionary(IDictionary dictionary)
        {
            throw new PlatformNotSupportedException();
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            throw new PlatformNotSupportedException();
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            throw new PlatformNotSupportedException();
        }
    }
#endif
}