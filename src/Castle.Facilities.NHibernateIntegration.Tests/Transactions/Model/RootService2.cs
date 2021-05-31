#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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

using Castle.Services.Transaction;

namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
    [Transactional]
    public class RootService2 : NHibernateGenericDao
    {
        private readonly FirstDao2 _firstDao;
        private readonly SecondDao2 _secondDao;

        public RootService2(ISessionManager sessionManager, FirstDao2 firstDao, SecondDao2 secondDao) : base(sessionManager)
        {
            _firstDao = firstDao;
            _secondDao = secondDao;
        }

        public OrderDao2 OrderDao { get; set; }

        [Transaction(IsDistributed = true)]
        public virtual void DoTwoDbOperation_Create(bool throwException)
        {
            var blog = _firstDao.Create();
            _secondDao.Create(blog);

            OrderDao.Create(1.122d);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }

        [Transaction(IsDistributed = true)]
        public virtual void DoTwoDbOperation_CreateStateless(bool throwException)
        {
            var blog = _firstDao.CreateStateless();
            _secondDao.CreateStateless(blog);

            OrderDao.CreateStateless(1.122d);

            if (throwException)
            {
                throw new InvalidOperationException("Nah, giving up.");
            }
        }
    }
}
