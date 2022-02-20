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

namespace Castle.Facilities.NHibernateIntegration.Tests.Components
{
	using MicroKernel.Registration;

	using NHibernate;
	using NHibernate.Criterion;

	using NHibernateIntegration.Components.Dao;

	using NUnit.Framework;

	using System;

	[TestFixture]
	public class NHibernateGenericDaoTests : AbstractNHibernateTestCase
	{
		private class NonPersistentClass
		{
		}

		private ISessionManager _sessionManager;
		private NHibernateGenericDao _nhGenericDao;
		private NHibernateGenericDao _nhGenericDao2;

		public override void OnSetUp()
		{
			Container.Register(
				Component.For<NHibernateGenericDao>()
						 .ImplementedBy<NHibernateGenericDao>());
			_sessionManager = Container.Resolve<ISessionManager>();
			_nhGenericDao = Container.Resolve<NHibernateGenericDao>();
			_nhGenericDao2 = new NHibernateGenericDao(_sessionManager, "sessionFactory1");

			using (var session = _sessionManager.OpenSession())
			{
				var blog1 = new Blog { Name = "myblog1" };
				var blog1Item = new BlogItem
				{
					ItemDate = DateTime.Now,
					ParentBlog = blog1,
					Text = "Hello",
					Title = "mytitle1"
				};
				blog1.Items.Add(blog1Item);

				var blog2 = new Blog { Name = "myblog2" };
				var blog2Item = new BlogItem
				{
					ItemDate = DateTime.Now,
					ParentBlog = blog1,
					Text = "Hello",
					Title = "mytitle2"
				};
				blog2.Items.Add(blog2Item);

				var blog3 = new Blog { Name = "myblog3" };
				var blog3Item = new BlogItem
				{
					ItemDate = DateTime.Now,
					ParentBlog = blog1,
					Text = "Hello3",
					Title = "mytitle3"
				};
				blog3.Items.Add(blog3Item);
				session.Save(blog1);
				session.Save(blog1Item);
				session.Save(blog2);
				session.Save(blog2Item);
				session.Save(blog3);
				session.Save(blog3Item);
			}
		}

		public override void OnTearDown()
		{
			using (var session = _sessionManager.OpenSession())
			{
				session.Delete("from BlogItem");
				session.Delete("from Blog");
			}
		}

		[Test]
		public void SetUppedCorrectly()
		{
			Assert.That(_nhGenericDao.SessionFactoryAlias, Is.Null);
			Assert.That(_nhGenericDao2.SessionFactoryAlias, Is.EqualTo("sessionFactory1"));
		}

