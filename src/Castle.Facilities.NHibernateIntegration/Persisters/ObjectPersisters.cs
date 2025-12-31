#region License
// Copyright (c) 2004-2026 Castle Project - https://www.castleproject.org/
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
    /// <summary>
    ///     Factory for creating <see cref="IObjectPersister{T}" /> instances.
    /// </summary>
    public class ObjectPersisterFactory
    {
        /// <summary>
        ///     Creates a default <see cref="IObjectPersister{T}" /> instance.
        /// </summary>
        /// <typeparam name="T">The type of object to persist.</typeparam>
        /// <returns>A new <see cref="BinaryFormatterObjectPersister{T}" /> instance.</returns>
        public static IObjectPersister<T> Create<T>()
        {
            return new BinaryFormatterObjectPersister<T>();
        }
    }

    /// <summary>
    ///     Defines file-based persistence operations for objects of type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type of object to persist.</typeparam>
    public interface IObjectPersister<T>
    {
        /// <summary>
        ///     Reads and deserializes an instance of <typeparamref name="T" /> from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file to read from.</param>
        /// <param name="fileMode">The mode used to open the file.</param>
        /// <param name="fileAccess">The access level requested for the file.</param>
        /// <param name="fileShare">The sharing mode allowed for concurrent file access.</param>
        /// <returns>The deserialized instance of <typeparamref name="T" />.</returns>
        public T Read(
            string filePath,
            FileMode fileMode = FileMode.Open,
            FileAccess fileAccess = FileAccess.Read,
            FileShare fileShare = FileShare.Read);

        /// <summary>
        ///     Serializes and writes the specified object to a file, overwriting any existing content by default.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="object">The object to serialize and persist.</param>
        /// <param name="fileMode">The mode used to open or create the file.</param>
        /// <param name="fileAccess">The access level requested for the file.</param>
        /// <param name="fileShare">The sharing mode allowed for concurrent file access.</param>
        public void Write(
            string filePath,
            T @object,
            FileMode fileMode = FileMode.Create,
            FileAccess fileAccess = FileAccess.Write,
            FileShare fileShare = FileShare.None);
    }

    /// <summary>
    ///     <see cref="IObjectPersister{T}" /> implementation that delegates serialization
    ///     to <see cref="SerializationHelper" />.
    /// </summary>
    /// <typeparam name="T">The type of object to persist.</typeparam>
    public class BinaryFormatterObjectPersister<T> : IObjectPersister<T>
    {
        /// <inheritdoc />
        public T Read(
            string filePath,
            FileMode fileMode = FileMode.Open,
            FileAccess fileAccess = FileAccess.Read,
            FileShare fileShare = FileShare.Read)
        {
            using var fileStream = new FileStream(filePath, fileMode, fileAccess, fileShare);
            using var memoryStream = new MemoryStream();

            fileStream.CopyTo(memoryStream);

            return (T) SerializationHelper.Deserialize(memoryStream.ToArray());
        }

        /// <inheritdoc />
        public void Write(
            string filePath,
            T @object,
            FileMode fileMode = FileMode.Create,
            FileAccess fileAccess = FileAccess.Write,
            FileShare fileShare = FileShare.None)
        {
            var buffer = SerializationHelper.Serialize(@object);

            using var fileStream = new FileStream(filePath, fileMode, fileAccess, fileShare);

            fileStream.Write(buffer, 0, buffer.Length);
        }
    }
}
