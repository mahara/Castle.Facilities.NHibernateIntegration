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
	using Core.Configuration;

	using NHibernate.Event;

	using System;
	using System.Configuration;
	using System.IO;
	using System.Reflection;

	using Configuration = NHibernate.Cfg.Configuration;

	/// <summary>
	/// Default imlementation of <see cref="IConfigurationBuilder" />.
	/// </summary>
	public class DefaultConfigurationBuilder : IConfigurationBuilder
	{
		private const string NHMappingAttributesAssemblyName = "NHibernate.Mapping.Attributes";

		/// <summary>
		/// Builds the <see cref="Configuration" /> object from the specified facility <see cref="IConfiguration" />.
		/// </summary>
		/// <param name="facilityConfiguration">The facility <see cref="IConfiguration" />.</param>
		/// <returns>The <see cref="Configuration" />.</returns>
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
		protected void ApplyConfigurationSettings(Configuration configuration, IConfiguration facilityConfiguration)
		{
			if (facilityConfiguration == null) return;

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
		protected void RegisterResources(Configuration configuration, IConfiguration facilityConfiguration)
		{
			if (facilityConfiguration == null) return;

			foreach (var item in facilityConfiguration.Children)
			{
				var name = item.Attributes["name"];
				var assembly = item.Attributes["assembly"];

				if (assembly != null)
				{
					configuration.AddResource(name, ObtainAssembly(assembly));
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
		protected void RegisterListeners(Configuration configuration, IConfiguration facilityConfiguration)
		{
			if (facilityConfiguration == null) return;

			foreach (var item in facilityConfiguration.Children)
			{
				var eventName = item.Attributes["event"];
				var typeName = item.Attributes["type"];

				if (!Enum.IsDefined(typeof(ListenerType), eventName))
					throw new ConfigurationErrorsException("An invalid listener type was specified.");

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
		protected void RegisterAssemblies(Configuration configuration, IConfiguration facilityConfiguration)
		{
			if (facilityConfiguration == null) return;

			foreach (var item in facilityConfiguration.Children)
			{
				var assembly = item.Value;

				configuration.AddAssembly(assembly);

				GenerateMappingFromAttributesIfNeeded(configuration, assembly);
			}
		}

		/// <summary>
		/// If <paramref name="targetAssembly" /> has a reference on <c>NHibernate.Mapping.Attributes</c>,
		/// then use the NHibernate mapping attributes contained in that assembly to update NHibernate configuration (<paramref name="configuration" />).
		/// Else do nothing.
		/// </summary>
		/// <remarks>
		/// To avoid an unnecessary dependency on the library <c>NHibernate.Mapping.Attributes.dll</c>
		/// when using this facility without NHibernate mapping attributes,
		/// all calls to that library are made using reflection.
		/// </remarks>
		/// <param name="configuration">The <see cref="Configuration" />.</param>
		/// <param name="targetAssembly">The target assembly name.</param>
		protected void GenerateMappingFromAttributesIfNeeded(Configuration configuration, string targetAssembly)
		{
			//Get an array of all assemblies referenced by targetAssembly
			var refAssemblies = Assembly.Load(targetAssembly).GetReferencedAssemblies();

			//If assembly "NHibernate.Mapping.Attributes" is referenced in targetAssembly
			if (Array.Exists(refAssemblies, delegate (AssemblyName an) { return an.Name.Equals(NHMappingAttributesAssemblyName); }))
			{
				//Obtains, by reflexion, the necessary tools to generate NH mapping from attributes
				var HbmSerializerType =
					Type.GetType(string.Concat(NHMappingAttributesAssemblyName, ".HbmSerializer, ", NHMappingAttributesAssemblyName));
				var hbmSerializer = Activator.CreateInstance(HbmSerializerType);
				var validate = HbmSerializerType.GetProperty("Validate");
				var serialize = HbmSerializerType.GetMethod("Serialize", new[] { typeof(Assembly) });

				//Enable validation of mapping documents generated from the mapping attributes
				validate.SetValue(hbmSerializer, true, null);

				//Generates a stream of mapping documents from all decorated classes in targetAssembly and add it to NH config
				configuration.AddInputStream((MemoryStream) serialize.Invoke(hbmSerializer, new object[] { Assembly.Load(targetAssembly) }));
			}
		}

		private Assembly ObtainAssembly(string assembly)
		{
			try
			{
				return Assembly.Load(assembly);
			}
			catch (Exception ex)
			{
				var message = string.Format("The assembly {0} could not be loaded.", assembly);

				throw new ConfigurationErrorsException(message, ex);
			}
		}
	}
}