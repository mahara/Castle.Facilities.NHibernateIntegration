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

using Castle.Core.Resource;
using Castle.Facilities.AutoTx;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    public abstract class AbstractNHibernateTestCase
    {
        protected IWindsorContainer Container;

        protected virtual string ConfigurationFilePath =>
            "DefaultConfiguration.xml";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            //
            //  TODO:   Remove this workaround in future NUnit3TestAdapter version (4.x).
            //
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

            OnOneTimeSetUp();
        }

        protected virtual void OnOneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            OnOneTimeTearDown();
        }

        protected virtual void OnOneTimeTearDown()
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            TestHelper.AssertApplicationConfigurationFileExists();

            Container = new WindsorContainer(new XmlInterpreter(new AssemblyResource(GetContainerFilePath())));

            Container.AddFacility<AutoTxFacility>();

            ConfigureContainer();
            CreateDatabaseSchema();
            OnSetUp();
        }

        protected virtual void OnSetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
            OnTearDown();
            DropDatabaseSchema();

            Container.Dispose();
            Container = null;
        }

        protected virtual void OnTearDown()
        {
        }

        protected virtual void ConfigureContainer()
        {
        }

        protected string GetContainerFilePath()
        {
            return $"Castle.Facilities.NHibernateIntegration.Tests/{ConfigurationFilePath}";
        }

        protected virtual void CreateDatabaseSchema()
        {
            var configurations = Container.ResolveAll<Configuration>();

            foreach (var configuration in configurations)
            {
                var export = new SchemaExport(configuration);
                export.Create(false, true);
            }
        }

        protected virtual void DropDatabaseSchema()
        {
            var configurations = Container.ResolveAll<Configuration>();

            foreach (var configuration in configurations)
            {
                var export = new SchemaExport(configuration);
                export.Drop(false, true);
            }
        }
    }
}
