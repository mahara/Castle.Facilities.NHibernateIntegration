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

        public OrderDao OrderDao { get; set; } = null!;

        [Transaction]
        public virtual Blog CreateBlogUsingDetachedCriteria(string name)
        {
            return _firstDao.Create(name);
        }

        [Transaction]
        public virtual Blog FindBlogUsingDetachedCriteria(string name)
        {
            var dc = DetachedCriteria.For<Blog>();
            dc.Add(Property.ForName(nameof(Blog.Name)).Eq(name));

            var session = SessionManager.OpenSession();
            return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
        }

        [Transaction]
        public virtual BlogItem SuccessfulCall()
        {
            var blog = _firstDao.Create();

            return _secondDao.Create(blog);
        }

        [Transaction]
        public virtual void CallWithException()
        {
            var blog = _firstDao.Create();

            _secondDao.CreateWithException(blog);
        }

        [Transaction]
        public virtual void CallWithException2()
        {
            var blog = _firstDao.Create();

            _secondDao.CreateWithException2(blog);
        }

        [Transaction]
        public virtual void DoBlogRefOperation(Blog blog)
        {
            var blogRef = new BlogRef
            {
                ParentBlog = blog,
                Title = "title"
            };

            _firstDao.AddBlogRef(blogRef);

            _firstDao.Delete("Blog1");
        }

        [Transaction]
        public virtual void TwoDbOperationCreate(bool throwException)
        {
            var blog = _firstDao.Create();

            _secondDao.Create(blog);

            OrderDao.Create(1.122f);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }

        [Transaction]
        public virtual Blog CreateBlogStatelessUsingDetachedCriteria(string name)
        {
            return _firstDao.CreateStateless(name);
        }

        [Transaction]
        public virtual Blog FindBlogStatelessUsingDetachedCriteria(string name)
        {
            var dc = DetachedCriteria.For<Blog>();
            dc.Add(Property.ForName(nameof(Blog.Name)).Eq(name));

            var session = SessionManager.OpenStatelessSession();
            return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
        }

        [Transaction]
        public virtual BlogItem SuccessfulCallStateless()
        {
            var blog = _firstDao.CreateStateless();
            return _secondDao.CreateStateless(blog);
        }

        [Transaction]
        public virtual void CallWithExceptionStateless()
        {
            var blog = _firstDao.CreateStateless();
            _secondDao.CreateWithExceptionStateless(blog);
        }

        [Transaction]
        public virtual void CallWithExceptionStateless2()
        {
            var blog = _firstDao.CreateStateless();
            _secondDao.CreateWithExceptionStateless2(blog);
        }

        [Transaction]
        public virtual void DoBlogRefOperationStateless(Blog blog)
        {
            var blogRef = new BlogRef
            {
                ParentBlog = blog,
                Title = "title"
            };

            _firstDao.AddBlogRefStateless(blogRef);

            _firstDao.DeleteStateless("Blog1");
        }

        [Transaction]
        public virtual void TwoDbOperationCreateStateless(bool throwException)
        {
            var blog = _firstDao.CreateStateless();

            _secondDao.CreateStateless(blog);

            OrderDao.CreateStateless(1.122f);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }
    }
}
