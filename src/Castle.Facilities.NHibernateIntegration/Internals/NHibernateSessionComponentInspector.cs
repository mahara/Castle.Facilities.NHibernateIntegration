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

using System.Linq;
using System.Reflection;

using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;

namespace Castle.Facilities.NHibernateIntegration.Internals
{
    /// <summary>
    /// Inspect components searching for session-aware services.
    /// </summary>
    public class NHibernateSessionComponentInspector : IContributeComponentModelConstruction
    {
        private const BindingFlags MethodBindingFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.FlattenHierarchy;

        public void ProcessModel(IKernel kernel, ComponentModel model)
        {
            if (model.Implementation.IsDefined(typeof(NHibernateSessionAwareAttribute), true))
            {
                model.Dependencies.Add(
                    new DependencyModel(Constants.Interceptor_SessionInterceptor_DependencyModelName, typeof(NHibernateSessionInterceptor), false));

                var methods = model.Implementation
                                   .GetMethods(MethodBindingFlags)
                                   .Where(m => m.IsDefined(typeof(NHibernateSessionRequiredAttribute), false))
                                   .ToArray();
                model.ExtendedProperties[Constants.Contributor_SessionComponentInspector_SessionRequiredMethods_PropertyName] = methods;

                model.Interceptors.Add(
                    new InterceptorReference(typeof(NHibernateSessionInterceptor)));
            }
        }
    }
}
