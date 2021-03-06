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

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
	#region Using Directives

	using System.Configuration;

	using Castle.Facilities.NHibernateIntegration.Builders;
	using Castle.Core.Configuration;
	using Castle.Core.Resource;
	using Castle.MicroKernel.Facilities;

	using NHibernate;

	using NUnit.Framework;

	using Castle.Windsor;
	using Castle.Windsor.Configuration.Interpreters;

	using Configuration = NHibernate.Cfg.Configuration;

	#endregion

	public class CustomConfigurationBuilder : IConfigurationBuilder
	{
		public int ConfigurationsCreated { get; private set; }

		#region IConfigurationBuilder Members

		public Configuration GetConfiguration(IConfiguration config)
		{
			this.ConfigurationsCreated++;

			var nhConfig = new DefaultConfigurationBuilder().GetConfiguration(config);
			nhConfig.Properties["dialect"] = ConfigurationManager.AppSettings["nhf.dialect"];
			nhConfig.Properties["connection.driver_class"] = ConfigurationManager.AppSettings["nhf.connection.driver_class"];
			nhConfig.Properties["connection.provider"] = ConfigurationManager.AppSettings["nhf.connection.provider"];
			nhConfig.Properties["connection.connection_string"] =
				ConfigurationManager.AppSettings["nhf.connection.connection_string.1"];
			if (config.Attributes["id"] != "sessionFactory1")
			{
				nhConfig.Properties["connection.connection_string"] =
					ConfigurationManager.AppSettings["nhf.connection.connection_string.2"];
			}

			return nhConfig;
		}

		#endregion
	}

	public class CustomNHibernateFacility : NHibernateFacility
	{
		public CustomNHibernateFacility()
			: base(new CustomConfigurationBuilder())
		{
		}
	}

	public abstract class AbstractCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
	{
		[Test]
		public void Invoked()
		{
			var session = this.container.Resolve<ISessionManager>().OpenSession();
			var configurationBuilder =
				(CustomConfigurationBuilder) this.container.Resolve<IConfigurationBuilder>();
			Assert.AreEqual(1, configurationBuilder.ConfigurationsCreated);
			session.Close();
		}
	}

	[TestFixture]
	public class CustomConfigurationBuilderTestCase : AbstractCustomConfigurationBuilderTestCase
	{
		protected override string ConfigurationFile => "customConfigurationBuilder.xml";
	}

	[TestFixture]
	public class CustomConfigurationBuilderRegressionTestCase : AbstractCustomConfigurationBuilderTestCase
	{
		protected override string ConfigurationFile => "configurationBuilderRegression.xml";
	}

	[TestFixture]
	public class InvalidCustomConfigurationBuilderTestCase : AbstractNHibernateTestCase
	{
		public override void SetUp()
		{
		}

		public override void TearDown()
		{
		}

		protected override string ConfigurationFile => "invalidConfigurationBuilder.xml";

		[Test]
		public void ThrowsWithMessage()
		{
			void Method()
			{
				this.container = new WindsorContainer(new XmlInterpreter(new AssemblyResource(this.GetContainerFile())));
			}

			Assert.That(Method, Throws.TypeOf<FacilityException>()
			                          .With.Message.EqualTo("ConfigurationBuilder type 'InvalidType' invalid or not found"));
		}
	}
}