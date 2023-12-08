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
            using var allocation = ArrayPool<byte>.Shared.AllocateByte((int) stream.Length, true);
            var buffer = allocation.Buffer;
#if NET
            stream.Read(buffer.AsSpan());
#else
            stream.Read(buffer, 0, buffer.Length);
#endif
            return (T) SerializationHelper.Deserialize(buffer);
        }

        public void Write(T @object, string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var stream = new FileStream(filePath, mode);
            var buffer = SerializationHelper.Serialize(@object);
#if NET
            stream.Write(buffer.AsSpan());
#else
            stream.Write(buffer, 0, buffer.Length);
#endif
        }
    }
}
