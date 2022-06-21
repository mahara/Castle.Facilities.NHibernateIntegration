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

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    using NHibernate;

    using NHibernateIntegration.Components.Dao;

    using NUnit.Framework;

    using Services.Transaction;

    [Transactional]
    public class OrderDao : NHibernateGenericDao
    {
        private readonly ISessionManager _sessionManager;

        public OrderDao(ISessionManager sessionManager) : base(sessionManager, "db2")
        {
            _sessionManager = sessionManager;
        }

        [Transaction]
        public virtual Order Create(float val)
        {
            using (var session = _sessionManager.OpenSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                var order = new Order
                {
                    Value = val
                };
                session.Save(order);

                return order;
            }
        }

        [Transaction]
        public virtual void Update(Order order, float newval)
        {
            using (var session = _sessionManager.OpenSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                order.Value = newval;

                session.Update(order);
            }
        }

        [Transaction]
        public virtual void Delete(int orderId)
        {
            using (var session = _sessionManager.OpenSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                var order = (Order) session.Load(typeof(Order), orderId);

                session.Delete(order);
            }
        }

        [Transaction]
        public virtual Order CreateStateless(float val)
        {
            using (var session = _sessionManager.OpenStatelessSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                var order = new Order
                {
                    Value = val
                };
                session.Insert(order);

                return order;
            }
        }

        [Transaction]
        public virtual void UpdateStateless(Order order, float newval)
        {
            using (var session = _sessionManager.OpenStatelessSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                order.Value = newval;

                session.Update(order);
            }
        }

        [Transaction]
        public virtual void DeleteStateless(int orderId)
        {
            using (var session = _sessionManager.OpenStatelessSession("db2"))
            {
                var transaction = session.GetCurrentTransaction();
                Assert.IsNotNull(transaction);

                var order = (Order) session.Get(typeof(Order).FullName, orderId);

                session.Delete(order);
            }
        }
    }
}