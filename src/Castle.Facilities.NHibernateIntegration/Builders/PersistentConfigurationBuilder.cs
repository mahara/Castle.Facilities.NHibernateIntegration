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

using System.Text.RegularExpressions;

using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.NHibernateIntegration.Persisters;
using Castle.Services.Transaction.Utilities;

using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    /// <summary>
    /// Serializes the NHibernate <see cref="Configuration" /> instance for subsequent initializations.
    /// </summary>
    public class PersistentConfigurationBuilder : DefaultConfigurationBuilder
    {
        public const string DefaultFileExtension = ".dat";

        private static readonly Regex _invalidFileNameCharsRegex =
            new(@"[:*?""<>\\/]",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger _logger = NullLogger.Instance;

        private readonly IConfigurationPersister _configurationPersister;

        public PersistentConfigurationBuilder() :
            this(new DefaultConfigurationPersister())
        {
        }

        public PersistentConfigurationBuilder(IConfigurationPersister configurationPersister)
        {
            _configurationPersister = configurationPersister;
        }

        /// <summary>
        /// Returns a deserialized NHibernate <see cref="Configuration" /> instance.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>An NHibernate <see cref="Configuration" />.</returns>
        public override Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            _logger.Debug("Building NHibernate configuration.");

            var filePath = GetFilePathFrom(facilityConfiguration);
            var dependentFilePaths = GetDependentFilePathsFrom(facilityConfiguration);

            Configuration configuration;

            if (_configurationPersister.IsNewConfigurationRequired(filePath, dependentFilePaths))
            {
                _logger.Debug("NHibernate configuration is either old or some of the dependent files have changed.");

                configuration = base.GetConfiguration(facilityConfiguration);

                _configurationPersister.WriteConfiguration(filePath, configuration);
            }
            else
            {
                configuration = _configurationPersister.ReadConfiguration(filePath);
            }

            _logger.Debug("NHibernate configuration built.");

            return configuration;
        }

        private static string GetFilePathFrom(IConfiguration facilityConfiguration)
        {
            var fileName = facilityConfiguration.Attributes[Constants.SessionFactory_FileName_ConfigurationElementAttributeName];

            fileName = !fileName.IsNullOrEmpty() ?
                       fileName :
                       $"{facilityConfiguration.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName]}{DefaultFileExtension}";

            return StripInvalidFileNameChars(fileName);
        }

        private static List<string> GetDependentFilePathsFrom(IConfiguration facilityConfiguration)
        {
            List<string> list = [];

            var assemblies = facilityConfiguration.Children[Constants.SessionFactory_Assemblies_ConfigurationElementName];
            if (assemblies is not null)
            {
                foreach (var assembly in assemblies.Children)
                {
                    list.Add($"{assembly.Value}.dll");
                }
            }

            var dependsOn = facilityConfiguration.Children[Constants.SessionFactory_DependsOn_ConfigurationElementName];
            if (dependsOn is not null)
            {
                foreach (var fileName in dependsOn.Children)
                {
                    list.Add(fileName.Value);
                }
            }

            return list;
        }

        private static string StripInvalidFileNameChars(string fileName)
        {
            return _invalidFileNameCharsRegex.Replace(fileName, string.Empty);
        }
    }
}
