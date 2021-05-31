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

using System.Collections.Generic;
using System.Text.RegularExpressions;

using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.NHibernateIntegration.Persisters;

using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    /// <summary>
    /// Serializes the NHibernate <see cref="Configuration" /> for subsequent initializations.
    /// </summary>
    public class PersistentConfigurationBuilder : DefaultConfigurationBuilder
    {
        public const string DefaultFileExtension = ".dat";

        private static readonly Regex _invalidFileNameCharsRegex =
            new(@"[:*?""<>\\/]",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly ILogger _logger = NullLogger.Instance;

        private readonly IConfigurationPersister _configurationPersister;

        public PersistentConfigurationBuilder() : this(new DefaultConfigurationPersister())
        {
        }

        public PersistentConfigurationBuilder(IConfigurationPersister configurationPersister)
        {
            _configurationPersister = configurationPersister;
        }

        /// <summary>
        /// Returns a deserialized NHibernate <see cref="Configuration" />.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>An NHibernate <see cref="Configuration" />.</returns>
        public override Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Building the NHibernate configuration.");
            }

            var fileName = GetFileNameFrom(facilityConfiguration);
            var dependentFileNames = GetDependentFileNamesFrom(facilityConfiguration);

            Configuration configuration;

            if (_configurationPersister.IsNewConfigurationRequired(fileName, dependentFileNames))
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug("Configuration is either old or some of the dependencies have changed.");
                }

                configuration = base.GetConfiguration(facilityConfiguration);
                _configurationPersister.WriteConfiguration(fileName, configuration);
            }
            else
            {
                configuration = _configurationPersister.ReadConfiguration(fileName);
            }

            return configuration;
        }

        private static string GetFileNameFrom(IConfiguration facilityConfiguration)
        {
            var fileName = facilityConfiguration.Attributes[Constants.SessionFactory_FileName_ConfigurationElementAttributeName] ??
                           $"{facilityConfiguration.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName]}{DefaultFileExtension}";
            return StripInvalidFileNameChars(fileName);
        }

        private static string StripInvalidFileNameChars(string input)
        {
            return _invalidFileNameCharsRegex.Replace(input, string.Empty);
        }

        private static List<string> GetDependentFileNamesFrom(IConfiguration facilityConfiguration)
        {
            List<string> list = new();

            var assemblies = facilityConfiguration.Children[Constants.SessionFactory_Assemblies_ConfigurationElementName];
            if (assemblies != null)
            {
                foreach (var assembly in assemblies.Children)
                {
                    list.Add($"{assembly.Value}.dll");
                }
            }

            var dependsOn = facilityConfiguration.Children[Constants.SessionFactory_DependsOn_ConfigurationElementName];
            if (dependsOn != null)
            {
                foreach (var fileName in dependsOn.Children)
                {
                    list.Add(fileName.Value);
                }
            }

            return list;
        }
    }
}
