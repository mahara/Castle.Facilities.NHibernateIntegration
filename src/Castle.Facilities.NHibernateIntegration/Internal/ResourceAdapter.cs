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

namespace Castle.Facilities.NHibernateIntegration.Internal;

using Castle.Services.Transaction;

using ITransaction = NHibernate.ITransaction;

/// <summary>
/// Adapter to <see cref="IResource" /> so a NHibernate transaction can be enlisted
/// within <see cref="Services.Transaction.ITransaction" /> instances.
/// </summary>
public class ResourceAdapter : IResource, IDisposable
{
    private readonly ITransaction _transaction;
    private readonly bool _isAmbient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceAdapter" /> class.
    /// </summary>
    /// <param name="transaction">The transaction.</param>
    /// <param name="isAmbient"></param>
    public ResourceAdapter(ITransaction transaction, bool isAmbient)
    {
        _transaction = transaction;
        _isAmbient = isAmbient;
    }

    /// <summary>
    /// </summary>
    public void Dispose()
    {
        _transaction.Dispose();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Implementors should start the transaction on the underlying resource.
    /// </summary>
    public void Start()
    {
        _transaction.Begin();
    }

    /// <summary>
    /// Implementors should commit the transaction on the underlying resource.
    /// </summary>
    public void Commit()
    {
        _transaction.Commit();
    }

    /// <summary>
    /// Implementors should rollback the transaction on the underlying resource.
    /// </summary>
    public void Rollback()
    {
        // HACK: It was supposed to only a test but it fixed the escalated transaction rollback issue.
        //       Not sure if this the right way to do it (probably not).
        if (!_isAmbient)
        {
            _transaction.Rollback();
        }
    }
}
