#region License
// Copyright (c) 2004-2024 Castle Project - https://www.castleproject.org/
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
using System.Reflection;

using Castle.Services.Transaction.Utilities;

using NHibernate.Event;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using NHibernateConfiguration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration.Builders
{
    /// <summary>
    /// Default implementation of <see cref="IConfigurationBuilder" />
    /// that uses the facility <see cref="CastleConfiguration" />
    /// as the source of the NHibernate <see cref="NHibernateConfiguration" />.
    /// </summary>
    public class DefaultConfigurationBuilder : IConfigurationBuilder
    {
        private const string NHibernateMappingAttributesAssemblyName = "NHibernate.Mapping.Attributes";

        public virtual NHibernateConfiguration GetConfiguration(CastleConfiguration facilityConfiguration)
        {
            var configuration = new NHibernateConfiguration();

            ApplyConfigurationSettings(configuration, facilityConfiguration.Children[Constants.SessionFactory_Settings_ConfigurationElementName]);
            RegisterAssemblies(configuration, facilityConfiguration.Children[Constants.SessionFactory_Assemblies_ConfigurationElementName]);
            RegisterResources(configuration, facilityConfiguration.Children[Constants.SessionFactory_Resources_ConfigurationElementName]);
            RegisterListeners(configuration, facilityConfiguration.Children[Constants.SessionFactory_Listeners_ConfigurationElementName]);

            return configuration;
        }

        /// <summary>
        /// Applies the configuration settings.
        /// </summary>
        /// <param name="configuration">The NHibernate <see cref="NHibernateConfiguration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="CastleConfiguration" />.</param>
        protected static void ApplyConfigurationSettings(NHibernateConfiguration configuration, CastleConfiguration facilityConfiguration)
        {
            if (facilityConfiguration is null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var key = item.Attributes[Constants.SessionFactory_Settings_Key_ConfigurationElementAttributeName];
                var value = item.Value;

                configuration.SetProperty(key, value);
            }
        }

        /// <summary>
        /// Registers the assemblies.
        /// </summary>
        /// <param name="configuration">The NHibernate <see cref="NHibernateConfiguration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="CastleConfiguration" />.</param>
        protected static void RegisterAssemblies(NHibernateConfiguration configuration, CastleConfiguration facilityConfiguration)
        {
            if (facilityConfiguration is null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var assemblyName = item.Value;

                configuration.AddAssembly(assemblyName);

                GenerateMappingFromAttributesIfNeeded(configuration, assemblyName);
            }
        }

        /// <summary>
        /// If <paramref name="targetAssemblyName" /> has a reference on <c>NHibernate.Mapping.Attributes</c>,
        /// then use the NHibernate mapping attributes contained in that assembly to update NHibernate <see cref="NHibernateConfiguration" /> instance (<paramref name="configuration" />);
        /// otherwise, do nothing.
        /// </summary>
        /// <remarks>
        /// To avoid an unnecessary dependency on the library <c>NHibernate.Mapping.Attributes.dll</c>
        /// when using this facility without NHibernate mapping attributes,
        /// all calls to that library are made using reflection.
        /// </remarks>
        /// <param name="configuration">The NHibernate <see cref="NHibernateConfiguration" />.</param>
        /// <param name="targetAssemblyName">The target assembly name.</param>
        protected static void GenerateMappingFromAttributesIfNeeded(NHibernateConfiguration configuration, string targetAssemblyName)
        {
            // Get all assemblies referenced by targetAssembly.
            var referencedAssemblies = Assembly.Load(targetAssemblyName).GetReferencedAssemblies();

            // Check if "NHibernate.Mapping.Attributes" assembly is referenced in targetAssembly.
            if (Array.Exists(referencedAssemblies,
                             (AssemblyName assemblyName) =>
                             string.Equals(assemblyName.Name,
                                           NHibernateMappingAttributesAssemblyName,
                                           StringComparison.Ordinal)))
            {
                // Get the necessary tools to generate NHibernate mapping from attributes by using reflection.
                var hbmSerializerType =
                    Type.GetType(
                        string.Concat(NHibernateMappingAttributesAssemblyName,
                                      ".HbmSerializer, ",
                                      NHibernateMappingAttributesAssemblyName))!;
                var hbmSerializer = Activator.CreateInstance(hbmSerializerType);
                var validateProperty = hbmSerializerType.GetProperty("Validate")!;
                var serializeMethod = hbmSerializerType.GetMethod("Serialize", [typeof(Assembly)])!;

                // Enable validation of mapping documents generated from the mapping attributes.
                validateProperty.SetValue(hbmSerializer, true, null);

                // Generate a stream of mapping documents from all decorated classes in targetAssembly and add it to NHibernate configuration.
                configuration.AddInputStream(
                    (MemoryStream) serializeMethod.Invoke(
                        hbmSerializer,
                        [Assembly.Load(targetAssemblyName)])!);
            }
        }

        /// <summary>
        /// Registers the resources.
        /// </summary>
        /// <param name="configuration">The NHibernate <see cref="NHibernateConfiguration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="CastleConfiguration" />.</param>
        protected static void RegisterResources(NHibernateConfiguration configuration, CastleConfiguration facilityConfiguration)
        {
            if (facilityConfiguration is null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var name = item.Attributes[Constants.SessionFactory_Resources_Name_ConfigurationElementAttributeName];
                var assemblyName = item.Attributes[Constants.SessionFactory_Resources_Assembly_ConfigurationElementAttributeName];

                if (!assemblyName.IsNullOrEmpty())
                {
                    configuration.AddResource(name, LoadAssembly(assemblyName));
                }
                else
                {
                    configuration.AddXmlFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name!));
                }
            }
        }

        /// <summary>
        /// Registers the listeners.
        /// </summary>
        /// <param name="configuration">The NHibernate <see cref="NHibernateConfiguration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="CastleConfiguration" />.</param>
        protected static void RegisterListeners(NHibernateConfiguration configuration, CastleConfiguration facilityConfiguration)
        {
            if (facilityConfiguration is null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var listenerTypeEnumName = item.Attributes[Constants.SessionFactory_Listeners_Event_ConfigurationElementAttributeName];
                var listenerTypeFullName = item.Attributes[Constants.SessionFactory_Listeners_Type_ConfigurationElementAttributeName];

                if (!Enum.TryParse<ListenerType>(listenerTypeEnumName, out var listenerTypeEnum))
                {
                    var message = $"An invalid '{nameof(ListenerType)}' enum value '{listenerTypeEnumName}' was specified.";
                    throw new ConfigurationErrorsException(message);
                }

                if (listenerTypeFullName.IsNullOrEmpty())
                {
                    const string Message = "The listener type's full name must be specified.";
                    throw new ConfigurationErrorsException(Message);
                }

                Type listenerType = null!;

                try
                {
                    listenerType = Type.GetType(listenerTypeFullName)!;
                }
                catch (Exception ex)
                {
                    const string Message = "The listener type specified by its full name could not be resolved.";
                    throw new ConfigurationErrorsException(Message, ex);
                }

                var listener = Activator.CreateInstance(listenerType);

                configuration.SetListener(listenerTypeEnum, listener);
            }
        }

        private static Assembly LoadAssembly(string assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (Exception ex)
            {
                var message = $"The assembly '{assemblyName}' could not be loaded.";
                throw new ConfigurationErrorsException(message, ex);
            }
        }
    }
}
