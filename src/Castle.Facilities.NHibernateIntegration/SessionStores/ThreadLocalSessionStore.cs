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
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="ThreadLocal{T}" />.
    /// </summary>
    /// <remarks>
    /// REFERENCES:
    /// -   <see href="https://github.com/hconceicao/Castle.Facilities.NHibernateIntegration3/blob/c927cc1788ed02260a2c46688971c3cdaaba7622/src/Castle.Facilities.NHibernateIntegration/SessionStores/ThreadLocalSessionStore.cs" />
    /// </remarks>
    public class ThreadLocalSessionStore : AbstractDictionaryStackSessionStore
    {
        private readonly ThreadLocal<IDictionary<string, Stack<SessionDelegate>>> _sessionThreadLocal = new();
        private readonly ThreadLocal<IDictionary<string, Stack<StatelessSessionDelegate>>> _statelessSessionThreadLocal = new();

        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
            return _sessionThreadLocal.Value!;
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
            _sessionThreadLocal.Value = dictionary;
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
            return _statelessSessionThreadLocal.Value!;
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
            _statelessSessionThreadLocal.Value = dictionary;
        }
    }
}
