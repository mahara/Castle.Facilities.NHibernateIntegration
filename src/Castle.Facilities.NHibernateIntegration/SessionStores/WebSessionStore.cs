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
    using System.Web;
#endif

    using MicroKernel.Facilities;

#if NET
    using Microsoft.AspNetCore.Http;
#endif

#if NETFRAMEWORK
    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// This is intended for legacy ASP.NET projects.
    /// </summary>
#else
    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// This is intended for ASP.NET (Core) projects.
    /// </summary>
#endif
    public class WebSessionStore : AbstractDictionaryStackSessionStore
    {
#if NET
        [CLSCompliant(false)]
        public IHttpContextAccessor HttpContextAccessor { get; set; }
#endif

        protected override IDictionary GetSessionDictionary()
        {
            return GetSessionContextDictionary(SessionSlotKey);
        }

        protected override void StoreSessionDictionary(IDictionary dictionary)
        {
            StoreSessionContextDictionary(SessionSlotKey, dictionary);
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            return GetSessionContextDictionary(StatelessSessionSlotKey);
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            StoreSessionContextDictionary(StatelessSessionSlotKey, dictionary);
        }

#if NETFRAMEWORK
        private HttpContext ObtainSessionContext()
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                throw new FacilityException($"{nameof(WebSessionStore)}: Could not obtain reference to {nameof(HttpContext)}.");
            }

            return context;
        }
#else
        private HttpContext ObtainSessionContext()
        {
            var context = HttpContextAccessor.HttpContext;
            if (context == null)
            {
                throw new FacilityException($"{nameof(WebSessionStore)}: Could not obtain reference to {nameof(HttpContext)}.");
            }

            return context;
        }
#endif

        private IDictionary GetSessionContextDictionary(string key)
        {
#if NETFRAMEWORK
            var dictionary = ObtainSessionContext().Items[key];
#else
            var dictionary = ObtainSessionContext().Items[key];
#endif

            return (IDictionary) dictionary;
        }

        private void StoreSessionContextDictionary(string key, IDictionary value)
        {
#if NETFRAMEWORK
            ObtainSessionContext().Items[key] = value;
#else
            ObtainSessionContext().Items[key] = value;
#endif
        }
    }
}