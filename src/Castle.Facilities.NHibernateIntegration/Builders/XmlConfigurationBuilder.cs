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

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    using System.Xml;

    using Core.Configuration;

    using Internal;

    using NHibernate.Cfg;

    /// <summary>
    /// The configuration builder for NHibernate's own cfg.xml.
    /// </summary>
    public class XmlConfigurationBuilder : IConfigurationBuilder
    {
        /// <summary>
        /// Returns the <see cref="Configuration" /> object for the given xml.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>An NHibernate <see cref="Configuration" />.</returns>
        public Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            Configuration configuration;

            var configurationFile = facilityConfiguration.Attributes["nhibernateConfigFile"];
            using (var configurationResource = new FileAssemblyResource(configurationFile))
            {
                using var reader = XmlReader.Create(configurationResource.GetStreamReader());
                configuration = new Configuration();
                configuration.Configure(reader);
            }

            return configuration;
        }
    }
}