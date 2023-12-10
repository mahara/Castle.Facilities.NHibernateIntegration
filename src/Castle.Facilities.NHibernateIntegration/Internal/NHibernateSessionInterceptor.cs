#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
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

namespace Castle.Facilities.NHibernateIntegration.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Castle.Core;
    using Castle.Core.Interceptor;
    using Castle.Core.Logging;
    using Castle.DynamicProxy;

    /// <summary>
    /// Interceptor in charge of the automatic session management.
    /// </summary>
    [Transient]
    public class NHibernateSessionInterceptor : IInterceptor, IOnBehalfAware
    {
        private readonly ISessionManager _sessionManager;
        private IEnumerable<MethodInfo>? _metaInfo;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NHibernateSessionInterceptor(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; set; } =
            NullLogger.Instance;

        /// <summary>
        /// Intercepts the specified invocation and creates a transaction if necessary.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        /// <returns></returns>
        public void Intercept(IInvocation invocation)
        {
            MethodInfo methodInfo;

            if (invocation.Method.DeclaringType!.IsInterface)
            {
                methodInfo = invocation.MethodInvocationTarget;
            }
            else
            {
                methodInfo = invocation.Method;
            }

            if (_metaInfo == null || !_metaInfo.Contains(methodInfo))
            {
                invocation.Proceed();

                return;
            }

            var session = _sessionManager.OpenSession();

            try
            {
                invocation.Proceed();
            }
            finally
            {
                session.Dispose();
            }
        }

        /// <summary>
        /// Sets the intercepted component's ComponentModel.
        /// </summary>
        /// <param name="target">The target's ComponentModel</param>
        public void SetInterceptedComponentModel(ComponentModel target)
        {
            _metaInfo = (MethodInfo[]) target.ExtendedProperties[NHibernateSessionComponentInspector.SessionRequiredMetaInfo];
        }
    }
}
