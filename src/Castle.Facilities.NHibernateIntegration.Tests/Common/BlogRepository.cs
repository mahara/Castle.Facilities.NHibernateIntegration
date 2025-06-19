#region License
// Copyright 2004-2019 Castle Project - https://www.castleproject.org/
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

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Common
{
    [NHSessionAware]
    public class BlogRepository
    {
        private readonly ISessionStore sessionStore;
        private readonly ISessionManager sessionManager;

        public BlogRepository(ISessionManager sessionManager, ISessionStore sessionStore)
        {
            this.sessionStore = sessionStore;
            this.sessionManager = sessionManager;
        }

        [NHSessionRequired]
        public virtual void FetchAll()
        {
            Assert.IsNotNull(sessionStore.FindCompatibleSession(Constants.DefaultAlias));

            sessionManager.OpenSession();
        }
    }
}
