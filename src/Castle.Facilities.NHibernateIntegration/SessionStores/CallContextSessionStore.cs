#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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

using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="CallContext" />.
    /// </summary>
    public class CallContextSessionStore : AbstractDictionaryStackSessionStore
    {
        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
            return (IDictionary<string, Stack<SessionDelegate>>) CallContext.GetData(SessionStacks_SlotName);
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
            CallContext.SetData(SessionStacks_SlotName, dictionary);
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
            return (IDictionary<string, Stack<StatelessSessionDelegate>>) CallContext.GetData(StatelessSessionStacks_SlotName);
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
            CallContext.SetData(StatelessSessionStacks_SlotName, dictionary);
        }
    }
}
