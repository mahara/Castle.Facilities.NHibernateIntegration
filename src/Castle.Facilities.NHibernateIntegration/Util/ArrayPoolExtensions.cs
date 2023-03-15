namespace Castle.Facilities.NHibernateIntegration.Util
{
    using System;
    using System.Buffers;
#if NET
    using System.Security.Cryptography;
#endif

    public static class ArrayPoolExtensions
    {
        public static ArrayPoolAllocation<T> Allocate<T>(this ArrayPool<T> pool,
                                                         int minimumLength,
                                                         bool clearBufferContents = false)
        {
            return new ArrayPoolAllocation<T>(pool, minimumLength, clearBufferContents);
        }

        public static ArrayPoolByteAllocation AllocateByte(this ArrayPool<byte> pool,
                                                           int minimumLength,
                                                           bool clearBufferContents = false)
        {
            return new ArrayPoolByteAllocation(pool, minimumLength, clearBufferContents);
        }
    }

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

        public T[] Buffer { get; }

        public void Dispose()
        {
            if (_clearBufferContents)
            {
                // https://github.com/dotnet/runtime/discussions/48697
                Buffer.AsSpan(0, Buffer.Length).Clear();
            }

            _pool.Return(Buffer);
        }
    }

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

        public byte[] Buffer { get; }

        public void Dispose()
        {
            if (_clearBufferContents)
            {
                // https://github.com/dotnet/runtime/discussions/48697
#if NET
                CryptographicOperations.ZeroMemory(Buffer.AsSpan(0, Buffer.Length));
#else
                Buffer.AsSpan(0, Buffer.Length).Clear();
#endif
            }

            _pool.Return(Buffer);
        }
    }
}
