#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
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

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    using Castle.Facilities.AutoTx;

    using Core.Resource;

    using NHibernate.Cfg;
    using NHibernate.Tool.hbm2ddl;

    using NUnit.Framework;

    using Windsor;
    using Windsor.Configuration.Interpreters;

    public abstract class AbstractNHibernateTestCase
    {
        protected IWindsorContainer Container;

        public AbstractNHibernateTestCase()
        {
        }

        protected virtual string ConfigurationFile =>
            "DefaultConfiguration.xml";

        [SetUp]
        public virtual void SetUp()
        {
            Container = new WindsorContainer(new XmlInterpreter(new AssemblyResource(GetContainerFile())));
            Container.AddFacility<AutoTxFacility>();
            ConfigureContainer();
            CreateDatabaseSchemas();
            OnSetUp();
        }

        protected string GetContainerFile()
        {
            return "Castle.Facilities.NHibernateIntegration.Tests/" + ConfigurationFile;
        }

        protected virtual void ConfigureContainer()
        {
        }

        protected virtual void CreateDatabaseSchemas()
        {
            var cfgs = Container.ResolveAll<Configuration>();
            foreach (var cfg in cfgs)
            {
                var export = new SchemaExport(cfg);
                export.Create(false, true);
            }
        }

        protected virtual void OnSetUp()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
            OnTearDown();
            DropDatabaseSchemas();
            Container.Dispose();
            Container = null;
        }

        protected virtual void OnTearDown()
        {
        }

        protected virtual void DropDatabaseSchemas()
        {
            var cfgs = Container.ResolveAll<Configuration>();
            foreach (var cfg in cfgs)
            {
                var export = new SchemaExport(cfg);
                export.Drop(false, true);
            }
        }
    }
}
