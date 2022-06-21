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

    public class MySecondDao
    {
        private readonly ISessionManager _sessionManager;

        public MySecondDao(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void PerformPieceOfOperation(ISession previousSession)
        {
            Assert.IsNotNull(previousSession);

            using (var session = _sessionManager.OpenSession())
            {
                Assert.IsNotNull(session);
                Assert.IsTrue(SessionDelegate.AreEqual(session, previousSession));
            }
        }

        public void PerformPieceOfOperation2(ISession previousSession)
        {
            Assert.IsNotNull(previousSession);

            using (var session = _sessionManager.OpenSession())
            {
                Assert.IsNotNull(session);
                // Assert.AreNotSame(session, previousSession);
                Assert.IsFalse(ReferenceEquals(session, previousSession));
            }
        }

        public void PerformStatelessPieceOfOperation(IStatelessSession previousSession)
        {
            Assert.IsNotNull(previousSession);

            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.IsNotNull(session);
                Assert.IsTrue(StatelessSessionDelegate.AreEqual(session, previousSession));
            }
        }

        public void PerformStatelessPieceOfOperation2(IStatelessSession previousSession)
        {
            Assert.IsNotNull(previousSession);

            using (var session = _sessionManager.OpenStatelessSession())
            {
                Assert.IsNotNull(session);
                Assert.IsFalse(ReferenceEquals(session, previousSession));
            }
        }
    }
}