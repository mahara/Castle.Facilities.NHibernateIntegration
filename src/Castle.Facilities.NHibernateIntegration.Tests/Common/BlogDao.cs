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

using Castle.MicroKernel;

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    public class BlogDao
    {
        private readonly IKernel _kernel;
        private readonly ISessionManager _sessionManager;

        public BlogDao(IKernel kernel, ISessionManager sessionManager)
        {
            _kernel = kernel;
            _sessionManager = sessionManager;
        }

        public List<Blog> FindBlogs()
        {
            using var session = _sessionManager.OpenSession();

            return (List<Blog>) session.CreateQuery($"from {nameof(Blog)}").List<Blog>();
        }

        public List<Blog> FindBlogsStateless()
        {
            using var session = _sessionManager.OpenStatelessSession();

            return (List<Blog>) session.CreateQuery($"from {nameof(Blog)}").List<Blog>();
        }

        public Blog CreateBlog(string name)
        {
            using var session = _sessionManager.OpenSession();

            var blog = new Blog
            {
                Name = name,
                Items = [],
            };

            session.Save(blog);

            return blog;
        }

        public void DeleteAll()
        {
            using var session = _sessionManager.OpenSession();

            session.Delete($"from {nameof(Blog)}");
        }
    }
}
