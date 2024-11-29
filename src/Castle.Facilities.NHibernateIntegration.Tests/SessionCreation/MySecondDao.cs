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

using NHibernate;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.SessionCreation
{
    public class MySecondDao
    {
        private readonly ISessionManager _sessionManager;

        public MySecondDao(ISessionManager sessionManager)
        {
            _sessionManager = sessionManager;
        }

        public void PerformPieceOfOperation(ISession previousSession)
        {
            Assert.That(previousSession, Is.Not.Null);

            using var session = _sessionManager.OpenSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(SessionDelegate.AreEqual(session, previousSession));
        }

        public void PerformPieceOfOperation2(ISession previousSession)
        {
            Assert.That(previousSession, Is.Not.Null);

            using var session = _sessionManager.OpenSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(ReferenceEquals(session, previousSession), Is.False);
        }

        public void PerformStatelessPieceOfOperation(IStatelessSession previousSession)
        {
            Assert.That(previousSession, Is.Not.Null);

            using var session = _sessionManager.OpenStatelessSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(StatelessSessionDelegate.AreEqual(session, previousSession));
        }

        public void PerformStatelessPieceOfOperation2(IStatelessSession previousSession)
        {
            Assert.That(previousSession, Is.Not.Null);

            using var session = _sessionManager.OpenStatelessSession();

            Assert.That(session, Is.Not.Null);
            Assert.That(ReferenceEquals(session, previousSession), Is.False);
        }
    }
}
