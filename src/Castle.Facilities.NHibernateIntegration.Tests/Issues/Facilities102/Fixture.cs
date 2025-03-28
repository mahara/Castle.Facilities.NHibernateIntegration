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

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities102
{
    [TestFixture]
    public class Fixture : IssueTestCase
    {
        [Test]
        public void HasAliassedSessionHasFlushModeSet()
        {
            var manager = Container.Resolve<ISessionManager>();
            var previousDefaultFlushMode = manager.DefaultFlushMode;

            manager.DefaultFlushMode = (FlushMode) 100;

            var session = manager.OpenSession("intercepted");

            Assert.That(session.FlushMode, Is.EqualTo(manager.DefaultFlushMode));

            manager.DefaultFlushMode = previousDefaultFlushMode;
        }

        [Test]
        public void SessionHasFlushModeSet()
        {
            var manager = Container.Resolve<ISessionManager>();
            var previousDefaultFlushMode = manager.DefaultFlushMode;

            manager.DefaultFlushMode = (FlushMode) 100;

            var session = manager.OpenSession();

            Assert.That(session.FlushMode, Is.EqualTo(manager.DefaultFlushMode));

            manager.DefaultFlushMode = previousDefaultFlushMode;
        }
    }
}
