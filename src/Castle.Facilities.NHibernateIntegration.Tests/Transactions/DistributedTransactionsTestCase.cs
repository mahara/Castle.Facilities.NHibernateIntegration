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

using System.Transactions;

using Castle.MicroKernel.Registration;
using Castle.Services.Transaction;

using NUnit.Framework;

#if NETFRAMEWORK
namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
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
                if (ex.InnerException is not null &&
                    ex.InnerException.GetType().Name == nameof(TransactionManagerCommunicationException))
                {
                    Assert.Ignore("MTS is not available.");
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
            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
            Assert.That(orders, Is.Empty);
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
                if (ex.InnerException is not null &&
                    ex.InnerException.GetType().Name == nameof(TransactionManagerCommunicationException))
                {
                    Assert.Ignore("MTS is not available.");
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
        public void ExceptionOnEndWithTwoDatabasesStateless()
        {
            var service = Container.Resolve<RootService2>();
            var orderDao = Container.Resolve<OrderDao2>("myorderdao");

            try
            {
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
            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
            Assert.That(orders, Is.Empty);
        }
    }
}
#endif
