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
using System.Web;

using Castle.MicroKernel.Facilities;

namespace Castle.Facilities.NHibernateIntegration.SessionStores
{
    /// <summary>
    /// Provides an implementation of <see cref="ISessionStore" />
    /// which relies on <see cref="HttpContext" />.
    /// This is intended for ASP.NET projects.
    /// </summary>
    public class WebSessionStore : AbstractDictionaryStackSessionStore
    {
        protected override IDictionary<string, Stack<SessionDelegate>> GetSessionDictionary()
        {
            var httpContext = ObtainHttpContext();

            return (IDictionary<string, Stack<SessionDelegate>>) httpContext.Items[SessionStacks_SlotName];
        }

        protected override void StoreSessionDictionary(IDictionary<string, Stack<SessionDelegate>> dictionary)
        {
            var httpContext = ObtainHttpContext();

            httpContext.Items[SessionStacks_SlotName] = dictionary;
        }

        protected override IDictionary<string, Stack<StatelessSessionDelegate>> GetStatelessSessionDictionary()
        {
            var httpContext = ObtainHttpContext();

            return (IDictionary<string, Stack<StatelessSessionDelegate>>) httpContext.Items[StatelessSessionStacks_SlotName];
        }

        protected override void StoreStatelessSessionDictionary(IDictionary<string, Stack<StatelessSessionDelegate>> dictionary)
        {
            var httpContext = ObtainHttpContext();

            httpContext.Items[StatelessSessionStacks_SlotName] = dictionary;
        }

        private static HttpContext ObtainHttpContext()
        {
            var httpContext = HttpContext.Current;

            if (httpContext == null)
            {
                var message = $"'{nameof(WebSessionStore)}': Could not obtain reference to '{nameof(HttpContext)}'.";
                throw new FacilityException(message);
            }

            return httpContext;
        }
    }
}
