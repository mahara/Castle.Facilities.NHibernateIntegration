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
        public void ExceptionOnEndWithTwoDatabases()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));
            var orders = orderDao.FindAll(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogitems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(0, blogs.Length);
            Assert.AreEqual(0, blogitems.Length);
            Assert.AreEqual(0, orders.Length);
        }

        [Test]
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create_Stateless(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogItems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogItems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(0, blogs.Length);
            Assert.AreEqual(0, blogItems.Length);
            Assert.AreEqual(0, orders.Length);
        }

        [Test]
        public void SuccessfulSituationWithTwoDatabases()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            service.DoTwoDBOperation_Create(false);

            var blogs = service.FindAll(typeof(Blog));
            var blogItems = service.FindAll(typeof(BlogItem));
            var orders = orderDao.FindAll(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogItems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, blogs.Length);
            Assert.AreEqual(1, blogItems.Length);
            Assert.AreEqual(1, orders.Length);
        }

        [Test]
        public void SuccessfulSituationWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService>();
            var orderDao = Container.Resolve<OrderDao>("myorderdao");

            service.DoTwoDBOperation_Create_Stateless(false);

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogItems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogItems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, blogs.Length);
            Assert.AreEqual(1, blogItems.Length);
            Assert.AreEqual(1, orders.Length);
        }
    }
}