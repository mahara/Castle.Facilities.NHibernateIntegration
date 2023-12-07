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

#if NETFRAMEWORK
using System.Web;

using Castle.MicroKernel.Facilities;
using Castle.Windsor;

using NHibernate;

namespace Castle.Facilities.NHibernateIntegration.Components.Web
{
    /// <summary>
    /// <see cref="IHttpModule" /> to set up a session for the request lifetime.
    /// <seealso cref="ISessionManager" />
    /// </summary>
    /// <remarks>
    /// To install the module, you must:
    /// <para>
    ///   <list type="number">
    ///     <item>
    ///       <description>
    ///         Add the module to the <c>httpModules</c> configuration section within <c>system.web</c>
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         Extend the <see cref="HttpApplication" /> if you haven't.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <description>
    ///         Make your <see cref="HttpApplication" /> subclass implement <see cref="IContainerAccessor" />
    ///         so the module can access the container instance.
    ///       </description>
    ///     </item>
    ///   </list>
    /// </para>
    /// </remarks>
    public class SessionWebModule : IHttpModule
    {
        public const string SessionKey = "SessionWebModule.session";

        private HttpApplication _httpApplication;

        /// <summary>
        /// Initializes a module and prepares it to handle requests.
        /// </summary>
        /// <param name="context">The app.</param>
        public void Init(HttpApplication context)
        {
            _httpApplication = context;

            _httpApplication.BeginRequest += OnBeginRequest;
            _httpApplication.EndRequest += OnEndRequest;
        }

        public void Dispose()
        {
            _httpApplication.BeginRequest -= OnBeginRequest;
            _httpApplication.EndRequest -= OnEndRequest;

            _httpApplication = null;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var container = GetContainer();

            var sessionManager = container.Resolve<ISessionManager>();
            HttpContext.Current.Items.Add(SessionKey, sessionManager.OpenSession());
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var session = (ISession) HttpContext.Current.Items[SessionKey];
            session?.Dispose();
        }

        private static IWindsorContainer GetContainer()
        {
            if (HttpContext.Current.ApplicationInstance is not IContainerAccessor containerAccessor)
            {
                var message = $"You must extend the '{nameof(HttpApplication)}' in your web project " +
                              $"and implement the '{nameof(IContainerAccessor)}' to properly expose your container instance.";
                throw new FacilityException(message);
            }

            var container = containerAccessor.Container;

            if (container is null)
            {
                var message = $"The container seems to be unavailable (null) in your '{nameof(HttpApplication)}' subclass.";
                throw new FacilityException(message);
            }

            return container;
        }
    }
}
#endif
