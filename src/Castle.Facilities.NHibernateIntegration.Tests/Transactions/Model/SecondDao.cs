#region License
// Copyright 2004-2025 Castle Project - https://www.castleproject.org/
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

using Castle.Services.Transaction;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [Transactional]
    public class SecondDao
    {
        private readonly ISessionManager _sessionManager;

        public SecondDao(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [Transaction]
        public virtual BlogItem Create(Blog blog)
        {
            using var session = _sessionManager.OpenSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            session.Save(blogItem);

            return blogItem;
        }

        [Transaction]
        public virtual BlogItem CreateWithException1(Blog blog)
        {
            using var session = _sessionManager.OpenSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            throw new NotSupportedException("I dont feel like supporting this.");
        }

        [Transaction]
        public virtual BlogItem CreateWithException2(Blog blog)
        {
            using var session = _sessionManager.OpenSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            session.Save(blogItem);

            throw new NotSupportedException("I dont feel like supporting this.");
        }

        [Transaction]
        public virtual BlogItem CreateStateless(Blog blog)
        {
            using var session = _sessionManager.OpenStatelessSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            session.Insert(blogItem);

            return blogItem;
        }

        [Transaction]
        public virtual BlogItem CreateWithExceptionStateless1(Blog blog)
        {
            using var session = _sessionManager.OpenStatelessSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            throw new NotSupportedException("I dont feel like supporting this.");
        }

        [Transaction]
        public virtual BlogItem CreateWithExceptionStateless2(Blog blog)
        {
            using var session = _sessionManager.OpenStatelessSession();

            var sessionTransaction = session.GetCurrentTransaction();

            Assert.That(sessionTransaction, Is.Not.Null);
            Assert.That(sessionTransaction.IsActive);

            var blogItem = new BlogItem
            {
                ParentBlog = blog,
                Title = "splinter cell is cool!",
                Text = "x",
                DateTime = DateTimeOffset.Now,
            };

            session.Insert(blogItem);

            throw new NotSupportedException("I dont feel like supporting this.");
        }
    }
}
