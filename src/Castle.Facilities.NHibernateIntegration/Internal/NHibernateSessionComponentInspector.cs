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

namespace Castle.Facilities.NHibernateIntegration.Internal
{
    using System.Linq;
    using System.Reflection;

    using Castle.Core;
    using Castle.MicroKernel;
    using Castle.MicroKernel.ModelBuilder;

    /// <summary>
    /// Inspect components searching for Session Aware services.
    /// </summary>
    public class NHibernateSessionComponentInspector : IContributeComponentModelConstruction
    {
        internal const string SessionRequiredMetaInfo = "nhfacility.sessionRequiredMetaInfo";

        private const string ComponentModelName = "session.interceptor";

        private static readonly BindingFlags BindingFlags =
            BindingFlags.Instance |
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Process the model.
        /// </summary>
        public void ProcessModel(IKernel kernel, ComponentModel model)
        {
            if (model.Implementation.IsDefined(typeof(NHSessionAwareAttribute), true))
            {
                model.Dependencies.Add(
                    new DependencyModel(ComponentModelName, typeof(NHibernateSessionInterceptor), false));

                var methods = model.Implementation
                                   .GetMethods(BindingFlags)
                                   .Where(m => m.IsDefined(typeof(NHSessionRequiredAttribute), false));
                model.ExtendedProperties[SessionRequiredMetaInfo] = methods.ToArray();

                model.Interceptors.Add(
                    new InterceptorReference(typeof(NHibernateSessionInterceptor)));
            }
        }
    }
}