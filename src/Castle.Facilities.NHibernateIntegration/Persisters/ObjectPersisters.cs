namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    using System;
    using System.Buffers;
    using System.IO;

    using Castle.Facilities.NHibernateIntegration.Util;

    using NHibernate.Util;

    public class ObjectPersisterFactory
    {
        public static IObjectPersister<T> Create<T>()
        {
            return new ObjectPersister<T>();
        }
    }

    public interface IObjectPersister<T>
    {
        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate);

        public void Write(T @object, string filePath, FileMode mode = FileMode.OpenOrCreate);
    }

    public class ObjectPersister<T> : IObjectPersister<T>
    {
        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            using var allocation = ArrayPool<byte>.Shared.Allocate((int) stream.Length);
            var bytes = allocation.Values;
#if NETFRAMEWORK
            stream.Read(bytes, 0, bytes.Length);
            var @object = (T) SerializationHelper.Deserialize(bytes);
#else
            var bytesAsSpan = bytes.AsSpan();
            stream.Read(bytesAsSpan);
            var @object = (T) SerializationHelper.Deserialize(bytesAsSpan.ToArray());
#endif
            return @object;
        }

        public void Write(T @object, string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            var bytes = SerializationHelper.Serialize(@object);
#if NETFRAMEWORK
            stream.Write(bytes, 0, bytes.Length);
#else
            var bytesAsSpan = bytes.AsSpan();
            stream.Write(bytesAsSpan);
#endif
        }
    }
}
