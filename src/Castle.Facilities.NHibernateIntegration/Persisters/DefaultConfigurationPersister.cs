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

namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    using System.Collections.Generic;
    using System.IO;

    using NHibernate.Cfg;

    /// <summary>
    /// Knows how to read/write an NHibernate <see cref="Configuration" /> from a given filename,
    /// and whether that file should be trusted or a new Configuration should be built.
    /// </summary>
    public class DefaultConfigurationPersister : IConfigurationPersister
    {
        private readonly IObjectPersister<Configuration> _persister =
            ObjectPersisterFactory.Create<Configuration>();

        /// <inheritdoc />
        public virtual Configuration ReadConfiguration(string filePath)
        {
            return _persister.Read(filePath);
        }

        /// <inheritdoc />
        public virtual void WriteConfiguration(Configuration configuration, string filePath)
        {
            _persister.Write(configuration, filePath);
        }

        /// <inheritdoc />
        public virtual bool IsNewConfigurationRequired(string filePath, IList<string> dependencies)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            var lastModified = File.GetLastWriteTime(filePath);

            var requiresNew = false;

            for (var i = 0; i < dependencies.Count && !requiresNew; i++)
            {
                var dependencyLastModified = File.GetLastWriteTime(dependencies[i]);
                requiresNew |= dependencyLastModified > lastModified;
            }

            return requiresNew;
        }
    }
}