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

using System.Xml;

using Castle.Core.Configuration;

using Castle.Facilities.NHibernateIntegration.Internal;
using Castle.Services.Transaction.Utilities;

using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    /// <summary>
    /// The configuration builder for NHibernate's cfg.xml.
    /// </summary>
    public class XmlConfigurationBuilder : IConfigurationBuilder
    {
        /// <summary>
        /// Returns the NHibernate <see cref="Configuration" /> object for the given XML.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>A NHibernate <see cref="Configuration" />.</returns>
        public Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            var filePath = facilityConfiguration.Attributes[Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName];

#if NET8_0_OR_GREATER
            ArgumentException.ThrowIfNullOrEmpty(filePath, Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName);
#else
            if (filePath.IsNullOrEmpty())
            {
                throw new ArgumentException($"'{Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName}' cannot be null or empty.", Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName);
            }
#endif

            using var configurationResource = new FileAssemblyResource(filePath);
            using var reader = XmlReader.Create(configurationResource.GetStreamReader());

            var configuration = new Configuration();

            configuration.Configure(reader);

            return configuration;
        }
    }
}
