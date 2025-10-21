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

using System;
using System.Collections.Generic;

using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;

using NHibernate;

namespace Castle.Facilities.NHibernateIntegration.Internals
{
    /// <summary>
    /// Default implementation of <see cref="ISessionFactoryResolver" />
    /// that always queries the kernel instance for the <see cref="ISessionFactory" /> instance.
    /// <para>
    /// This gives a chance to developers to change the <see cref="ISessionFactory" /> instance
    /// during the application lifetime.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Inspired by Cuyahoga project.
    /// </remarks>
    public class SessionFactoryResolver : ISessionFactoryResolver
    {
        private readonly IKernel _kernel;
        private readonly Dictionary<string, string> _aliasToId = new(StringComparer.OrdinalIgnoreCase);

        public SessionFactoryResolver(IKernel kernel)
        {
            _kernel = kernel;
        }

        public void RegisterAliasToIdMapping(string alias, string id)
        {
            if (alias is null)
            {
                throw new ArgumentNullException(nameof(alias));
            }
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (_aliasToId.ContainsKey(alias))
            {
                var message = $"A mapping already exists for the specified alias: '{alias}'.";
                throw new ArgumentException(message);
            }

            _aliasToId.Add(alias, id);
        }

        public ISessionFactory GetSessionFactory(string alias)
        {
            if (alias is null)
            {
                throw new ArgumentNullException(nameof(alias));
            }

            if (!_aliasToId.TryGetValue(alias, out var id))
            {
                var message = $"An '{nameof(ISessionFactory)}' component was not mapped for the specified alias: '{alias}'.";
                throw new FacilityException(message);
            }

            return _kernel.Resolve<ISessionFactory>(id);
        }
    }
}
