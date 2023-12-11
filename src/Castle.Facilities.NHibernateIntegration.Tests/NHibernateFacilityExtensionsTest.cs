#region License
// Copyright (c) 2004-2023 Castle Project - https://www.castleproject.org/
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

using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Configuration;
using Castle.MicroKernel.Facilities;
using Castle.Windsor;

using Microsoft.Extensions.Configuration;

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    [TestFixture]
    public class NHibernateFacilityExtensionsTest : TestBase
    {
#pragma warning disable NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method
        private IWindsorContainer _container = null!;
#pragma warning restore NUnit1032 // An IDisposable field/property should be Disposed in a TearDown method

        protected override void OnSetUp()
        {
            _container = new WindsorContainer();

            var microsoftConfiguration = new ConfigurationBuilder()
                .SetBasePath(TestFixtureBaseFolderPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            _container.RegisterMicrosoftConfigurationMapper();

            _container.AddNHibernateFacility<NHibernateFacility>(
                microsoftConfiguration,
                f => f.ConfigurationBuilder<DefaultConfigurationBuilder>());
        }

        protected override void OnTearDown()
        {
            _container.Dispose();
            _container = null!;
        }



        [Test]
        public void ShouldResolveConfiguredComponents()
        {
            Assert.That(
                _container.Resolve<IMicrosoftConfigurationMapper>(),
                Is.TypeOf<DefaultMicrosoftConfigurationMapper>());

            Assert.That(
                _container.Resolve<IConfigurationBuilder>(),
                Is.TypeOf<DefaultConfigurationBuilder>());
        }

        [Test]
        public void ShouldResolveDefaultSessionFactory()
        {
            var sessionManager = _container.Resolve<ISessionManager>();

            using var _ = sessionManager.OpenSession();
        }

        [Test]
        public void ShouldResolveAliasedSessionFactory()
        {
            var sessionManager = _container.Resolve<ISessionManager>();

            using var _ = sessionManager.OpenSession("sessionFactory2");
        }

        [Test]
        public void ShouldNotResolveNonExistentSessionFactory()
        {
            var sessionManager = _container.Resolve<ISessionManager>();

            Assert.That(
                () => sessionManager.OpenSession("nonExistentSessionFactory"),
                Throws.TypeOf<FacilityException>()
                      .With.Message.EqualTo($"An '{nameof(ISessionFactory)}' component was not mapped for the specified alias: 'nonExistentSessionFactory'."));
        }
    }
}
