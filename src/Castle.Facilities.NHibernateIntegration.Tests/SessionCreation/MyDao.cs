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

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionCreation;

using NHibernate;

using NUnit.Framework;

public class MyDao
{
    private readonly ISessionManager _sessionManager;
    private readonly MySecondDao _secondDao;

    public MyDao(ISessionManager sessionManager, MySecondDao secondDao)
    {
        _sessionManager = sessionManager;
        _secondDao = secondDao;
    }

    public void PerformComplexOperation1()
    {
        using var session = _sessionManager.OpenSession();

        Assert.That(session, Is.Not.Null);

        _secondDao.PerformSimpleOperation(session);
    }

    public void PerformComplexOperation2()
    {
        ISession? previousSession = null;

        using (var session = _sessionManager.OpenSession())
        {
            previousSession = session;
        }

        _secondDao.PerformSimpleOperation2(previousSession);
    }

    public void DoOpenCloseAndDisposeOperation()
    {
        using var session = _sessionManager.OpenSession();

        Assert.That(session.IsConnected, Is.True);
        Assert.That(session.IsOpen, Is.True);

        session.Close();

        Assert.That(session.IsConnected, Is.False);
        Assert.That(session.IsOpen, Is.False);
    }

    public void PerformComplexStatelessOperation1()
    {
        using var session = _sessionManager.OpenStatelessSession();

        Assert.That(session, Is.Not.Null);

        _secondDao.PerformSimpleStatelessOperation(session);
    }

    public void PerformComplexStatelessOperation2()
    {
        IStatelessSession? previousSession = null;

        using (var session = _sessionManager.OpenStatelessSession())
        {
            previousSession = session;
        }

        _secondDao.PerformSimpleStatelessOperation2(previousSession);
    }

    public void DoStatelessOpenCloseAndDisposeOperation()
    {
        using var session = _sessionManager.OpenStatelessSession();

        Assert.That(session.IsConnected, Is.True);
        Assert.That(session.IsOpen, Is.True);

        session.Close();

        Assert.That(session.IsConnected, Is.False);
        Assert.That(session.IsOpen, Is.False);
    }
}
