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

namespace Castle.Facilities.NHibernateIntegration.Util;

using System.Buffers;
#if NET
using System.Security.Cryptography;
#endif

public static class ArrayPoolExtensions
{
    public static ArrayPoolAllocation<T> Allocate<T>(this ArrayPool<T> pool,
                                                     int minimumLength,
                                                     bool clearBufferContents = true)
    {
        return new ArrayPoolAllocation<T>(pool, minimumLength, clearBufferContents);
    }

    public static ArrayPoolByteAllocation AllocateByte(this ArrayPool<byte> pool,
                                                       int minimumLength,
                                                       bool clearBufferContents = true)
    {
        return new ArrayPoolByteAllocation(pool, minimumLength, clearBufferContents);
    }
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <remarks>
///     REFERENCES:
///     -   <see href="https://ericlippert.com/2011/03/14/to-box-or-not-to-box/" />
///     -   <see href="https://stackoverflow.com/questions/7914423/struct-and-idisposable" />
///     -   <see href="https://stackoverflow.com/questions/2412981/if-my-struct-implements-idisposable-will-it-be-boxed-when-used-in-a-using-statem" />
///     -   <see href="https://stackoverflow.com/questions/1330571/when-does-a-using-statement-box-its-argument-when-its-a-struct" />
///     -   <see href="" />
/// </remarks>
#if NET
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed.")]
#endif
public readonly struct ArrayPoolAllocation<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    private readonly bool _clearBufferContents;

    internal ArrayPoolAllocation(ArrayPool<T> pool,
                                 int minimumLength,
                                 bool clearBufferContents)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _clearBufferContents = clearBufferContents;

        Buffer = _pool.Rent(minimumLength);
    }

    public void Dispose()
    {
        if (_clearBufferContents)
        {
            // https://github.com/dotnet/runtime/discussions/48697
            Buffer.AsSpan().Clear();
        }

        _pool.Return(Buffer);
    }

    public T[] Buffer { get; }
}

#if NET
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed.")]
#endif
public readonly struct ArrayPoolByteAllocation : IDisposable
{
    private readonly ArrayPool<byte> _pool;
    private readonly bool _clearBufferContents;

    internal ArrayPoolByteAllocation(ArrayPool<byte> pool,
                                     int minimumLength,
                                     bool clearBufferContents)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _clearBufferContents = clearBufferContents;

        Buffer = _pool.Rent(minimumLength);
    }

    public void Dispose()
    {
        if (_clearBufferContents)
        {
            // https://github.com/dotnet/runtime/discussions/48697
#if NET
            CryptographicOperations.ZeroMemory(Buffer.AsSpan());
#else
            Buffer.AsSpan().Clear();
#endif
        }

        _pool.Return(Buffer);
    }

    public byte[] Buffer { get; }
}