		[Test]
		public void CanGetById()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var blog = _nhGenericDao.FindById(typeof(Blog), 1) as Blog;
				Assert.That(blog.Id, Is.EqualTo(1));
			}
		}

		[Test]
		public void CanInitializeLazyProperty()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var b = _nhGenericDao.FindById(typeof(Blog), 1) as Blog;
				Assert.That(NHibernateUtil.IsInitialized(b.Items), Is.False);

				_nhGenericDao.InitializeLazyProperty(b, "Items");
				Assert.That(NHibernateUtil.IsInitialized(b.Items), Is.True);
			}
		}

		[Test]
		public void ThrowsExceptionOnNonExistingProperty()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var b = _nhGenericDao.FindById(typeof(Blog), 1) as Blog;
				Assert.Throws<ArgumentOutOfRangeException>(() => _nhGenericDao.InitializeLazyProperty(b, "Bla"));
			}
		}

		[Test]
		public void ThrowsExceptionWhenNullInstance()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var b = _nhGenericDao.FindById(typeof(Blog), 1) as Blog;
				Assert.Throws<ArgumentNullException>(() => _nhGenericDao.InitializeLazyProperty(null, "Items"));
				Assert.Throws<ArgumentNullException>(() => _nhGenericDao.InitializeLazyProperty(b, null));
			}
		}

		[Test]
		public void ThrowsExceptionWhenNullInstance2()
		{
			using (var session = _sessionManager.OpenSession())
			{
				Assert.Throws<ArgumentNullException>(() => _nhGenericDao.InitializeLazyProperties(null));
			}
		}

		[Test]
		public void CanInitializeAllLazyProperties()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var b = _nhGenericDao.FindById(typeof(Blog), 1) as Blog;
				Assert.That(NHibernateUtil.IsInitialized(b.Items), Is.False);

				_nhGenericDao.InitializeLazyProperties(b);
				Assert.That(NHibernateUtil.IsInitialized(b.Items), Is.True);
			}
		}

		[Test]
		public void CanSaveNewItem()
		{
			using (var session = _sessionManager.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var b = new Blog();
				b.Name = "blah";
				_nhGenericDao.Save(b);
				Assert.That(b.Id, Is.GreaterThan(0));

				transaction.Rollback();
			}
		}


		[Test]
		public void CannotSaveNull()
		{
			using (var session = _sessionManager.OpenSession())
			{
				Assert.Throws<DataException>(() => _nhGenericDao.Save(new NonPersistentClass()));
			}
		}

		[Test]
		public void CanFindAll()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(Blog));
				Assert.That(results, Has.Length.EqualTo(3));
			}
		}

		[Test]
		public void CanFindAllWithCriterion()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(Blog),
													new[] { Restrictions.Eq("Name", "myblog2") });

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((Blog) results.GetValue(0)).Name, Is.EqualTo("myblog2"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderBy()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem),
													new[] { Restrictions.Eq("Text", "Hello") },
													new[] { Order.Desc("Title") });

				Assert.That(results, Has.Length.EqualTo(2));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle2"));
				Assert.That(((BlogItem) results.GetValue(1)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderByLimits()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem),
													new[] { Restrictions.Eq("Text", "Hello") },
													new[] { Order.Desc("Title") },
													1,
													1);

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderByLimitsOutOfRangeReturnsEmptyArray()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem),
													new[] { Restrictions.Eq("Text", "Hello") },
													new[] { Order.Desc("Title") },
													2,
													3);

				Assert.That(results, Has.Length.EqualTo(0));
			}
		}

		[Test]
		public void CanFindAllWithLimits()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem), 1, 2);

				Assert.That(results, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void CanFindAllWithLimitsOutOfRangeReturnsEmptyArray()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem), 3, 4);

				Assert.That(results, Has.Length.EqualTo(0));
			}
		}

		[Test]
		public void CanFindAllWithCriterionLimit()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem),
													new[] { Restrictions.Eq("Text", "Hello") },
													0,
													1);

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void FindAllWithCustomQuery()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAllWithCustomQuery("from BlogItem b where b.Text='Hello'");

				Assert.That(results, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void FindAllWithCustomQueryLimits()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAllWithCustomQuery("from BlogItem b where b.Text='Hello'", 1, 1);
				Assert.That(results, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public void DeleteAllWithType()
		{
			using (var session = _sessionManager.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				_nhGenericDao.DeleteAll(typeof(Blog));
				transaction.Commit();
			}

			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(Blog));
				Assert.That(results, Has.Length.EqualTo(0));
			}
		}

		[Test]
		public void Delete()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(Blog));
				Assert.That(results, Has.Length.EqualTo(3));
			}

			using (var session = _sessionManager.OpenSession())
			using (var transaction = session.BeginTransaction())
			{
				var blog = _nhGenericDao.FindById(typeof(Blog), 1);
				_nhGenericDao.Delete(blog);
				transaction.Commit();
			}

			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAll(typeof(Blog));
				Assert.That(results, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void CreateSavesObjectInTheDatabase()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var b = new Blog { Name = "myblog4" };
				var id = _nhGenericDao.Create(b);
				Assert.That(id, Is.GreaterThan(0));
			}
		}

		[Test]
		public void GetByNamedQuery()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAllWithNamedQuery("getAllBlogs");

				Assert.That(results, Has.Length.EqualTo(3));
			}
		}

		[Test]
		public void GetByNamedQueryThrowsExceptionWhenNullParameter()
		{
			using (var session = _sessionManager.OpenSession())
			{
				Assert.Throws<ArgumentNullException>(() => _nhGenericDao.FindAllWithNamedQuery(null));
			}
		}

		[Test]
		public void GetByNamedQueryThrowsExceptionWhenNonExistingQuery()
		{
			using (var session = _sessionManager.OpenSession())
			{
				Assert.Throws<DataException>(() => _nhGenericDao.FindAllWithNamedQuery("getMyBlogs"));
			}
		}

		[Test]
		public void GetByNamedQueryWithLimits()
		{
			using (var session = _sessionManager.OpenSession())
			{
				var results = _nhGenericDao.FindAllWithNamedQuery("getAllBlogs", 1, 2);

				Assert.That(results, Has.Length.EqualTo(2));
			}
		}


		[Test]
		public void CanFindAllStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(typeof(Blog));
				Assert.That(results, Has.Length.EqualTo(3));
			}
		}

		[Test]
		public void CanFindAllWithCriterionStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(
					typeof(Blog),
					new[] { Restrictions.Eq("Name", "myblog2") });

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((Blog) results.GetValue(0)).Name, Is.EqualTo("myblog2"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderByStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(
					typeof(BlogItem),
					new[] { Restrictions.Eq("Text", "Hello") },
					new[] { Order.Desc("Title") });

				Assert.That(results, Has.Length.EqualTo(2));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle2"));
				Assert.That(((BlogItem) results.GetValue(1)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderByLimitsStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAll(typeof(BlogItem),
												   new[] { Restrictions.Eq("Text", "Hello") },
												   new[] { Order.Desc("Title") }, 1, 1);

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void CanFindAllWithCriterionOrderByLimitsOutOfRangeReturnsEmptyArrayStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(
					typeof(BlogItem),
					new[] { Restrictions.Eq("Text", "Hello") },
					new[] { Order.Desc("Title") }, 2, 3);

				Assert.That(results, Has.Length.EqualTo(0));
			}
		}

		[Test]
		public void CanFindAllWithLimitsStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(typeof(BlogItem), 1, 2);
				Assert.That(results, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void CanFindAllWithLimitsOutOfRangeReturnsEmptyArrayStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(typeof(BlogItem), 3, 4);
				Assert.That(results, Has.Length.EqualTo(0));
			}
		}

		[Test]
		public void CanFindAllWithCriterionLimitStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllStateless(typeof(BlogItem),
															 new[] { Restrictions.Eq("Text", "Hello") },
															 0,
															 1);

				Assert.That(results, Has.Length.EqualTo(1));
				Assert.That(((BlogItem) results.GetValue(0)).Title, Is.EqualTo("mytitle1"));
			}
		}

		[Test]
		public void FindAllWithCustomQueryStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllWithCustomQueryStateless("from BlogItem b where b.Text='Hello'");

				Assert.That(results, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void FindAllWithCustomQueryLimitsStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllWithCustomQueryStateless("from BlogItem b where b.Text='Hello'", 1, 1);

				Assert.That(results, Has.Length.EqualTo(1));
			}
		}

		[Test]
		public void GetByNamedQueryStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllWithNamedQueryStateless("getAllBlogs");

				Assert.That(results, Has.Length.EqualTo(3));
			}
		}

		[Test]
		public void GetByNamedQueryThrowsExceptionWhenNullParameterStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				Assert.Throws<ArgumentNullException>(() => _nhGenericDao.FindAllWithNamedQueryStateless(null));
			}
		}

		[Test]
		public void GetByNamedQueryThrowsExceptionWhenNonExistingQueryStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				Assert.Throws<DataException>(() => _nhGenericDao.FindAllWithNamedQueryStateless("getMyBlogs"));
			}
		}

		[Test]
		public void GetByNamedQueryWithLimitsStateless()
		{
			using (var session = _sessionManager.OpenStatelessSession())
			{
				var results = _nhGenericDao.FindAllWithNamedQueryStateless("getAllBlogs", 1, 2);

				Assert.That(results, Has.Length.EqualTo(2));
			}
		}
	}
}