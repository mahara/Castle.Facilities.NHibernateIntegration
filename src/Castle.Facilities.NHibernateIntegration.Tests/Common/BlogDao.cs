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

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    using System.Collections;
    using System.Collections.Generic;

    using MicroKernel;

    public class BlogDao
    {
        protected readonly IKernel Kernel;
        protected readonly ISessionManager SessionManager;

        public BlogDao(IKernel kernel, ISessionManager sessionManager)
        {
            Kernel = kernel;
            SessionManager = sessionManager;
        }

        public Blog CreateBlog(string name)
        {
            using var session = SessionManager.OpenSession();
            var blog = new Blog
            {
                Name = name,
                Items = new List<BlogItem>()
            };

            session.Save(blog);

            return blog;
        }

        public IList ObtainBlogs()
        {
            using var session = SessionManager.OpenSession();
            return session.CreateQuery("from Blog").List();
        }

        public void DeleteAll()
        {
            using var session = SessionManager.OpenSession();
            session.Delete("from Blog");
        }

        public IList ObtainBlogsStateless()
        {
            using var session = SessionManager.OpenStatelessSession();
            return session.CreateQuery("from Blog").List();
        }
    }
}