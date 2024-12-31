#region License
// Copyright 2004-2025 Castle Project - https://www.castleproject.org/
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

using Castle.Core;

using Castle.MicroKernel;
using Castle.MicroKernel.ComponentActivator;
using Castle.MicroKernel.Context;

using NHibernate;
using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration.Internals
{
    /// <summary>
    /// Postpones the initiation of <see cref="ISessionFactory" /> until <see cref="IKernel.Resolve{T}()" />.
    /// </summary>
    public class SessionFactoryActivator : DefaultComponentActivator
    {
        public SessionFactoryActivator(
            ComponentModel model,
            IKernelInternal kernel,
            ComponentInstanceDelegate onCreation,
            ComponentInstanceDelegate onDestruction) :
            base(model, kernel, onCreation, onDestruction)
        {
        }

        /// <summary>
        /// Creates the <see cref="ISessionFactory" /> from the configuration.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="burden"></param>
        /// <returns></returns>
        public override object Create(CreationContext context, Burden burden)
        {
            RaiseCreatingSessionFactory();

            var configuration = (Configuration) Model.ExtendedProperties[Constants.SessionFactory_Configuration_ComponentPropertyName];

            var sessionFactory = configuration.BuildSessionFactory();

            burden.SetRootInstance(sessionFactory);

            return sessionFactory;
        }

        /// <summary>
        /// Calls the contributors.
        /// </summary>
        protected virtual void RaiseCreatingSessionFactory()
        {
            if (Model.ExtendedProperties[Constants.SessionFactory_Configuration_ComponentPropertyName] is Configuration configuration)
            {
                var contributors = Kernel.ResolveAll<IConfigurationContributor>();
                foreach (var contributor in contributors)
                {
                    contributor.Process(Model.Name, configuration);
                }
            }
        }
    }
}
