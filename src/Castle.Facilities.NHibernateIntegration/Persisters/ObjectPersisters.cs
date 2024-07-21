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
    using System.IO;

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

        public void Write(T @object, string filePath, FileMode mode = FileMode.Create);
    }

    public class ObjectPersister<T> : IObjectPersister<T>
    {
        public T Read(string filePath, FileMode mode = FileMode.OpenOrCreate)
        {
            using var fileStream = new FileStream(filePath, mode);
            using var memoryStream = new MemoryStream();

            fileStream.CopyTo(memoryStream);

            return (T) SerializationHelper.Deserialize(memoryStream.ToArray());
        }

        public void Write(T @object, string filePath, FileMode mode = FileMode.Create)
        {
            using var fileStream = new FileStream(filePath, mode);

            var buffer = SerializationHelper.Serialize(@object);
#if NET
            fileStream.Write(buffer.AsSpan());
#else
            fileStream.Write(buffer, 0, buffer.Length);
#endif
        }
    }
}
