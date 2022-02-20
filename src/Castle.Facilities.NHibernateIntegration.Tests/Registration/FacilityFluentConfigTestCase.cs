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

namespace Castle.Facilities.NHibernateIntegration.Tests.Registration
{
	using Castle.Core.Configuration;
	using Castle.Core.Resource;
	using Castle.Facilities.AutoTx;
	using Castle.Facilities.NHibernateIntegration.SessionStores;
	using Castle.MicroKernel.Facilities;
	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;

	using NHibernate.Cfg;

	using NUnit.Framework;

	[TestFixture]
	public class FacilityFluentConfigTestCase
	{
		[Test]
		public void Should_be_able_to_revolve_ISessionManager_when_fluently_configured()
		{
			var container = new WindsorContainer();

			container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder<TestConfigurationBuilder>());

			var sessionManager = container.Resolve<ISessionManager>();
			sessionManager.OpenSession();
			Assert.AreEqual(typeof(TestConfigurationBuilder), container.Resolve<IConfigurationBuilder>().GetType());
		}

		[Test]
		public void Should_not_accept_non_implementors_of_IConfigurationBuilder_for_override()
		{
			void Method()
			{
				var container = new WindsorContainer();

				container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder(GetType()));
			}

			Assert.That(Method, Throws.TypeOf<FacilityException>());
		}

		[Test]
		[Ignore("TODO: .NET Core Migration Issue")]
		public void Should_override_DefaultConfigurationBuilder()
		{
			var file = "Castle.Facilities.NHibernateIntegration.Tests/MinimalConfiguration.xml";

			var container = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file)));
			container.AddFacility<AutoTxFacility>();

			container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

			Assert.AreEqual(typeof(DummyConfigurationBuilder), container.Resolve<IConfigurationBuilder>().GetType());
		}

		[Test]
		[Ignore("TODO: .NET Core Migration Issue")]
		public void Should_override_IsWeb()
		{
			var file = "Castle.Facilities.NHibernateIntegration.Tests/MinimalConfiguration.xml";

			var container = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file)));
			container.AddFacility<AutoTxFacility>();

			container.AddFacility<NHibernateFacility>(f => f.IsWeb().ConfigurationBuilder<DummyConfigurationBuilder>());

			var sessionStore = container.Resolve<ISessionStore>();

			Assert.IsInstanceOf(typeof(CallContextSessionStore), sessionStore);
		}

		[Test]
		public void ShouldOverrideDefaultSessionStore()
		{
			var container = new WindsorContainer();
			container.AddFacility<AutoTxFacility>();

			container.AddFacility<NHibernateFacility>(
				f => f.IsWeb()
					  .SessionStore<CallContextSessionStore>()
					  .ConfigurationBuilder<DummyConfigurationBuilder>());

			var sessionStore = container.Resolve<ISessionStore>();

			Assert.IsInstanceOf(typeof(CallContextSessionStore), sessionStore);
		}

		[Test]
		public void ShouldUseDefaultSessionStore()
		{
			var container = new WindsorContainer();
			container.AddFacility<AutoTxFacility>();

			container.AddFacility<NHibernateFacility>(
				f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

			var sessionStore = container.Resolve<ISessionStore>();

			Assert.IsInstanceOf(typeof(LogicalCallContextSessionStore), sessionStore);
		}
	}

	internal class DummyConfigurationBuilder : IConfigurationBuilder
	{
		public Configuration GetConfiguration(IConfiguration config)
		{
			return new Configuration();
		}
	}
}