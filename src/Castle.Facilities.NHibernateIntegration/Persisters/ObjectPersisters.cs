#region License
// Copyright 2004-2022 Castle Project - https://www.castleproject.org/
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

using NHibernate.Util;

namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    public class ObjectPersisterFactory
    {
        public static IObjectPersister<T> Create<T>()
        {
            return new BinaryFormatterObjectPersister<T>();
        }
    }

    public interface IObjectPersister<T>
    {
        public T Read(string filePath, FileMode fileMode = FileMode.OpenOrCreate);

        public void Write(string filePath, T @object, FileMode fileMode = FileMode.Create);
    }

    /// <summary>
    ///     <see cref="System.Runtime.Serialization.Formatters.Binary.BinaryFormatter" /> object persister.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BinaryFormatterObjectPersister<T> : IObjectPersister<T>
    {
        public T Read(string filePath, FileMode fileMode = FileMode.OpenOrCreate)
        {
            using var fileStream = new FileStream(filePath, fileMode);
            using var memoryStream = new MemoryStream();

            fileStream.CopyTo(memoryStream);

            return (T) SerializationHelper.Deserialize(memoryStream.ToArray());
        }

        public void Write(string filePath, T @object, FileMode fileMode = FileMode.Create)
        {
            using var fileStream = new FileStream(filePath, fileMode);

            var buffer = SerializationHelper.Serialize(@object);

            fileStream.Write(buffer, 0, buffer.Length);
        }
    }
}
