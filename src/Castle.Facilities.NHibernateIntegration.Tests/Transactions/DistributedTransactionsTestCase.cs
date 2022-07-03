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

    using MicroKernel.Registration;

    using NUnit.Framework;

    using Services.Transaction;

    [TestFixture]
    public class DistributedTransactionsTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFile =>
            "Transactions/TwoDatabaseConfiguration.xml";

        protected override void ConfigureContainer()
        {
            Container.Register(Component.For<RootService2>().Named("root"));
            Container.Register(Component.For<FirstDao2>().Named("myfirstdao"));
            Container.Register(Component.For<SecondDao2>().Named("myseconddao"));
            Container.Register(Component.For<OrderDao2>().Named("myorderdao"));
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void SuccessfulSituationWithTwoDatabases()
        {
            var service = Container.Resolve<RootService2>();
            var orderDao = Container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.TwoDbOperationCreate(false);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null
                    && ex.InnerException.GetType().Name == "TransactionManagerCommunicationException")
                {
                    Assert.Ignore("MTS is not available");
                }

                throw;
            }

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));
            var orders = orderDao.FindAll(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogitems, Has.Length.EqualTo(1));
            Assert.That(orders, Has.Length.EqualTo(1));
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void ExceptionOnEndWithTwoDatabases()
        {
            var service = Container.Resolve<RootService2>();
            var orderDao = Container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.TwoDbOperationCreate(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
            catch (RollbackResourceException e)
            {
                foreach (var resource in e.FailedResources)
                {
                    Console.WriteLine(resource.Second);
                }

                throw;
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
        [Explicit("Requires MSDTC to be running.")]
        public void SuccessfulSituationWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService2>();
            var orderDao = Container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.TwoDbOperationCreateStateless(false);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null
                    && ex.InnerException.GetType().Name == "TransactionManagerCommunicationException")
                {
                    Assert.Ignore("MTS is not available");
                }

                throw;
            }

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogitems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogitems, Has.Length.EqualTo(1));
            Assert.That(orders, Has.Length.EqualTo(1));
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        [Ignore("TODO: Fix failed test.")]
        // System.Data.SqlClient.SqlException : Distributed transaction completed. Either enlist this session in a new transaction or the NULL transaction.
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService2>();
            var orderDao = Container.Resolve<OrderDao2>("myorderdao");

            try
            {
                //service.TwoDbOperationCreate(true);
                service.TwoDbOperationCreateStateless(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
            catch (RollbackResourceException e)
            {
                foreach (var resource in e.FailedResources)
                {
                    Console.WriteLine(resource.Second);
                }

                throw;
            }

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogitems = service.FindAllStateless(typeof(BlogItem));
            var orders = orderDao.FindAllStateless(typeof(Order));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(orders, Is.Not.Null);
            Assert.That(blogs.Length, Is.EqualTo(0));
            Assert.That(blogitems.Length, Is.EqualTo(0));
            Assert.That(orders.Length, Is.EqualTo(0));
        }
    }
}