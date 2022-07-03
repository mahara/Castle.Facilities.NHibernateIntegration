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
    using System.Collections;
    using System.Threading;

    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="AsyncLocal{T}" />.
    /// </summary>
    public class AsyncLocalSessionStore : AbstractDictionaryStackSessionStore
    {
        private readonly AsyncLocal<IDictionary> _sessionAsyncLocal = new();
        private readonly AsyncLocal<IDictionary> _statelessSessionAsyncLocal = new();

        protected override IDictionary GetSessionDictionary()
        {
            return _sessionAsyncLocal.Value;
        }

        protected override void StoreSessionDictionary(IDictionary dictionary)
        {
            _sessionAsyncLocal.Value = dictionary;
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            return _statelessSessionAsyncLocal.Value;
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            _statelessSessionAsyncLocal.Value = dictionary;
        }
    }
}
