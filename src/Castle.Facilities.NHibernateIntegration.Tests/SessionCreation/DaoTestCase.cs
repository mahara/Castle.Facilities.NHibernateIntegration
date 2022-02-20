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

using Castle.MicroKernel.Registration;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionCreation
{
    [TestFixture]
    public class DaoTestCase : AbstractNHibernateTestCase
    {
        protected override void ConfigureContainer()
        {
            Container.Register(Component.For<MyDao>().Named("mydao"));
            Container.Register(Component.For<MySecondDao>().Named("myseconddao"));
        }

        [Test]
        public void SessionIsShared()
        {
            var dao = Container.Resolve<MyDao>();

            dao.PerformComplexOperation();
        }

        [Test]
        public void SessionDisposedIsNotReused()
        {
            var dao = Container.Resolve<MyDao>();

            dao.PerformComplexOperation2();
        }

        [Test]
        public void ClosingAndDisposing()
        {
            var dao = Container.Resolve<MyDao>();

            dao.DoOpenCloseAndDispose();
        }

        [Test]
        public void StatelessSessionIsShared()
        {
            var dao = Container.Resolve<MyDao>();

            dao.PerformStatelessComplexOperation();
        }

        [Test]
        public void StatelessSessionDisposedIsNotReused()
        {
            var dao = Container.Resolve<MyDao>();

            dao.PerformStatelessComplexOperation2();
        }

        [Test]
        public void StatelessSessionClosingAndDisposing()
        {
            var dao = Container.Resolve<MyDao>();

            dao.DoStatelessOpenCloseAndDispose();
        }
    }
}
