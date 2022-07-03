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

    using NHibernate;

    using NUnit.Framework;

    [TestFixture]
    public class TransactionsTestCase : AbstractNHibernateTestCase
    {
        protected override void ConfigureContainer()
        {
            Container.Register(Component.For<RootService>().Named("root"));
            Container.Register(Component.For<FirstDao>().Named("myfirstdao"));
            Container.Register(Component.For<SecondDao>().Named("myseconddao"));
            Container.Register(Component.For<OrderDao>().Named("myorderdao"));
        }

        [Test]
        public void TestTransaction()
        {
            var service = Container.Resolve<RootService>();
            var dao = Container.Resolve<FirstDao>("myfirstdao");

            var blog = dao.Create("Blog1");

            try
            {
                service.DoBlogRefOperation(blog);

                // Expects a constraint exception on Commit.
                Assert.Fail("Must fail.");
            }
            catch (Exception)
            {
                // Transaction exception expected.
            }
        }

        [Test]
        public void TestTransactionStateless()
        {
            var service = Container.Resolve<RootService>();
            var dao = Container.Resolve<FirstDao>("myfirstdao");

            var blog = dao.CreateStateless("Blog1");

            try
            {
                service.DoBlogRefOperationStateless(blog);

                // Expects a constraint exception on Commit.
                Assert.Fail("Must fail.");
            }
            catch (Exception)
            {
                // Transaction exception expected.
            }
        }

        [Test]
        public void TestTransactionUsingDetachedCriteria()
        {
            var service = Container.Resolve<RootService>();

            var blogName = "Delicious Food!";

            var blogA = service.CreateBlogUsingDetachedCriteria(blogName);
            Assert.That(blogA, Is.Not.Null);

            var blogB = service.FindBlogUsingDetachedCriteria(blogName);
            Assert.That(blogB, Is.Not.Null);

            Assert.That(blogB.Name, Is.EqualTo(blogA.Name));
        }

        [Test]
        public void TestTransactionStatelessUsingDetachedCriteria()
        {
            var service = Container.Resolve<RootService>();

            var blogName = "Delicious Food!";

            var blogA = service.CreateBlogStatelessUsingDetachedCriteria(blogName);
            Assert.That(blogA, Is.Not.Null);

            var blogB = service.FindBlogStatelessUsingDetachedCriteria(blogName);
            Assert.That(blogB, Is.Not.Null);

            Assert.That(blogB.Name, Is.EqualTo(blogA.Name));
        }

        [Test]
        public void SimpleAndSucessfulSituationUsingRootTransactionBoundary()
        {
            var service = Container.Resolve<RootService>();

            service.SuccessfulCall();

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogitems, Has.Length.EqualTo(1));
        }

        [Test]
        public void SimpleAndSucessfulSituationUsingRootTransactionBoundaryStateless()
        {
            var service = Container.Resolve<RootService>();

            service.SuccessfulCallStateless();

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogitems = service.FindAllStateless(typeof(BlogItem));

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogitems, Is.Not.Null);
            Assert.That(blogs, Has.Length.EqualTo(1));
            Assert.That(blogitems, Has.Length.EqualTo(1));
        }

        [Test]
        public void NonTransactionalRoot()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenSession())
            {
                transaction = session.GetCurrentTransaction();
                Assert.That(transaction, Is.Null);

                var first = Container.Resolve<FirstDao>("myfirstdao");
                var second = Container.Resolve<SecondDao>("myseconddao");

                // This call is transactional.
                var blog = first.Create();

                // TODO: Assert transaction was committed
                //var sessionImplementation = session.GetSessionImplementation();
                //var connectionManager = sessionImplementation.ConnectionManager;
                //var tx = connectionManager.CurrentTransaction;
                //transaction = session.GetCurrentTransaction();
                //Assert.That(transaction.WasCommitted, Is.True);

                try
                {
                    second.CreateWithException2(blog);
                }
                catch (Exception)
                {
                    // Expected.
                }

                // TODO: Assert transaction was rolled back
                //transaction = session.GetCurrentTransaction();
                //Assert.That(transaction.WasRolledBack, Is.True);

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAll(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(1));
                var blogItems = rootService.FindAll(typeof(BlogItem));
                Assert.That(blogItems, Is.Empty);
            }

            Assert.That(transaction, Is.Null);
        }

        [Test]
        public void NonTransactionalRootStateless()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenStatelessSession())
            {
                transaction = session.GetCurrentTransaction();
                Assert.That(transaction, Is.Null);

                var first = Container.Resolve<FirstDao>("myfirstdao");
                var second = Container.Resolve<SecondDao>("myseconddao");

                // This call is transactional
                var blog = first.CreateStateless();

                // TODO: Assert transaction was committed
                //transaction = session.GetCurrentTransaction();
                //Assert.IsTrue(transaction.WasCommitted);

                try
                {
                    second.CreateWithExceptionStateless2(blog);
                }
                catch (Exception)
                {
                    // Expected
                }

                // TODO: Assert transaction was rolled back
                //transaction = session.GetCurrentTransaction();
                //Assert.IsTrue(transaction.WasRolledBack);

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAllStateless(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(1));
                var blogItems = rootService.FindAllStateless(typeof(BlogItem));
                Assert.That(blogItems, Is.Empty);
            }

            Assert.That(transaction, Is.Null);
        }

        [Test]
        public void TransactionNotHijackingTheSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenSession())
            {
                transaction = session.GetCurrentTransaction();
                Assert.That(transaction, Is.Null);

                var service = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                var blog = service.Create();

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAll(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(1));
            }

            Assert.That(transaction, Is.Null);
            // TODO: Assert transaction was committed
            //Assert.IsTrue(transaction.WasCommitted);
        }

        [Test]
        public void TransactionNotHijackingTheStatelessSession()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenStatelessSession())
            {
                transaction = session.GetCurrentTransaction();
                Assert.That(transaction, Is.Null);

                var service = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                var blog = service.CreateStateless();

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAllStateless(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(1));
            }

            Assert.That(transaction, Is.Null);
            // TODO: Assert transaction was committed
            //Assert.IsTrue(transaction.WasCommitted);
        }

        [Test]
        public void SessionBeingSharedByMultipleTransactionsInSequence()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenSession())
            {
                transaction = session.GetCurrentTransaction();
                Assert.That(transaction, Is.Null);

                var service = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                service.Create();

                // This call is transactional.
                service.Create("ps2's blogs");

                // This call is transactional.
                service.Create("game cube's blogs");

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAll(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(3));
            }

            Assert.That(transaction, Is.Null);
            // TODO: Assert transaction was committed
            //Assert.IsTrue(transaction.WasCommitted);
        }

        [Test]
        public void SessionBeingSharedByMultipleTransactionsInSequenceStateless()
        {
            var sessionManager = Container.Resolve<ISessionManager>();

            ITransaction transaction;

            using (var session = sessionManager.OpenStatelessSession())
            {
                transaction = session.GetCurrentTransaction();

                Assert.That(transaction, Is.Null);

                var service = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                service.CreateStateless();

                // This call is transactional.
                service.CreateStateless("ps2's blogs");

                // This call is transactional.
                service.CreateStateless("game cube's blogs");

                var rootService = Container.Resolve<RootService>();

                var blogs = rootService.FindAllStateless(typeof(Blog));
                Assert.That(blogs, Has.Length.EqualTo(3));
            }

            Assert.That(transaction, Is.Null);
            // TODO: Assert transaction was committed
            //Assert.IsTrue(transaction.WasCommitted);
        }

        [Test]
        public void CallWithException1()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithException();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));

            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
        }

        [Test]
        public void CallWithException2()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithException2();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAll(typeof(Blog));
            var blogitems = service.FindAll(typeof(BlogItem));

            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
        }

        [Test]
        public void CallWithExceptionStateless1()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithExceptionStateless();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogitems = service.FindAllStateless(typeof(BlogItem));

            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
        }

        [Test]
        public void CallWithExceptionStateless2()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithExceptionStateless2();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAllStateless(typeof(Blog));
            var blogitems = service.FindAllStateless(typeof(BlogItem));

            Assert.That(blogs, Is.Empty);
            Assert.That(blogitems, Is.Empty);
        }
    }
}