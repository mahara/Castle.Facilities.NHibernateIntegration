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
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Castle.Core.Logging;

    using Core.Configuration;

    using NHibernate.Cfg;

    using Persisters;

    /// <summary>
    /// Serializes the <see cref="Configuration" /> for subsequent initializations.
    /// </summary>
    public class PersistentConfigurationBuilder : DefaultConfigurationBuilder
    {
        private const string DEFAULT_EXTENSION = ".dat";

        private readonly ILogger _logger = NullLogger.Instance;

        private readonly IConfigurationPersister _configurationPersister;

        /// <summary>
        /// Initializes the persistent <see cref="Configuration" /> builder
        /// with an specific <see cref="IConfigurationPersister" />
        /// </summary>
        public PersistentConfigurationBuilder(IConfigurationPersister configurationPersister)
        {
            _configurationPersister = configurationPersister;
        }

        /// <summary>
        /// Initializes the persistent <see cref="Configuration" /> builder
        /// using the default <see cref="IConfigurationPersister" />
        /// </summary>
        public PersistentConfigurationBuilder()
            : this(new DefaultConfigurationPersister())
        {
        }

        /// <summary>
        /// Returns the Deserialized Configuration.
        /// </summary>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="Configuration" />.</returns>
        public override Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            if (_logger.IsDebugEnabled)
            {
                _logger.Debug("Building the Configuration.");
            }

            var filename = GetFilenameFrom(facilityConfiguration);
            var dependentFilenames = GetDependentFilenamesFrom(facilityConfiguration);

            Configuration configuration;
            if (_configurationPersister.IsNewConfigurationRequired(filename, dependentFilenames))
            {
                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug("Configuration is either old or some of the dependencies have changed.");
                }

                configuration = base.GetConfiguration(facilityConfiguration);
                _configurationPersister.WriteConfiguration(filename, configuration);
            }
            else
            {
                configuration = _configurationPersister.ReadConfiguration(filename);
            }

            return configuration;
        }

        private string GetFilenameFrom(IConfiguration configuration)
        {
            var filename = configuration.Attributes["fileName"]
                           ?? configuration.Attributes["id"] + DEFAULT_EXTENSION;

            return StripInvalidCharacters(filename);
        }

        private string StripInvalidCharacters(string input)
        {
            return Regex.Replace(input, "[:*?\"<>\\\\/]", "", RegexOptions.IgnoreCase);
        }

        private IList<string> GetDependentFilenamesFrom(IConfiguration configuration)
        {
            var list = new List<string>();

            var assemblies = configuration.Children["assemblies"];
            if (assemblies != null)
            {
                foreach (var assembly in assemblies.Children)
                {
                    list.Add(assembly.Value + ".dll");
                }
            }

            var dependsOn = configuration.Children["dependsOn"];
            if (dependsOn != null)
            {
                foreach (var on in dependsOn.Children)
                {
                    list.Add(on.Value);
                }
            }

            return list;
        }
    }
}