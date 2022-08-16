#region License
// Copyright (c) 2004-2022 Castle Project - https://www.castleproject.org/
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

using System.Configuration;

using Castle.Core.Configuration;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.SubSystems.Conversion;

using Microsoft.Extensions.Configuration;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using MicrosoftConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Castle.Facilities.NHibernateIntegration.Configuration
{
    public interface IMicrosoftConfigurationMapper
    {
        Type GetFacilityType(CastleConfiguration configuration);

        Type GetFacilityType(MicrosoftConfiguration configuration);

        CastleConfiguration Map(MicrosoftConfiguration configuration);
    }

    public class DefaultMicrosoftConfigurationMapper : IMicrosoftConfigurationMapper
    {
        private readonly IKernel _kernel;

        public DefaultMicrosoftConfigurationMapper(IKernel kernel)
        {
            _kernel = kernel;
        }

        public Type GetFacilityType(CastleConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var facilityTypeFullName =
                configuration.Attributes[Constants.FacilityType_ConfigurationElementAttributeName];

            if (string.IsNullOrEmpty(facilityTypeFullName))
            {
                const string Message = $"The '{Constants.FacilityType_ConfigurationElementAttributeName}' attribute is required.";
                throw new ConfigurationErrorsException(Message);
            }

            try
            {
                var converter = _kernel.GetConversionManager();

                var facilityType = converter.PerformConversion<Type>(facilityTypeFullName);

                return facilityType;
            }
            catch (ConverterException ex)
            {
                var message = $"The type '{facilityTypeFullName}' specified in the '{Constants.FacilityType_ConfigurationElementAttributeName}' attribute could not be resolved.";
                throw new FacilityException(message, ex);
            }
        }

        public Type GetFacilityType(MicrosoftConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection =
                configuration.GetSection(Constants.NHibernateFacility_ConfigurationSectionName);

            if (!configurationSection.Exists())
            {
                const string Message = $"The '{Constants.NHibernateFacility_ConfigurationSectionName}' section is required.";
                throw new ConfigurationErrorsException(Message);
            }

            var facilityTypeFullName =
                configurationSection[Constants.FacilityType_ConfigurationSectionName];

            if (string.IsNullOrEmpty(facilityTypeFullName))
            {
                const string Message = $"The '{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}' section is required.";
                throw new ConfigurationErrorsException(Message);
            }

            try
            {
                var converter = _kernel.GetConversionManager();

                var facilityType = converter.PerformConversion<Type>(facilityTypeFullName);

                return facilityType;
            }
            catch (ConverterException ex)
            {
                var message = $"The type '{facilityTypeFullName}' specified in the '{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}' section could not be resolved.";
                throw new FacilityException(message, ex);
            }
        }

        public CastleConfiguration Map(MicrosoftConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var configurationSection =
                configuration.GetSection(Constants.NHibernateFacility_ConfigurationSectionName);

            if (!configurationSection.Exists())
            {
                const string Message = $"The '{Constants.NHibernateFacility_ConfigurationSectionName}' section is required.";
                throw new ConfigurationErrorsException(Message);
            }

            var configurationNode =
                new MutableConfiguration(Constants.Facility_ConfigurationElementName);

            var facilityTypeFullName =
                configurationSection[Constants.FacilityType_ConfigurationSectionName];

            if (string.IsNullOrEmpty(facilityTypeFullName))
            {
                const string Message = $"The '{Constants.NHibernateFacility_ConfigurationSectionName}:{Constants.FacilityType_ConfigurationSectionName}' section is required.";
                throw new ConfigurationErrorsException(Message);
            }

            configurationNode.Attributes[Constants.FacilityType_ConfigurationElementAttributeName] =
                facilityTypeFullName;

            configurationNode.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName] =
                configurationSection[Constants.ConfigurationBuilderType_ConfigurationSectionName];

            configurationNode.Attributes[Constants.SessionStoreType_ConfigurationElementAttributeName] =
                configurationSection[Constants.SessionStoreType_ConfigurationSectionName];

            configurationNode.Attributes[Constants.SessionStore_IsWeb_ConfigurationElementAttributeName] =
                configurationSection[Constants.SessionStore_IsWeb_ConfigurationSectionName];

            configurationNode.Attributes[Constants.Session_DefaultFlushMode_ConfigurationElementAttributeName] =
                configurationSection[Constants.Session_DefaultFlushMode_ConfigurationSectionName];

            configurationNode.Attributes[Constants.UseReflectionOptimizer_ConfigurationElementAttributeName] =
                configurationSection[Constants.UseReflectionOptimizer_ConfigurationSectionName];

            var sessionFactoriesConfigurationSection =
                configurationSection.GetSection(Constants.SessionFactories_ConfigurationSectionName);

            if (sessionFactoriesConfigurationSection is null)
            {
                const string Message = $"The '{Constants.SessionFactories_ConfigurationSectionName}' section is required.";
                throw new ConfigurationErrorsException(Message);
            }

            var sessionFactoryConfigurationSections = sessionFactoriesConfigurationSection.GetChildren();

            if (!sessionFactoryConfigurationSections.Any())
            {
                const string Message = $"At least one '{Constants.SessionFactory_ConfigurationSectionName}' section children are required.";
                throw new ConfigurationErrorsException(Message);
            }

            foreach (var sessionFactoryConfigurationSection in sessionFactoryConfigurationSections)
            {
                var sessionFactoryNode =
                    new MutableConfiguration(Constants.SessionFactory_ConfigurationElementName);

                if (string.IsNullOrEmpty(sessionFactoryConfigurationSection.Key))
                {
                    const string Message = $"Each section within the '{Constants.SessionFactories_ConfigurationSectionName}' section requires a key.";
                    throw new ConfigurationErrorsException(Message);
                }

                sessionFactoryNode.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName] =
                    sessionFactoryConfigurationSection.Key;

                sessionFactoryNode.Attributes[Constants.SessionFactory_Alias_ConfigurationElementAttributeName] =
                    sessionFactoryConfigurationSection[Constants.SessionFactory_Alias_ConfigurationSectionName];

                sessionFactoryNode.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName] =
                    sessionFactoryConfigurationSection[Constants.ConfigurationBuilderType_ConfigurationSectionName];

                var settingsConfigurationSection =
                    sessionFactoryConfigurationSection.GetSection(Constants.SessionFactory_Settings_ConfigurationSectionName);

                if (settingsConfigurationSection is null)
                {
                    const string Message = $"The '{Constants.SessionFactory_Settings_ConfigurationSectionName}' section is required.";
                    throw new ConfigurationErrorsException(Message);
                }

                var settingConfigurationSections = settingsConfigurationSection.GetChildren();

                if (!settingConfigurationSections.Any())
                {
                    const string Message = $"At least one section within the '{Constants.SessionFactory_Settings_ConfigurationSectionName}' section is required.";
                    throw new ConfigurationErrorsException(Message);
                }

                var settings = new Dictionary<string, string?>();

                foreach (var settingConfigurationSection in settingConfigurationSections)
                {
                    settings[settingConfigurationSection.Key] = settingConfigurationSection.Value;
                }

                if (settings.TryGetValue(
                        NHibernate.Cfg.Environment.ConnectionStringName,
                        out var connectionStringName) &&
                    !string.IsNullOrEmpty(connectionStringName))
                {
                    settings[NHibernate.Cfg.Environment.ConnectionString] =
                        configuration.GetConnectionString(connectionStringName);
                }

                var settingsNode =
                    new MutableConfiguration(Constants.SessionFactory_Settings_ConfigurationElementName);

                foreach (var setting in settings)
                {
                    var itemNode =
                        new MutableConfiguration(Constants.SessionFactory_Settings_Item_ConfigurationElementAttributeName);

                    itemNode.Attributes[Constants.SessionFactory_Settings_Key_ConfigurationElementAttributeName] =
                        setting.Key;
                    itemNode.Value =
                        setting.Value;

                    settingsNode.Children.Add(itemNode);
                }

                sessionFactoryNode.Children.Add(settingsNode);

                configurationNode.Children.Add(sessionFactoryNode);
            }

            return configurationNode;
        }
    }
}
