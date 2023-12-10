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

namespace Castle.Facilities.NHibernateIntegration
{
    using Castle.Core.Configuration;

    using NHibernate.Cfg;

    /// <summary>
    /// Builds up the NHibernate <see cref="Configuration" />.
    /// </summary>
    public interface IConfigurationBuilder
    {
        /// <summary>
        /// Builds the NHibernate <see cref="Configuration" /> object from the specified facility <see cref="IConfiguration" />.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>An NHibernate <see cref="Configuration" />.</returns>
        Configuration GetConfiguration(IConfiguration facilityConfiguration);
    }
}
