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

    using MicroKernel.Facilities;

    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// It's intended for legacy ASP.NET projects.
    /// </summary>
    public class WebSessionStore : AbstractDictStackSessionStore
    {
        protected override IDictionary GetDictionary()
        {
            var currentContext = ObtainSessionContext();

            return currentContext.Items[SlotKey] as IDictionary;
        }

        protected override void StoreDictionary(IDictionary dictionary)
        {
            var currentContext = ObtainSessionContext();

            currentContext.Items[SlotKey] = dictionary;
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            var currentContext = ObtainSessionContext();

            return currentContext.Items[StatelessSessionSlotKey] as IDictionary;
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            var currentContext = ObtainSessionContext();

            currentContext.Items[StatelessSessionSlotKey] = dictionary;
        }

        private static HttpContext ObtainSessionContext()
        {
            var context = HttpContext.Current;
            if (context == null)
            {
                throw new FacilityException($"{nameof(WebSessionStore)}: Could not obtain reference to {nameof(HttpContext)}.");
            }

            return context;
        }
    }
#else

    using MicroKernel.Facilities;

    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// An implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// It's intended for ASP.NET (Core) projects.
    /// </summary>
    public class WebSessionStore : AbstractDictStackSessionStore
    {
        private readonly HttpContext _httpContext;

        [CLSCompliant(false)]
        public WebSessionStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        protected override IDictionary GetDictionary()
        {
            var currentContext = ObtainSessionContext();

            return currentContext.Items[SlotKey] as IDictionary;
        }

        protected override void StoreDictionary(IDictionary dictionary)
        {
            var currentContext = ObtainSessionContext();

            currentContext.Items[SlotKey] = dictionary;
        }

        protected override IDictionary GetStatelessSessionDictionary()
        {
            var currentContext = ObtainSessionContext();

            return currentContext.Items[StatelessSessionSlotKey] as IDictionary;
        }

        protected override void StoreStatelessSessionDictionary(IDictionary dictionary)
        {
            var currentContext = ObtainSessionContext();

            currentContext.Items[StatelessSessionSlotKey] = dictionary;
        }

        private HttpContext ObtainSessionContext()
        {
            var context = _httpContext;
            if (context == null)
            {
                throw new FacilityException($"{nameof(WebSessionStore)}: Could not obtain reference to {nameof(HttpContext)}.");
            }

            return context;
        }
    }
#endif
}