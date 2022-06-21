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

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionCreation
{
    using NHibernate;

    using NUnit.Framework;

    public class MyDao
    {
        private readonly ISessionManager _sessionManager;
        private readonly MySecondDao _otherDao;

        public MyDao(ISessionManager sessionManager, MySecondDao otherDao)
        {
            _sessionManager = sessionManager;
            _otherDao = otherDao;
        }

        public void PerformComplexOperation1()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.IsNotNull(session);

                _otherDao.PerformPieceOfOperation(session);
            }
        }

        public void PerformComplexOperation2()
        {
            ISession previousSession = null;

            using (var session = _sessionManager.OpenSession())
            {
                previousSession = session;
            }

            _otherDao.PerformPieceOfOperation2(previousSession);
        }

        public void DoOpenCloseAndDispose()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.IsTrue(session.IsConnected);
                Assert.IsTrue(session.IsOpen);

                session.Close();

                Assert.IsFalse(session.IsConnected);
                Assert.IsFalse(session.IsOpen);
            }
        }

        public void PerformStatelessComplexOperation1()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.IsNotNull(session);

                _otherDao.PerformStatelessPieceOfOperation(session);
            }
        }

        public void PerformStatelessComplexOperation2()
        {
            IStatelessSession previousSession = null;

            using (var session = _sessionManager.OpenStatelessSession())
            {
                previousSession = session;
            }

            _otherDao.PerformStatelessPieceOfOperation2(previousSession);
        }

        public void DoStatelessOpenCloseAndDispose()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.IsTrue(session.IsConnected);
                Assert.IsTrue(session.IsOpen);

                session.Close();

                Assert.IsFalse(session.IsConnected);
                Assert.IsFalse(session.IsOpen);
            }
        }
    }
}