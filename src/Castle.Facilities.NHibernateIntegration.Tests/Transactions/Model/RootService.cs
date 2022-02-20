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
	using NHibernate.Criterion;

	using NHibernateIntegration.Components.Dao;

	using Services.Transaction;

	using System;

	[Transactional]
	public class RootService : NHibernateGenericDao
	{
		private readonly FirstDao firstDao;
		private readonly SecondDao secondDao;
		private OrderDao orderDao;

		public RootService(FirstDao firstDao, SecondDao secondDao, ISessionManager sessManager) : base(sessManager)
		{
			this.firstDao = firstDao;
			this.secondDao = secondDao;
		}

		public OrderDao OrderDao
		{
			get { return orderDao; }
			set { orderDao = value; }
		}

		[Transaction]
		public virtual Blog CreateBlogStatelessUsingDetachedCriteria(string name)
		{
			return firstDao.CreateStateless(name);
		}

		[Transaction]
		public virtual Blog FindBlogUsingDetachedCriteria(string name)
		{
			var dc = DetachedCriteria.For<Blog>();
			dc.Add(Property.ForName("Name").Eq(name));

			var session = SessionManager.OpenSession();
			return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
		}

		[Transaction]
		public virtual Blog FindBlogStatelessUsingDetachedCriteria(string name)
		{
			var dc = DetachedCriteria.For<Blog>();
			dc.Add(Property.ForName("Name").Eq(name));

			var session = SessionManager.OpenStatelessSession();
			return dc.GetExecutableCriteria(session).UniqueResult<Blog>();
		}

		[Transaction]
		public virtual BlogItem SuccessFullCall()
		{
			var blog = firstDao.Create();
			return secondDao.Create(blog);
		}

		[Transaction]
		public virtual void CallWithException()
		{
			var blog = firstDao.Create();
			secondDao.CreateWithException(blog);
		}

		[Transaction]
		public virtual void CallWithException2()
		{
			var blog = firstDao.Create();
			secondDao.CreateWithException2(blog);
		}

		[Transaction]
		public virtual void DoBlogRefOperation(Blog blog)
		{
			var blogRef = new BlogRef();
			blogRef.ParentBlog = blog;
			blogRef.Title = "title";
			firstDao.AddBlogRef(blogRef);

			//constraint exception
			firstDao.Delete("Blog1");
		}

		[Transaction]
		public virtual void DoTwoDBOperation_Create(bool throwException)
		{
			var blog = firstDao.Create();
			secondDao.Create(blog);
			orderDao.Create(1.122f);

			if (throwException)
			{
				throw new InvalidOperationException("Nah, giving up");
			}
		}

		[Transaction]
		public virtual BlogItem SuccessFullCallStateless()
		{
			var blog = firstDao.CreateStateless();
			return secondDao.CreateStateless(blog);
		}

		[Transaction]
		public virtual void CallWithExceptionStateless()
		{
			var blog = firstDao.CreateStateless();
			secondDao.CreateWithExceptionStateless(blog);
		}

		[Transaction]
		public virtual void CallWithExceptionStateless2()
		{
			var blog = firstDao.CreateStateless();
			secondDao.CreateWithExceptionStateless2(blog);
		}

		[Transaction]
		public virtual void DoBlogRefOperationStateless(Blog blog)
		{
			var blogRef = new BlogRef();
			blogRef.ParentBlog = blog;
			blogRef.Title = "title";
			firstDao.AddBlogRefStateless(blogRef);

			//constraint exception
			firstDao.DeleteStateless("Blog1");
		}

		[Transaction]
		public virtual void DoTwoDBOperation_Create_Stateless(bool throwException)
		{
			var blog = firstDao.CreateStateless();
			secondDao.CreateStateless(blog);
			orderDao.CreateStateless(1.122f);

			if (throwException)
			{
				throw new InvalidOperationException("Nah, giving up");
			}
		}
	}
}