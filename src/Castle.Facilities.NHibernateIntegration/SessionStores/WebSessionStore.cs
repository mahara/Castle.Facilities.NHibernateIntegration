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
    using System.Collections.Generic;
#if NETFRAMEWORK
    using System.Web;
#endif

    using Castle.MicroKernel.Facilities;
#if NET

    using Microsoft.AspNetCore.Http;
#endif

#if NET
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// This is intended for ASP.NET (Core) projects.
    /// </summary>
#else
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// This is intended for legacy ASP.NET projects.
    /// </summary>
#endif
    public class WebSessionStore : AbstractDictionaryStackSessionStore
    {
#if NET
        [CLSCompliant(false)]
        public IHttpContextAccessor HttpContextAccessor { get; set; }
#endif

        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
            return GetSessionDictionaryFromWebContext<IDictionary<string, Stack<SessionDelegate>>>(SessionSlotKey);
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
            StoreSessionDictionaryInWebContext(SessionSlotKey, dictionary);
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
            return GetSessionDictionaryFromWebContext<IDictionary<string, Stack<StatelessSessionDelegate>>>(StatelessSessionSlotKey);
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
            StoreSessionDictionaryInWebContext(StatelessSessionSlotKey, dictionary);
        }

        private T GetSessionDictionaryFromWebContext<T>(string key)
        {
#if NET
            if (!GetWebContext().Items.TryGetValue(key, out var value))
            {
                return default!;
            }
#else
            var value = GetWebContext().Items[key];
#endif

            return (T) value!;
        }

        private void StoreSessionDictionaryInWebContext<T>(string key, T value)
        {
            GetWebContext().Items[key] = value;
        }

#if NET
        private HttpContext GetWebContext()
        {
            var context = HttpContextAccessor?.HttpContext ??
                          throw new FacilityException($"'{nameof(WebSessionStore)}': Could not obtain reference to '{nameof(HttpContext)}'.");
            return context;
        }
#else
        private HttpContext GetWebContext()
        {
            var context = HttpContext.Current ??
                          throw new FacilityException($"'{nameof(WebSessionStore)}': Could not obtain reference to '{nameof(HttpContext)}'.");
            return context;
        }
#endif
    }
}
