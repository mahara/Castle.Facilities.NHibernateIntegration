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

using Castle.Services.Transaction;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [Transactional]
    public class FirstDao2
    {
        private readonly ISessionManager _sessionManager;

        public FirstDao2(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        [Transaction]
        public virtual Blog Create()
        {
            return Create("xbox blog");
        }

        [Transaction]
        public virtual Blog Create(string name)
        {
            using var session = _sessionManager.OpenSession();

            var blog = new Blog { Name = name };
            session.Save(blog);
            return blog;
        }

        [Transaction]
        public virtual Blog CreateStateless()
        {
            return CreateStateless("xbox blog");
        }

        [Transaction]
        public virtual Blog CreateStateless(string name)
        {
            using var session = _sessionManager.OpenStatelessSession();

            var blog = new Blog { Name = name };
            session.Insert(blog);
            return blog;
        }
    }
}
