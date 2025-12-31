#region License
// Copyright (c) 2004-2026 Castle Project - https://www.castleproject.org/
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

using Castle.Facilities.NHibernateIntegration.Internals;
using Castle.Services.Transaction.Utilities;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using ConfigurationErrorsException = System.Configuration.ConfigurationErrorsException;
using NHibernateConfiguration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    /// <summary>
    /// The configuration builder for NHibernate's cfg.xml.
    /// </summary>
    public class NHibernateCfgXmlConfigurationBuilder : IConfigurationBuilder
    {
        /// <summary>
        /// Returns the NHibernate <see cref="NHibernateConfiguration" /> instance for the given XML.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="CastleConfiguration" />.</param>
        /// <returns>An NHibernate <see cref="NHibernateConfiguration" />.</returns>
        public NHibernateConfiguration GetConfiguration(CastleConfiguration facilityConfiguration)
        {
            const string FilePathAttributeName = Constants.SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName;

            var filePath = facilityConfiguration.Attributes[FilePathAttributeName];

            if (filePath.IsNullOrEmpty())
            {
                const string Message = $"'{FilePathAttributeName}' cannot be null or empty.";
                throw new ConfigurationErrorsException(Message);
            }

            using var configurationResource = new FileAssemblyResource(filePath);
            using var reader = XmlReader.Create(configurationResource.GetStreamReader());

            var configuration = new NHibernateConfiguration();

            configuration.Configure(reader);

            return configuration;
        }
    }
}
