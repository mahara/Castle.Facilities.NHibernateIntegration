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

using Castle.Core.Configuration;
using Castle.Core.Resource;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.MicroKernel;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor.Configuration.Interpreters;

using NUnit.Framework;

using Rhino.Mocks;

using Is = Rhino.Mocks.Constraints.Is;
using List = Rhino.Mocks.Constraints.List;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities117
{
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
            _facilityConfiguration = configurationStore.GetFacilityConfiguration(typeof(NHibernateFacility).FullName).Children[Constants.SessionFactory_ConfigurationElementName];
        }

        [Test]
        public void DerivesValidFilenameFromSessionFactoryIdWhenNotExplicitlySpecified()
        {
            var configurationPersister = MockRepository.GenerateMock<IConfigurationPersister>();
            configurationPersister.Expect(
                x =>
                x.IsNewConfigurationRequired(null, null))
                 .IgnoreArguments()
                 .Constraints(Is.Equal("sessionFactory1.dat"), Is.Anything())
                 .Return(false);

            var builder = new PersistentConfigurationBuilder(configurationPersister);
            builder.GetConfiguration(_facilityConfiguration);

            configurationPersister.VerifyAllExpectations();
        }

        [Test]
        public void IncludesMappingAssembliesInDependentFiles()
        {
            var dependentFileNames = new[]
            {
                "Castle.Facilities.NHibernateIntegration.Tests.dll",
            };

            var configurationPersister = MockRepository.GenerateMock<IConfigurationPersister>();
            configurationPersister.Expect(
                x =>
                x.IsNewConfigurationRequired(null, null))
                 .IgnoreArguments()
                 .Constraints(Is.Anything(),
                              List.ContainsAll(dependentFileNames))
                 .Return(false);

            var builder = new PersistentConfigurationBuilder(configurationPersister);
            builder.GetConfiguration(_facilityConfiguration);

            configurationPersister.VerifyAllExpectations();
        }
    }
}
