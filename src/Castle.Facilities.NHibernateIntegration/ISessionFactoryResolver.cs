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

using Castle.MicroKernel.Facilities;

using NHibernate;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// A contract for possible different approaches of <see cref="ISessionFactory" />s obtention.
    /// </summary>
    /// <remarks>
    /// Inspired by Cuyahoga project.
    /// </remarks>
    public interface ISessionFactoryResolver
    {
        /// <summary>
        /// Invoked by the facility while the configuration node are being interpreted.
        /// </summary>
        /// <param name="alias">
        /// The alias associated with the <see cref="ISessionFactory" /> on the configuration node.
        /// </param>
        /// <param name="componentId">
        /// The component ID associated with the <see cref="ISessionFactory" /> on the kernel.
        /// </param>
        void RegisterAliasComponentIdMapping(string alias, string componentId);

        /// <summary>
        /// Implementors should return a <see cref="ISessionFactory" /> instance
        /// for the specified alias configured previously.
        /// </summary>
        /// <param name="alias">
        /// The alias associated with the <see cref="ISessionFactory" /> on the configuration node.
        /// </param>
        /// <returns>
        /// A <see cref="ISessionFactory" /> instance.
        /// </returns>
        /// <exception cref="FacilityException">
        /// If the alias is not associated with a <see cref="ISessionFactory" />.
        /// </exception>
        ISessionFactory GetSessionFactory(string alias);
    }
}
