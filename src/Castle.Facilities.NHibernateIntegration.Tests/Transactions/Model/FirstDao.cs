﻿#region License
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
    using Castle.Services.Transaction;

    using NHibernate;

    using NUnit.Framework;

    [Transactional]
    public class FirstDao
    {
        private readonly ISessionManager _sessionManager;

        public FirstDao(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [Transaction]
        public virtual Blog Create()
        {
            return Create("Xbox Blog");
        }

        [Transaction]
        public virtual Blog Create(string name)
        {
            using var session = _sessionManager.OpenSession();
            var transaction = session.GetCurrentTransaction();

            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.IsActive, Is.True);

            var blog = new Blog
            {
                Name = name
            };
            session.Save(blog);

            return blog;
        }

        [Transaction]
        public virtual void Delete(string name)
        {
            using var session = _sessionManager.OpenSession();
            var transaction = session.GetCurrentTransaction();

            Assert.That(transaction, Is.Not.Null);

            session.Delete($"from Blog b where b.Name ='{name}'");
            session.Flush();
        }

        public virtual void AddBlogRef(BlogRef blogRef)
        {
            using var session = _sessionManager.OpenSession();
            session.Save(blogRef);
        }

        [Transaction]
        public virtual Blog CreateStateless()
        {
            return CreateStateless("Xbox Blog");
        }

        [Transaction]
        public virtual Blog CreateStateless(string name)
        {
            using var session = _sessionManager.OpenStatelessSession();
            var transaction = session.GetCurrentTransaction();

            Assert.That(transaction, Is.Not.Null);
            Assert.That(transaction.IsActive, Is.True);

            var blog = new Blog
            {
                Name = name
            };
            session.Insert(blog);
            return blog;
        }

        [Transaction]
        public virtual void DeleteStateless(string name)
        {
            using var session = _sessionManager.OpenStatelessSession();
            var transaction = session.GetCurrentTransaction();

            Assert.That(transaction, Is.Not.Null);

            session.Delete($"from Blog b where b.Name ='{name}'");
        }

        public virtual void AddBlogRefStateless(BlogRef blogRef)
        {
            using var session = _sessionManager.OpenStatelessSession();
            session.Insert(blogRef);
        }
    }
}