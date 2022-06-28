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

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities117
{
    using System.Collections.Generic;

    using Builders;

    using Castle.MicroKernel;

    using Core.Configuration;
    using Core.Resource;

    using MicroKernel.SubSystems.Configuration;

    using Moq;

    using NUnit.Framework;

    using Windsor.Configuration.Interpreters;

    [TestFixture]
    public class PersistentConfigurationBuilderConventionsFixture
    {
        private IConfiguration _facilityConfiguration;

        [SetUp]
        public void SetUp()
        {
            var configurationStore = new DefaultConfigurationStore();
            var resource = new AssemblyResource("Castle.Facilities.NHibernateIntegration.Tests/Issues/Facilities117/facility.xml");
            var xmlInterpreter = new XmlInterpreter(resource);
            xmlInterpreter.ProcessResource(resource, configurationStore, new DefaultKernel());
            _facilityConfiguration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName).Children["factory"];
        }

        [Test]
        public void DerivesValidFilenameFromSessionFactoryIdWhenNotExplicitlySpecified()
        {
            var configurationPersister = new Mock<IConfigurationPersister>().Object;
            Mock.Get(configurationPersister)
                .Setup(x =>
                       x.IsNewConfigurationRequired(It.Is<string>(x => x.Equals("sessionFactory1.dat")), It.IsAny<IList<string>>()))
                .Returns(false);

            var builder = new PersistentConfigurationBuilder(configurationPersister);
            builder.GetConfiguration(_facilityConfiguration);

            Mock.Get(configurationPersister).VerifyAll();
        }

        [Test]
        public void IncludesMappingAssembliesInDependentFileList()
        {
            var list = new List<string> { "Castle.Facilities.NHibernateIntegration.Tests.dll" };

            var configurationPersister = new Mock<IConfigurationPersister>().Object;
            Mock.Get(configurationPersister)
                .Setup(x =>
                       x.IsNewConfigurationRequired(It.IsAny<string>(),
                                                    It.Is<IList<string>>(x => x.Contains(list[0]))))
                .Returns(false);

            var builder = new PersistentConfigurationBuilder(configurationPersister);
            builder.GetConfiguration(_facilityConfiguration);

            Mock.Get(configurationPersister).VerifyAll();
        }
    }
}