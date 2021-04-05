#region License

//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
// 

#endregion

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
	#region Using Directives

	using System;

	using Castle.Services.Transaction;

	using NHibernate;

	using NUnit.Framework;

	#endregion

	[Transactional]
	public class SecondDao
	{
		private readonly ISessionManager _sessionManager;

		public SecondDao(ISessionManager sessionManager)
		{
			this._sessionManager = sessionManager;
		}

		[Transaction]
		public virtual BlogItem Create(Blog blog)
		{
			using (var session = this._sessionManager.OpenSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				session.Save(item);

				return item;
			}
		}

		[Transaction]
		public virtual BlogItem CreateWithException(Blog blog)
		{
			using (var session = this._sessionManager.OpenSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				throw new NotSupportedException("I don't feel like supporting this");
			}
		}

		[Transaction]
		public virtual BlogItem CreateWithException2(Blog blog)
		{
			using (var session = this._sessionManager.OpenSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				session.Save(item);

				throw new NotSupportedException("I don't feel like supporting this");
			}
		}

		[Transaction]
		public virtual BlogItem CreateStateless(Blog blog)
		{
			using (var session = this._sessionManager.OpenStatelessSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				session.Insert(item);

				return item;
			}
		}

		[Transaction]
		public virtual BlogItem CreateWithExceptionStateless(Blog blog)
		{
			using (var session = this._sessionManager.OpenStatelessSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				throw new NotSupportedException("I don't feel like supporting this");
			}
		}

		[Transaction]
		public virtual BlogItem CreateWithExceptionStateless2(Blog blog)
		{
			using (var session = this._sessionManager.OpenStatelessSession())
			{
				var transaction = session.GetCurrentTransaction();

				Assert.IsNotNull(transaction);

				var item = new BlogItem();

				item.ParentBlog = blog;
				item.ItemDate = DateTime.Now;
				item.Text = "x";
				item.Title = "splinter cell is cool!";

				session.Insert(item);

				throw new NotSupportedException("I don't feel like supporting this");
			}
		}
	}
}