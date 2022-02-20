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

using Castle.Facilities.NHibernateIntegration.Components.Dao;
using Castle.MicroKernel.Registration;

using NHibernate;
using NHibernate.Criterion;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Components
{
    [TestFixture]
    public class NHibernateGenericDaoTests : AbstractNHibernateTestCase
    {
        private class NonPersistentClass
        {
        }

        private ISessionManager _sessionManager;
        private NHibernateGenericDao _nhGenericDao;
        private NHibernateGenericDao _nhGenericDao2;

        protected override void OnSetUp()
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
                    ParentBlog = blog1,
                    Title = "mytitle1",
                    Text = "Hello",
                    DateTime = DateTimeOffset.Now,
                };
                blog1.Items.Add(blog1Item);

                var blog2 = new Blog { Name = "myblog2" };
                var blog2Item = new BlogItem
                {
                    ParentBlog = blog1,
                    Title = "mytitle2",
                    Text = "Hello",
                    DateTime = DateTimeOffset.Now,
                };
                blog2.Items.Add(blog2Item);

                var blog3 = new Blog { Name = "myblog3" };
                var blog3Item = new BlogItem
                {
                    ParentBlog = blog1,
                    Title = "mytitle3",
                    Text = "Hello3",
                    DateTime = DateTimeOffset.Now,
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

        protected override void OnTearDown()
        {
            using (var session = _sessionManager.OpenSession())
            {
                session.Delete($"from {nameof(BlogItem)}");
                session.Delete($"from {nameof(Blog)}");
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
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.That(blog.Id, Is.EqualTo(1));
            }
        }

        [Test]
        public void CanInitializeLazyProperty()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.That(NHibernateUtil.IsInitialized(blog.Items), Is.False);

                _nhGenericDao.InitializeLazyProperty(blog, nameof(Blog.Items));

                Assert.That(NHibernateUtil.IsInitialized(blog.Items));
            }
        }

        [Test]
        public void ThrowsExceptionOnNonExistingProperty()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.Throws<ArgumentOutOfRangeException>(
                    () => _nhGenericDao.InitializeLazyProperty(blog, "Bla"));
            }
        }

        [Test]
        public void ThrowsExceptionWhenNullInstance1()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.Throws<ArgumentNullException>(
                    () => _nhGenericDao.InitializeLazyProperty(null, nameof(Blog.Items)));
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.InitializeLazyProperty(blog, null));
            }
        }

        [Test]
        public void ThrowsExceptionWhenNullInstance2()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.Throws<ArgumentNullException>(
                    () => _nhGenericDao.InitializeLazyProperty(null, nameof(Blog.Items)));
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.InitializeLazyProperty(blog, string.Empty));
            }
        }

        [Test]
        public void ThrowsExceptionWhenNullInstance3()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.Throws<ArgumentNullException>(
                    () => _nhGenericDao.InitializeLazyProperties(null));
            }
        }

        [Test]
        public void CanInitializeAllLazyProperties()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = _nhGenericDao.FindById<Blog>(1);

                Assert.That(NHibernateUtil.IsInitialized(blog.Items), Is.False);

                _nhGenericDao.InitializeLazyProperties(blog);

                Assert.That(NHibernateUtil.IsInitialized(blog.Items));
            }
        }

        [Test]
        public void CanSaveNewItem()
        {
            using (var session = _sessionManager.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var blog = new Blog { Name = "blah" };
                    _nhGenericDao.Save(blog);

                    Assert.That(blog.Id, Is.GreaterThan(0));

                    transaction.Rollback();
                }
            }
        }

        [Test]
        public void CannotSaveNull()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.Throws<DataException>(
                    () => _nhGenericDao.Save(new NonPersistentClass()));
            }
        }

        [Test]
        public void CanFindAll()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void CanFindAllWithCriterion()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAll<Blog>(
                    new[] { Restrictions.Eq(nameof(Blog.Name), "myblog2") });

                Assert.That(blogs, Has.Count.EqualTo(1));
                Assert.That(blogs[0].Name, Is.EqualTo("myblog2"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderBy()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) });

                Assert.That(blogItems, Has.Count.EqualTo(2));
                Assert.That(blogItems[0].Title, Is.EqualTo("mytitle2"));
                Assert.That(blogItems[1].Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderByLimits()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) }, 1, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
                Assert.That(blogItems[0].Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderByLimitsOutOfRangeReturnsEmptyList()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) }, 2, 3);

                Assert.That(blogItems, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void CanFindAllWithLimits()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(1, 2);

                Assert.That(blogItems, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void CanFindAllWithLimitsOutOfRangeReturnsEmptyList()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(3, 4);

                Assert.That(blogItems, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void CanFindAllWithCriterionLimit()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") }, 0, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
                Assert.That((blogItems[0]).Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void FindAllWithCustomQuery()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAllWithCustomQuery<BlogItem>(
                    $"from {nameof(BlogItem)} b where b.{nameof(BlogItem.Text)}='Hello'");

                Assert.That(blogItems, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void FindAllWithCustomQueryLimits()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogItems = _nhGenericDao.FindAllWithCustomQuery<BlogItem>(
                    $"from {nameof(BlogItem)} b where b.{nameof(BlogItem.Text)}='Hello'", 1, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void DeleteAllWithType()
        {
            using (var session = _sessionManager.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    _nhGenericDao.DeleteAll<Blog>();

                    transaction.Commit();
                }
            }

            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void Delete()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(3));
            }

            using (var session = _sessionManager.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var blog = _nhGenericDao.FindById<Blog>(1);
                    _nhGenericDao.Delete(blog);

                    transaction.Commit();
                }
            }

            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAll<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void CreateSavesObjectInTheDatabase()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blog = new Blog { Name = "myblog4" };
                var blogId = (int) _nhGenericDao.Create(blog);

                Assert.That(blogId, Is.GreaterThan(0));
            }
        }

        [Test]
        public void GetByNamedQuery()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAllWithNamedQuery<Blog>("getAllBlogs");

                Assert.That(blogs, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenNullParameter()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.FindAllWithNamedQuery<Blog>(null));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenEmptyStringParameter()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.FindAllWithNamedQuery<Blog>(string.Empty));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenNonExistingQuery()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.Throws<DataException>(
                    () => _nhGenericDao.FindAllWithNamedQuery<Blog>("getMyBlogs"));
            }
        }

        [Test]
        public void GetByNamedQueryWithLimits()
        {
            using (var session = _sessionManager.OpenSession())
            {
                var blogs = _nhGenericDao.FindAllWithNamedQuery<Blog>("getAllBlogs", 1, 2);

                Assert.That(blogs, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void CanFindAllStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogs = _nhGenericDao.FindAllStateless<Blog>();

                Assert.That(blogs, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void CanFindAllWithCriterionStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogs = _nhGenericDao.FindAllStateless<Blog>(
                    new[] { Restrictions.Eq(nameof(Blog.Name), "myblog2") });

                Assert.That(blogs, Has.Count.EqualTo(1));
                Assert.That(blogs[0].Name, Is.EqualTo("myblog2"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderByStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllStateless<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) });

                Assert.That(blogItems, Has.Count.EqualTo(2));
                Assert.That(blogItems[0].Title, Is.EqualTo("mytitle2"));
                Assert.That(blogItems[1].Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderByLimitsStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAll<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) }, 1, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
                Assert.That(blogItems[0].Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void CanFindAllWithCriterionOrderByLimitsOutOfRangeReturnsEmptyListStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllStateless<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") },
                    new[] { NHibernate.Criterion.Order.Desc(nameof(BlogItem.Title)) }, 2, 3);

                Assert.That(blogItems, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void CanFindAllWithLimitsStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllStateless<BlogItem>(1, 2);

                Assert.That(blogItems, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void CanFindAllWithLimitsOutOfRangeReturnsEmptyListStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllStateless<BlogItem>(3, 4);

                Assert.That(blogItems, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public void CanFindAllWithCriterionLimitStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllStateless<BlogItem>(
                    new[] { Restrictions.Eq(nameof(BlogItem.Text), "Hello") }, 0, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
                Assert.That(blogItems[0].Title, Is.EqualTo("mytitle1"));
            }
        }

        [Test]
        public void FindAllWithCustomQueryStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllWithCustomQueryStateless<BlogItem>(
                    $"from {nameof(BlogItem)} b where b.{nameof(BlogItem.Text)}='Hello'");

                Assert.That(blogItems, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void FindAllWithCustomQueryLimitsStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogItems = _nhGenericDao.FindAllWithCustomQueryStateless<BlogItem>(
                    $"from {nameof(BlogItem)} b where b.{nameof(BlogItem.Text)}='Hello'", 1, 1);

                Assert.That(blogItems, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public void GetByNamedQueryStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogs = _nhGenericDao.FindAllWithNamedQueryStateless<Blog>("getAllBlogs");

                Assert.That(blogs, Has.Count.EqualTo(3));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenNullParameterStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.FindAllWithNamedQueryStateless<Blog>(null));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenEmptyStringParameterStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.Throws<ArgumentException>(
                    () => _nhGenericDao.FindAllWithNamedQueryStateless<Blog>(string.Empty));
            }
        }

        [Test]
        public void GetByNamedQueryThrowsExceptionWhenNonExistingQueryStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.Throws<DataException>(
                    () => _nhGenericDao.FindAllWithNamedQueryStateless<Blog>("getMyBlogs"));
            }
        }

        [Test]
        public void GetByNamedQueryWithLimitsStateless()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                var blogs = _nhGenericDao.FindAllWithNamedQueryStateless<Blog>("getAllBlogs", 1, 2);

                Assert.That(blogs, Has.Count.EqualTo(2));
            }
        }
    }
}
