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

using NHibernate.Cfg;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// An interface for reading/writing an NHibernate <see cref="Configuration" /> instance
    /// from/to a given file path,
    /// and whether that file should be trusted or a new configuration should be built.
    /// </summary>
    public interface IConfigurationPersister
    {
        /// <summary>
        /// Gets the NHibernate <see cref="Configuration" /> instance from the file.
        /// </summary>
        /// <param name="filePath">The path of the file to read from.</param>
        /// <returns>An NHibernate <see cref="Configuration" />.</returns>
        Configuration ReadConfiguration(string filePath);

        /// <summary>
        /// Writes the NHibernate <see cref="Configuration" /> instance to the file.
        /// </summary>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="configuration">The NHibernate <see cref="Configuration" />.</param>
        void WriteConfiguration(string filePath, Configuration configuration);

        /// <summary>
        /// Checks if a new NHibernate <see cref="Configuration" /> instance is required or a serialized one should be used.
        /// </summary>
        /// <param name="filePath">The path of the file containing the NHibernate <see cref="Configuration" />.</param>
        /// <param name="dependentFilePaths">The paths of the files that the serialized NHibernate <see cref="Configuration" /> depends on.</param>
        /// <returns>Whether the NHibernate <see cref="Configuration" /> should be created or not.</returns>
        bool IsNewConfigurationRequired(string filePath, IList<string> dependentFilePaths);
    }
}
