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

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [TestFixture]
    public class TransactionsTestCase : AbstractNHibernateTestCase
    {
        protected override void ConfigureContainer()
        {
            Container.Register(Component.For<RootService>().Named("root"));
            Container.Register(Component.For<FirstDao>().Named("myfirstdao"));
            Container.Register(Component.For<SecondDao>().Named("myseconddao"));
        }

        [Test]
        public void FailedTransaction()
        {
            var service = Container.Resolve<RootService>();
            var daoService = Container.Resolve<FirstDao>("myfirstdao");

            var blog = daoService.Create("Blog1");

            try
            {
                service.BlogRefOperation_CallWithException(blog);

                // Expects a constraint exception on Commit.
                Assert.Fail("Must fail.");
            }
            catch (Exception)
            {
                // Transaction exception expected.
            }
        }

        [Test]
        public void TransactionNotHijackingTheSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var daoService = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                var blog = daoService.Create();

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(1));
            }

            Assert.That(currentTransaction.WasCommitted);
        }

        [Test]
        public void SessionBeingSharedByMultipleTransactionsInSequence()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var daoService = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                daoService.Create();
                // This call is transactional.
                daoService.Create("PS2's Blogs");
                // This call is transactional.
                daoService.Create("Game Cube's Blogs");

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(3));
            }

            Assert.That(currentTransaction.WasCommitted);
        }

        [Test]
        public void NonTransactionalRoot()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var firstDaoService = Container.Resolve<FirstDao>("myfirstdao");
                var secondDaoService = Container.Resolve<SecondDao>("myseconddao");

                // This call is transactional.
                var blog = firstDaoService.Create();

                //
                //  TODO:   Assert transaction was committed.
                //
                //Assert.That(currentTransaction.WasCommitted);

                try
                {
                    secondDaoService.CreateWithException2(blog);
                }
                catch (Exception)
                {
                    // Expected.
                }

                //
                //  TODO:   Assert transaction was rolled back.
                //
                //Assert.That(currentTransaction.WasRolledBack);

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(1));

                var blogItems = service.FindAll<BlogItem>();

                Assert.That(blogItems, Is.Empty);
            }
        }

        [Test]
        public void SimpleAndSucessfulSituationUsingRootTransactionBoundary()
        {
            var service = Container.Resolve<RootService>();

            service.SuccessfulCall();

            var blogs = service.FindAll<Blog>();
            var blogItems = service.FindAll<BlogItem>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(1));
            Assert.That(blogItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void CallWithException1()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithException1();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAll<Blog>();
            var blogItems = service.FindAll<BlogItem>();

            Assert.That(blogs, Is.Empty);
            Assert.That(blogItems, Is.Empty);
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

            var blogs = service.FindAll<Blog>();
            var blogItems = service.FindAll<BlogItem>();

            Assert.That(blogs, Is.Empty);
            Assert.That(blogItems, Is.Empty);
        }

        [Test]
        public void SuccessfulTransactionUsingDetachedCriteria()
        {
            var service = Container.Resolve<RootService>();

            var blogName = "Delicious Food!";
            var blogA = service.CreateBlog(blogName);

            Assert.That(blogA, Is.Not.Null);

            var blogB = service.FindBlogUsingDetachedCriteria(blogName);

            Assert.That(blogB, Is.Not.Null);
            Assert.That(blogB.Name, Is.EqualTo(blogA.Name));
        }

        [Test]
        public void FailedTransactionStateless()
        {
            var service = Container.Resolve<RootService>();
            var daoService = Container.Resolve<FirstDao>("myfirstdao");

            var blog = daoService.CreateStateless("Blog1");

            try
            {
                service.BlogRefOperation_CallWithExceptionStateless(blog);

                // Expects a constraint exception on Commit.
                Assert.Fail("Must fail");
            }
            catch (Exception)
            {
                // Transaction exception expected.
            }
        }

        [Test]
        public void TransactionNotHijackingTheStatelessSession()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenStatelessSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var daoService = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                var blog = daoService.CreateStateless();

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAllStateless<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(1));
            }

            Assert.That(currentTransaction.WasCommitted);
        }

        [Test]
        public void SessionBeingSharedByMultipleTransactionsInSequenceStateless()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenStatelessSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var daoService = Container.Resolve<FirstDao>("myfirstdao");

                // This call is transactional.
                daoService.CreateStateless();
                // This call is transactional.
                daoService.CreateStateless("PS2's Blogs");
                // This call is transactional.
                daoService.CreateStateless("Game Cube's Blogs");

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAllStateless<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(3));
            }

            Assert.That(currentTransaction.WasCommitted);
        }

        [Test]
        public void NonTransactionalRootStateless()
        {
            var manager = Container.Resolve<ISessionManager>();

            ITransaction currentTransaction;

            using (var session = manager.OpenStatelessSession())
            {
                currentTransaction = session.Transaction;

                Assert.That(currentTransaction.IsActive, Is.False);

                var firstDaoService = Container.Resolve<FirstDao>("myfirstdao");
                var secondDaoService = Container.Resolve<SecondDao>("myseconddao");

                // This call is transactional.
                var blog = firstDaoService.CreateStateless();

                //
                //  TODO:   Assert transaction was committed.
                //
                //Assert.That(currentTransaction.WasCommitted);

                try
                {
                    secondDaoService.CreateWithExceptionStateless2(blog);
                }
                catch (Exception)
                {
                    // Expected.
                }

                //
                //  TODO:   Assert transaction was rolled back.
                //
                //Assert.That(currentTransaction.WasRolledBack);

                var service = Container.Resolve<RootService>();

                var blogs = service.FindAllStateless<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(1));

                var blogItems = service.FindAllStateless<BlogItem>();

                Assert.That(blogItems, Is.Empty);
            }
        }

        [Test]
        public void SimpleAndSucessfulSituationUsingRootTransactionBoundaryStateless()
        {
            var service = Container.Resolve<RootService>();

            service.SuccessfulCallStateless();

            var blogs = service.FindAllStateless<Blog>();
            var blogItems = service.FindAllStateless<BlogItem>();

            Assert.That(blogs, Is.Not.Null);
            Assert.That(blogItems, Is.Not.Null);
            Assert.That(blogs, Has.Count.EqualTo(1));
            Assert.That(blogItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void CallWithExceptionStateless1()
        {
            var service = Container.Resolve<RootService>();

            try
            {
                service.CallWithExceptionStateless1();
            }
            catch (NotSupportedException)
            {
            }

            // Ensure rollback happened.

            var blogs = service.FindAllStateless<Blog>();
            var blogItems = service.FindAllStateless<BlogItem>();

            Assert.That(blogs, Is.Empty);
            Assert.That(blogItems, Is.Empty);
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

            var blogs = service.FindAllStateless<Blog>();
            var blogItems = service.FindAllStateless<BlogItem>();

            Assert.That(blogs, Is.Empty);
            Assert.That(blogItems, Is.Empty);
        }

        [Test]
        public void SuccessfulTransactionUsingDetachedCriteriaStateless()
        {
            var service = Container.Resolve<RootService>();

            var blogName = "Delicious Food!";
            var blogA = service.CreateBlogStateless(blogName);

            Assert.That(blogA, Is.Not.Null);

            var blogB = service.FindBlogUsingDetachedCriteriaStateless(blogName);

            Assert.That(blogB, Is.Not.Null);
            Assert.That(blogB.Name, Is.EqualTo(blogA.Name));
        }
    }
}
