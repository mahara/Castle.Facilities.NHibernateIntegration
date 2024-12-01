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

using Castle.Facilities.NHibernateIntegration.Components.Dao;
using Castle.Services.Transaction;

using NHibernate;

using NUnit.Framework;

[Transactional]
public class OrderDao2 : NHibernateGenericDao
{
    private readonly ISessionManager _sessionManager;

    public OrderDao2(ISessionManager sessionManager) : base(sessionManager, "db2")
    {
        _sessionManager = sessionManager;
    }

    [Transaction]
    public virtual Order Create(float val)
    {
        using var session = _sessionManager.OpenSession("db2");

        var transaction = session.GetCurrentTransaction();
        Assert.That(transaction, Is.Not.Null);

        var order = new Order
        {
            Value = val
        };

        session.Save(order);

        return order;
    }

    [Transaction]
    public virtual Order CreateStateless(float val)
    {
        using var session = _sessionManager.OpenStatelessSession("db2");

        var transaction = session.GetCurrentTransaction();
        Assert.That(transaction, Is.Not.Null);

        var order = new Order
        {
            Value = val
        };

        session.Insert(order);

        return order;
    }
}
