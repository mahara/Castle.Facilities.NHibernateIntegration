#region License

//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

#endregion

namespace Castle.Facilities.NHibernateIntegration.Internal
{
	#region Using Directives

	using System;
	using System.Collections;
	using System.Collections.Specialized;

	using Castle.MicroKernel;
	using Castle.MicroKernel.Facilities;

	using NHibernate;

	#endregion

	/// <summary>
	///     Default implementation of <see cref="ISessionFactoryResolver" />
	///     that always queries the kernel instance for the session factory instance.
	///     <para>
	///         This gives a chance to developers replace the session factory instance
	///         during the application lifetime.
	///     </para>
	/// </summary>
	/// <remarks>
	///     Inspired on Cuyahoga project
	/// </remarks>
	public class SessionFactoryResolver : ISessionFactoryResolver
	{
		private readonly IDictionary _aliasToKey = new HybridDictionary(true);
		private readonly IKernel _kernel;

		/// <summary>
		///     Constructs a SessionFactoryResolver
		/// </summary>
		/// <param name="kernel">
		///     Kernel instance supplied by the container itself
		/// </param>
		public SessionFactoryResolver(IKernel kernel)
		{
			this._kernel = kernel;
		}

		/// <summary>
		///     Associated the alias with the component key
		/// </summary>
		/// <param name="alias">
		///     The alias associated with the session
		///     factory on the configuration node
		/// </param>
		/// <param name="componentKey">
		///     The component key associated with
		///     the session factory on the kernel
		/// </param>
		public void RegisterAliasComponentIdMapping(string alias, string componentKey)
		{
			if (this._aliasToKey.Contains(alias))
			{
				throw new ArgumentException($"A mapping already exists for the specified alias: {alias}");
			}

			this._aliasToKey.Add(alias, componentKey);
		}

		/// <summary>
		///     Returns a session factory instance associated with the
		///     specified alias.
		/// </summary>
		/// <param name="alias">
		///     The alias associated with the session
		///     factory on the configuration node
		/// </param>
		/// <returns>A session factory instance</returns>
		/// <exception cref="FacilityException">
		///     If the alias is not associated with a session factory
		/// </exception>
		public ISessionFactory GetSessionFactory(string alias)
		{
			var componentKey = this._aliasToKey[alias] as string;

			if (componentKey == null)
			{
				throw new FacilityException($"An ISessionFactory component was not mapped for the specified alias: {alias}");
			}

			return this._kernel.Resolve<ISessionFactory>(componentKey);
		}
	}
}