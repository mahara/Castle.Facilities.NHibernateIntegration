#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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
using Castle.Services.Transaction;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [TestFixture]
    public class DistributedTransactionsTestCase : AbstractNHibernateTestCase
    {
        protected override string ConfigurationFile
        {
            get { return "Transactions/TwoDatabaseConfiguration.xml"; }
        }

        protected override void ConfigureContainer()
        {
            container.Register(Component.For<RootService2>().Named("root"));
            container.Register(Component.For<FirstDao2>().Named("myfirstdao"));
            container.Register(Component.For<SecondDao2>().Named("myseconddao"));
            container.Register(Component.For<OrderDao2>().Named("myorderdao"));
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void SuccessfulSituationWithTwoDatabases()
        {
            RootService2 service = container.Resolve<RootService2>();
            OrderDao2 orderDao = container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create(false);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType().Name == "TransactionManagerCommunicationException")
                    Assert.Ignore("MTS is not available");
                throw;
            }

            Array blogs = service.FindAll(typeof(Blog));
            Array blogitems = service.FindAll(typeof(BlogItem));
            Array orders = orderDao.FindAll(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogitems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, blogs.Length);
            Assert.AreEqual(1, blogitems.Length);
            Assert.AreEqual(1, orders.Length);
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void ExceptionOnEndWithTwoDatabases()
        {
            RootService2 service = container.Resolve<RootService2>();
            OrderDao2 orderDao = container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
            catch (RollbackResourceException e)
            {
                foreach (var resource in e.FailedResources)
                {
                    Console.WriteLine(resource.Item2);
                }

                throw;
            }

            Array blogs = service.FindAll(typeof(Blog));
            Array blogitems = service.FindAll(typeof(BlogItem));
            Array orders = orderDao.FindAll(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogitems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(0, blogs.Length);
            Assert.AreEqual(0, blogitems.Length);
            Assert.AreEqual(0, orders.Length);
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void SuccessfulSituationWithTwoDatabasesStateless()
        {
            RootService2 service = container.Resolve<RootService2>();
            OrderDao2 orderDao = container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create_Stateless(false);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.GetType().Name == "TransactionManagerCommunicationException")
                    Assert.Ignore("MTS is not available");
                throw;
            }

            Array blogs = service.FindAllStateless(typeof(Blog));
            Array blogitems = service.FindAllStateless(typeof(BlogItem));
            Array orders = orderDao.FindAllStateless(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogitems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(1, blogs.Length);
            Assert.AreEqual(1, blogitems.Length);
            Assert.AreEqual(1, orders.Length);
        }

        [Test]
        [Explicit("Requires MSDTC to be running.")]
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            RootService2 service = container.Resolve<RootService2>();
            OrderDao2 orderDao = container.Resolve<OrderDao2>("myorderdao");

            try
            {
                service.DoTwoDBOperation_Create_Stateless(true);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
            catch (RollbackResourceException e)
            {
                foreach (var resource in e.FailedResources)
                {
                    Console.WriteLine(resource.Item2);
                }

                throw;
            }

            Array blogs = service.FindAllStateless(typeof(Blog));
            Array blogitems = service.FindAllStateless(typeof(BlogItem));
            Array orders = orderDao.FindAllStateless(typeof(Order));

            Assert.IsNotNull(blogs);
            Assert.IsNotNull(blogitems);
            Assert.IsNotNull(orders);
            Assert.AreEqual(0, blogs.Length);
            Assert.AreEqual(0, blogitems.Length);
            Assert.AreEqual(0, orders.Length);
        }
    }
}
