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

namespace Castle.Facilities.NHibernateIntegration
{
    using System.Collections.Generic;

    using NHibernate.Cfg;

    /// <summary>
    /// Knows how to read/write an NHibernate <see cref="Configuration" /> from a given filename,
    /// and whether that file should be trusted or a new Configuration should be built.
    /// </summary>
    public interface IConfigurationPersister
    {
        /// <summary>
        /// Gets the <see cref="Configuration" /> from the file.
        /// </summary>
        /// <param name="filePath">The path of the file to read from.</param>
        /// <returns>The <see cref="Configuration" />.</returns>
        Configuration ReadConfiguration(string filePath);

        /// <summary>
        /// Writes the <see cref="Configuration" /> to the file.
        /// </summary>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <param name="configuration">The <see cref="Configuration" />.</param>
        void WriteConfiguration(string filePath, Configuration configuration);

        /// <summary>
        /// Checks if a new <see cref="Configuration" /> is required or a serialized one should be used.
        /// </summary>
        /// <param name="filePath">The path of the file containing the <see cref="Configuration" />.</param>
        /// <param name="dependencies">The files that the serialized configuration depends on.</param>
        /// <returns>Whether the <see cref="Configuration" /> should be created or not.</returns>
        bool IsNewConfigurationRequired(string filePath, IList<string> dependencies);
    }
}