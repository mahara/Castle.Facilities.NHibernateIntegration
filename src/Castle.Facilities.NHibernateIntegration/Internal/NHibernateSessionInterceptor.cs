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

using System.Reflection;

using Castle.Core;
using Castle.Core.Interceptor;
using Castle.Core.Logging;
using Castle.DynamicProxy;

namespace Castle.Facilities.NHibernateIntegration.Internal
{
    /// <summary>
    /// Interceptor in charge of the automatic session management.
    /// </summary>
    [Transient]
    public class NHibernateSessionInterceptor : IInterceptor, IOnBehalfAware
    {
        private readonly ISessionManager _sessionManager;
        private IEnumerable<MethodInfo> _methods;

        public NHibernateSessionInterceptor(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public void SetInterceptedComponentModel(ComponentModel target)
        {
            _methods = (MethodInfo[]) target.ExtendedProperties[NHibernateSessionComponentInspector.SessionRequiredMetaInfo_ConfigurationPropertyName];
        }

        /// <summary>
        /// Intercepts the specified invocation and creates a transaction if necessary.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        /// <returns></returns>
        public void Intercept(IInvocation invocation)
        {
            MethodInfo method;

            if (invocation.Method.DeclaringType.IsInterface)
            {
                method = invocation.MethodInvocationTarget;
            }
            else
            {
                method = invocation.Method;
            }

            if (_methods is null || !_methods.Contains(method))
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
    }
}
