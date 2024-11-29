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

using Castle.Services.Transaction;

namespace Castle.Facilities.NHibernateIntegration.Internal
{
    /// <summary>
    /// Synchronization to ensure the session disposal on the end of a transaction.
    /// </summary>
    public class SessionDisposeSynchronization : ISynchronization
    {
        private readonly SessionDelegate _session;

        public SessionDisposeSynchronization(SessionDelegate session)
        {
            _session = session;
        }

        /// <summary>
        /// Implementors may have code executing just before the transaction completes.
        /// </summary>
        public void BeforeCompletion()
        {
        }

        /// <summary>
        /// Implementors may have code executing just after the transaction completes.
        /// </summary>
        public void AfterCompletion()
        {
            _session.InternalClose(false);
        }
    }
}
