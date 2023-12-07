#region License
// Copyright 2004-2023 Castle Project - https://www.castleproject.org/
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

using ITransaction = NHibernate.ITransaction;

namespace Castle.Facilities.NHibernateIntegration.Internal
{
    /// <summary>
    /// Adapter to <see cref="IResource" /> so an NHibernate transaction can be enlisted within
    /// <see cref="Services.Transaction.ITransaction" /> instances.
    /// </summary>
    public class ResourceAdapter : IResource, IDisposable
    {
        private readonly ITransaction _transaction;
        private readonly bool _isAmbient;

        public ResourceAdapter(ITransaction transaction, bool isAmbient)
        {
            _transaction = transaction;
            _isAmbient = isAmbient;
        }

        public void Dispose()
        {
            _transaction.Dispose();

            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            _transaction.Begin();
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Rollback()
        {
            //
            // NOTE:    (HACK) It was supossed to only a test,
            //          but it fixed the escalated transaction rollback issue.
            //          Not sure if this the right way to do it (but probably not).
            //
            if (!_isAmbient)
            {
                _transaction.Rollback();
            }
        }
    }
}
