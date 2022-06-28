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

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities112
{
    using System.Reflection;

    using Castle.Core;
    using Castle.MicroKernel.Handlers;
    using Castle.MicroKernel.Lifestyle;

    using NHibernate;

    using NUnit.Framework;

    [TestFixture]
    [Explicit("Should be dropped, too much intrusion.")]
    public class LazyInitializationTestCase : IssueTestCase
    {
        protected override string ConfigurationFile => "DefaultConfiguration.xml";

        [Test]
        public virtual void SessionFactoryIsSingleton()
        {
            var componentModel = Container.Kernel.GetHandler("sessionFactory1").ComponentModel;

            Assert.That(componentModel.LifestyleType, Is.EqualTo(LifestyleType.Singleton));
        }

        [Test]
        [Ignore(@"Missing ""instance"" field in ""SingletonLifestyleManager/AbstractLifestyleManager"" class.")]
        public virtual void SessionFactoryIsLazilyInitialized()
        {
            var handler = Container.Kernel.GetHandler("sessionFactory1");

            var lifestyleManagerField =
                typeof(DefaultHandler).GetField("lifestyleManager",
                                                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            var lifeStyleManager = lifestyleManagerField.GetValue(handler) as SingletonLifestyleManager;
            Assert.That(lifeStyleManager, Is.Not.Null);

            var instanceField =
                typeof(SingletonLifestyleManager).GetField("instance",
                                                           BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetField);
            var instance = instanceField.GetValue(lifeStyleManager);
            Assert.That(instance, Is.Null);

            Container.Resolve<ISessionFactory>();

            instance = instanceField.GetValue(lifeStyleManager);
            Assert.That(instance, Is.Not.Null);
        }
    }
}