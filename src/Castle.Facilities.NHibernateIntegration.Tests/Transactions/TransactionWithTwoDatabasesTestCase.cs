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

using System;

using Castle.MicroKernel.Registration;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [TestFixture]
    public class TransactionWithTwoDatabasesTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFilePath => "Transactions/TwoDatabaseConfiguration.xml";

        protected override void ConfigureContainer()
        {
            Container.Register(Component.For<RootService>().Named("root"));
            Container.Register(Component.For<FirstDao>().Named("myfirstdao"));
            Container.Register(Component.For<SecondDao>().Named("myseconddao"));
            Container.Register(Component.For<OrderDao>().Named("myorderdao"));
        }

        [Test]
        public void SuccessfulSituationWithTwoDatabases()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            service.DoTwoDbOperation_Create(false);

            var blogs = service.FindAll<Blog>();
            var blogItems = service.FindAll<BlogItem>();
            var orders = orderDao.FindAll<Order>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(1));
            Assert.That(blogItems, Has.Count.EqualTo(1));
            Assert.That(orders, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExceptionOnEndWithTwoDatabases()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.DoTwoDbOperation_Create(true);
            }
            catch (InvalidOperationException)
            {
                // Expected.
            }

            var blogs = service.FindAll<Blog>();
            var blogItems = service.FindAll<BlogItem>();
            var orders = orderDao.FindAll<Order>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(0));
            Assert.That(blogItems, Has.Count.EqualTo(0));
            Assert.That(orders, Has.Count.EqualTo(0));
        }

        [Test]
        public void SuccessfulSituationWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            service.DoTwoDbOperation_CreateStateless(false);

            var blogs = service.FindAllStateless<Blog>();
            var blogItems = service.FindAllStateless<BlogItem>();
            var orders = orderDao.FindAllStateless<Order>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(1));
            Assert.That(blogItems, Has.Count.EqualTo(1));
            Assert.That(orders, Has.Count.EqualTo(1));
        }

        [Test]
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.DoTwoDbOperation_CreateStateless(true);
            }
            catch (InvalidOperationException)
            {
                // Expected.
            }

            var blogs = service.FindAllStateless<Blog>();
            var blogItems = service.FindAllStateless<BlogItem>();
            var orders = orderDao.FindAllStateless<Order>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(0));
            Assert.That(blogItems, Has.Count.EqualTo(0));
            Assert.That(orders, Has.Count.EqualTo(0));
        }
    }
}
