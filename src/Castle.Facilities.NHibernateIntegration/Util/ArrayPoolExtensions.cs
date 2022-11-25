namespace Castle.Facilities.NHibernateIntegration.Util
{
    using System;
    using System.Buffers;

    public static class ArrayPoolExtensions
    {
        public static ArrayPoolAllocation<T> Allocate<T>(this ArrayPool<T> pool, int minimumSize)
        {
            return new ArrayPoolAllocation<T>(pool, minimumSize);
        }
    }

    public readonly struct ArrayPoolAllocation<T> : IDisposable
    {
        private readonly ArrayPool<T> _pool;

        internal ArrayPoolAllocation(ArrayPool<T> pool, int minimumSize)
        {
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            Values = _pool.Rent(minimumSize);
        }

        public T[] Values { get; }

        public void Dispose()
        {
            _pool.Return(Values);
        }
    }
}
