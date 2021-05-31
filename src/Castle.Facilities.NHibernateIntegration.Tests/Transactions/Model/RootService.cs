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

using Castle.Facilities.NHibernateIntegration.Components.Dao;
using Castle.Services.Transaction;

using NHibernate.Criterion;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [Transactional]
    public class RootService : NHibernateGenericDao
    {
        private readonly FirstDao _firstDao;
        private readonly SecondDao _secondDao;

        public RootService(ISessionManager sessionManager, FirstDao firstDao, SecondDao secondDao) :
            base(sessionManager)
        {
            _firstDao = firstDao;
            _secondDao = secondDao;
        }

        public OrderDao OrderDao { get; set; }

        [Transaction]
        public virtual Blog FindBlogUsingDetachedCriteria(string name)
        {
            var dc = DetachedCriteria.For<Blog>();
            dc.Add(Property.ForName(nameof(Blog.Name)).Eq(name));

            var session = SessionManager.OpenSession();
            return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
        }

        [Transaction]
        public virtual Blog CreateBlog(string name)
        {
            return _firstDao.Create(name);
        }

        [Transaction]
        public virtual BlogItem SuccessfulCall()
        {
            var blog = _firstDao.Create();

            return _secondDao.Create(blog);
        }

        [Transaction]
        public virtual void CallWithException1()
        {
            var blog = _firstDao.Create();

            _secondDao.CreateWithException1(blog);
        }

        [Transaction]
        public virtual void CallWithException2()
        {
            var blog = _firstDao.Create();

            _secondDao.CreateWithException2(blog);
        }

        [Transaction]
        public virtual void BlogRefOperation_CallWithException(Blog blog)
        {
            var blogRef = new BlogRef
            {
                ParentBlog = blog,
                Title = "title",
            };
            _firstDao.AddBlogRef(blogRef);

            // Constraint exception.
            _firstDao.Delete("Blog1");
        }

        [Transaction]
        public virtual void TwoDbOperation_Create(bool throwException)
        {
            var blog = _firstDao.Create();
            _secondDao.Create(blog);

            OrderDao.Create(1.122d);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }

        [Transaction]
        public virtual Blog FindBlogUsingDetachedCriteriaStateless(string name)
        {
            var dc = DetachedCriteria.For<Blog>();
            dc.Add(Property.ForName(nameof(Blog.Name)).Eq(name));

            var session = SessionManager.OpenStatelessSession();
            return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
        }

        [Transaction]
        public virtual Blog CreateBlogStateless(string name)
        {
            return _firstDao.CreateStateless(name);
        }

        [Transaction]
        public virtual BlogItem SuccessfulCallStateless()
        {
            var blog = _firstDao.CreateStateless();

            return _secondDao.CreateStateless(blog);
        }

        [Transaction]
        public virtual void CallWithExceptionStateless1()
        {
            var blog = _firstDao.CreateStateless();

            _secondDao.CreateWithExceptionStateless1(blog);
        }

        [Transaction]
        public virtual void CallWithExceptionStateless2()
        {
            var blog = _firstDao.CreateStateless();

            _secondDao.CreateWithExceptionStateless2(blog);
        }

        [Transaction]
        public virtual void BlogRefOperation_CallWithExceptionStateless(Blog blog)
        {
            var blogRef = new BlogRef
            {
                ParentBlog = blog,
                Title = "title",
            };
            _firstDao.AddBlogRefStateless(blogRef);

            // Constraint exception.
            _firstDao.DeleteStateless("Blog1");
        }

        [Transaction]
        public virtual void TwoDbOperation_CreateStateless(bool throwException)
        {
            var blog = _firstDao.CreateStateless();
            _secondDao.CreateStateless(blog);

            OrderDao.CreateStateless(1.122d);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }
    }
}
