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

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionCreation
{
    public class MyDao
    {
        private readonly ISessionManager _sessionManager;
        private readonly MySecondDao _secondDao;

        public MyDao(ISessionManager sessionManager, MySecondDao secondDao)
        {
            _sessionManager = sessionManager;
            _secondDao = secondDao;
        }

        public void PerformComplexOperation()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.That(session, Is.Not.Null);

                _secondDao.PerformPieceOfOperation(session);
            }
        }

        public void PerformComplexOperation2()
        {
            ISession previousSession = null;

            using (var session = _sessionManager.OpenSession())
            {
                previousSession = session;
            }

            _secondDao.PerformPieceOfOperation2(previousSession);
        }

        public void DoOpenCloseAndDispose()
        {
            using (var session = _sessionManager.OpenSession())
            {
                Assert.That(session.IsConnected);
                Assert.That(session.IsOpen);

                session.Close();

                Assert.That(session.IsConnected, Is.False);
                Assert.That(session.IsOpen, Is.False);
            }
        }

        public void PerformStatelessComplexOperation()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.That(session, Is.Not.Null);

                _secondDao.PerformStatelessPieceOfOperation(session);
            }
        }

        public void PerformStatelessComplexOperation2()
        {
            IStatelessSession previousSession = null;

            using (var session = _sessionManager.OpenStatelessSession())
            {
                previousSession = session;
            }

            _secondDao.PerformStatelessPieceOfOperation2(previousSession);
        }

        public void DoStatelessOpenCloseAndDispose()
        {
            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.That(session.IsConnected);
                Assert.That(session.IsOpen);

                session.Close();

                Assert.That(session.IsConnected, Is.False);
                Assert.That(session.IsOpen, Is.False);
            }
        }
    }
}
