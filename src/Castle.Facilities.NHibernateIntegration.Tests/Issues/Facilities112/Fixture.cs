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

using System.Reflection;

using Castle.Core;
using Castle.MicroKernel.Handlers;
using Castle.MicroKernel.Lifestyle;

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities112
{
    [TestFixture]
    [Explicit("Should be dropped, too much intrusion.")]
    public class LazyInitializationTestCase : IssueTestCase
    {
        protected override string ConfigurationFilePath =>
            "DefaultConfiguration.xml";

        [Test]
        [Ignore(@"'instance' field doesn't exist in 'SingletonLifestyleManager/AbstractLifestyleManager' class.")]
        public virtual void SessionFactoryIsLazilyInitialized()
        {
            var handler = Container.Kernel.GetHandler("sessionFactory1");

            var lifestyleManagerField =
                typeof(DefaultHandler).GetField(
                    "lifestyleManager",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.GetField);
            var instanceField =
                typeof(SingletonLifestyleManager).GetField(
                    "instance",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic |
                    BindingFlags.GetField);

            var lifeStyleManager = lifestyleManagerField!.GetValue(handler) as SingletonLifestyleManager;

            Assert.That(lifeStyleManager, Is.Not.Null);

            var instance = instanceField!.GetValue(lifeStyleManager);

            Assert.That(instance, Is.Null);

            Container.Resolve<ISessionFactory>();

            instance = instanceField.GetValue(lifeStyleManager);

            Assert.That(instance, Is.Not.Null);
        }

        [Test]
        public virtual void SessionFactoryIsSingleton()
        {
            var model = Container.Kernel.GetHandler("sessionFactory1").ComponentModel;

            Assert.That(model.LifestyleType, Is.EqualTo(LifestyleType.Singleton));
        }
    }
}
