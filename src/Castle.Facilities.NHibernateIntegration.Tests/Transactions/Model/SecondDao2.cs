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

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions;

public class SecondDao2
{
    private readonly ISessionManager _sessionManager;

    public SecondDao2(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public BlogItem Create(Blog blog)
    {
        using var session = _sessionManager.OpenSession();

        var item = new BlogItem
        {
            ParentBlog = blog,
            Title = "splinter cell is cool!",
            Text = "x",
            DateTime = DateTime.Now,
        };

        session.Save(item);

        return item;
    }

    public BlogItem CreateStateless(Blog blog)
    {
        using var session = _sessionManager.OpenStatelessSession();

        var item = new BlogItem
        {
            ParentBlog = blog,
            Title = "splinter cell is cool!",
            Text = "x",
            DateTime = DateTime.Now,
        };

        session.Insert(item);

        return item;
    }
}
