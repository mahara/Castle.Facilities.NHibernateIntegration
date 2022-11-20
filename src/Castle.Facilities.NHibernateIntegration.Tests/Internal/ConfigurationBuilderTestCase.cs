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

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
    using Common;

    using NHibernate.Cfg;

    using NUnit.Framework;

    [TestFixture]
    public class ConfigurationBuilderTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFile =>
            "Internal/TwoDatabaseConfiguration.xml";

        [Test]
        public void SaveUpdateListenerAdded()
        {
            var configuration = Container.Resolve<Configuration>("sessionFactory4.cfg");

            Assert.That(configuration.EventListeners.SaveOrUpdateEventListeners, Has.Length.EqualTo(1));
            Assert.That(configuration.EventListeners.SaveOrUpdateEventListeners[0].GetType(), Is.EqualTo(typeof(CustomSaveUpdateListener)));

            Assert.That(configuration.EventListeners.DeleteEventListeners, Has.Length.EqualTo(1));
            Assert.That(configuration.EventListeners.DeleteEventListeners[0].GetType(), Is.EqualTo(typeof(CustomDeleteListener)));
        }
    }
}
