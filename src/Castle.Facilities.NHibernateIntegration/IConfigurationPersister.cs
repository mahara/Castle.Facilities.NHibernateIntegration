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

using NHibernateConfiguration = NHibernate.Cfg.Configuration;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    ///     Defines persistence and freshness-check operations
    ///     for an NHibernate <see cref="NHibernateConfiguration" /> instance.
    /// </summary>
    public interface IConfigurationPersister
    {
        /// <summary>
        ///     Reads and deserializes an NHibernate <see cref="NHibernateConfiguration" /> from the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file to read from.</param>
        /// <returns>The deserialized <see cref="NHibernateConfiguration" /> instance.</returns>
        NHibernateConfiguration ReadConfiguration(string filePath);

        /// <summary>
        ///     Serializes and writes the specified NHibernate <see cref="NHibernateConfiguration" /> to the specified file.
        /// </summary>
        /// <param name="filePath">The path to the file to write to.</param>
        /// <param name="configuration">The <see cref="NHibernateConfiguration" /> instance to persist.</param>
        void WriteConfiguration(string filePath, NHibernateConfiguration configuration);

        /// <summary>
        ///     Determines whether a new <see cref="NHibernateConfiguration" /> must be built
        ///     rather than reusing the serialized file.
        /// </summary>
        /// <param name="filePath">The path to the serialized <see cref="NHibernateConfiguration" /> file.</param>
        /// <param name="dependentFilePaths">The paths of files that the serialized configuration depends on.</param>
        /// <returns>
        ///     <see langword="true" /> if the serialized file does not exist or any dependent file is newer;
        ///     otherwise, <see langword="false" />.
        /// </returns>
        bool IsNewConfigurationRequired(string filePath, IList<string> dependentFilePaths);
    }
}
