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
    using System;
    using System.Configuration;
    using System.IO;
    using System.Reflection;

    using Core.Configuration;

    using NHibernate.Event;

    using Configuration = NHibernate.Cfg.Configuration;

    /// <summary>
    /// Default imlementation of <see cref="IConfigurationBuilder" />.
    /// </summary>
    public class DefaultConfigurationBuilder : IConfigurationBuilder
    {
        private const string NHibernateMappingAttributesAssemblyName = "NHibernate.Mapping.Attributes";

        /// <inheritdoc />
        public virtual Configuration GetConfiguration(IConfiguration facilityConfiguration)
        {
            var configuration = new Configuration();

            ApplyConfigurationSettings(configuration, facilityConfiguration.Children["settings"]);
            RegisterAssemblies(configuration, facilityConfiguration.Children["assemblies"]);
            RegisterResources(configuration, facilityConfiguration.Children["resources"]);
            RegisterListeners(configuration, facilityConfiguration.Children["listeners"]);

            return configuration;
        }

        /// <summary>
        /// Applies the configuration settings.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        protected static void ApplyConfigurationSettings(Configuration configuration, IConfiguration facilityConfiguration)
        {
            if (facilityConfiguration == null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var key = item.Attributes["key"];
                var value = item.Value;

                configuration.SetProperty(key, value);
            }
        }

        /// <summary>
        /// Registers the resources.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        protected static void RegisterResources(Configuration configuration, IConfiguration facilityConfiguration)
        {
            if (facilityConfiguration == null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var name = item.Attributes["name"];
                var assemblyName = item.Attributes["assembly"];
                if (assemblyName != null)
                {
                    configuration.AddResource(name, LoadAssembly(assemblyName));
                }
                else
                {
                    configuration.AddXmlFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name));
                }
            }
        }

        /// <summary>
        /// Registers the listeners.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        protected static void RegisterListeners(Configuration configuration, IConfiguration facilityConfiguration)
        {
            if (facilityConfiguration == null)
            {
                return;
            }

            foreach (var item in facilityConfiguration.Children)
            {
                var eventName = item.Attributes["event"];
                var typeName = item.Attributes["type"];

                if (!Enum.IsDefined(typeof(ListenerType), eventName))
                {
                    throw new ConfigurationErrorsException("An invalid listener type was specified.");
                }

                var classType = Type.GetType(typeName);

                //if (classType == null)
                //    throw new ConfigurationErrorsException("The full type name of the listener class must be specified.");

                var listenerType = (ListenerType) Enum.Parse(typeof(ListenerType), eventName);
                var listenerInstance = Activator.CreateInstance(classType);

                configuration.SetListener(listenerType, listenerInstance);
            }
        }

        /// <summary>
        /// Registers the assemblies.
        /// </summary>
        /// <param name="configuration">The <see cref="Configuration" />.</param>
        /// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
        protected static void RegisterAssemblies(Configuration configuration, IConfiguration facilityConfiguration)
        {
            if (facilityConfiguration == null)
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
        /// then use the NHibernate mapping attributes contained in that assembly to update NHibernate configuration (<paramref name="configuration" />).
        /// Else, do nothing.
        /// </summary>
        /// <remarks>
        /// To avoid an unnecessary dependency on the library <c>NHibernate.Mapping.Attributes.dll</c>
        /// when using this facility without NHibernate mapping attributes,
        /// all calls to that library are made using reflection.
        /// </remarks>
        /// <param name="configuration">The NHibernate <see cref="Configuration" />.</param>
        /// <param name="targetAssemblyName">The target assembly name.</param>
        protected static void GenerateMappingFromAttributesIfNeeded(Configuration configuration, string targetAssemblyName)
        {
            // Get all assemblies referenced by targetAssembly.
            var referencedAssemblies = Assembly.Load(targetAssemblyName).GetReferencedAssemblies();

            // If assembly "NHibernate.Mapping.Attributes" is referenced in targetAssembly.
            if (Array.Exists(referencedAssemblies,
                             (AssemblyName x) => x.Name.Equals(NHibernateMappingAttributesAssemblyName)))
            {
                // Obtains, by reflection, the necessary tools to generate NHibernate mapping from attributes.
                var hbmSerializerType =
                    Type.GetType(string.Concat(NHibernateMappingAttributesAssemblyName,
                                               ".HbmSerializer, ",
                                               NHibernateMappingAttributesAssemblyName));
                var hbmSerializer = Activator.CreateInstance(hbmSerializerType);
                var validateProperty = hbmSerializerType.GetProperty("Validate");
                var serializeMethod = hbmSerializerType.GetMethod("Serialize", new[] { typeof(Assembly) });

                // Enable validation of mapping documents generated from the mapping attributes.
                validateProperty.SetValue(hbmSerializer, true, null);

                // Generates a stream of mapping documents from all decorated classes in targetAssembly
                // and add it to NHibernate configuration.
                configuration.AddInputStream(
                    (MemoryStream) serializeMethod.Invoke(hbmSerializer,
                                                          new object[] { Assembly.Load(targetAssemblyName) }));
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
                var message = string.Format("The assembly '{0}' could not be loaded.", assemblyName);

                throw new ConfigurationErrorsException(message, ex);
            }
        }
    }
}