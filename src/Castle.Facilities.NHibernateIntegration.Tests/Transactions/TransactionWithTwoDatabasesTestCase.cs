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

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    using System;

    using Castle.MicroKernel.Registration;

    using NUnit.Framework;

    [TestFixture]
    public class TransactionWithTwoDatabasesTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFile =>
            "Transactions/TwoDatabaseConfiguration.xml";

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

            service.TwoDbOperationCreate(false);

            var blogs = service.FindAll(typeof(Blog));
            var blogItems = service.FindAll(typeof(BlogItem));
            var orders = orderDao.FindAll(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogItems, Has.Length.EqualTo(1));
            Assert.That(orders, Has.Length.EqualTo(1));
        }
        [Test]
        public void ExceptionOnEndWithTwoDatabases()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.TwoDbOperationCreate(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));
            var orders = orderDao.FindAll(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs.Length, Is.EqualTo(0));
            Assert.That(blogitems.Length, Is.EqualTo(0));
            Assert.That(orders.Length, Is.EqualTo(0));
        }


        [Test]
        public void SuccessfulSituationWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            service.TwoDbOperationCreateStateless(false);

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogItems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogItems, Has.Length.EqualTo(1));
            Assert.That(orders, Has.Length.EqualTo(1));
        }

        [Test]
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.TwoDbOperationCreateStateless(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogItems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs.Length, Is.EqualTo(0));
            Assert.That(blogItems.Length, Is.EqualTo(0));
            Assert.That(orders.Length, Is.EqualTo(0));
        }
    }
}
