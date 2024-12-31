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

using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Allows implementors to modify NHibernate <see cref="Configuration" />.
    /// </summary>
    public interface IConfigurationContributor
    {
        /// <summary>
        /// Modifies available <see cref="Configuration" /> instances.
        /// </summary>
        /// <param name="sessionFactoryName">The name of the session factory.</param>
        /// <param name="configuration">The configuration for session factory.</param>
        void Process(string sessionFactoryName, Configuration configuration);
    }
}
