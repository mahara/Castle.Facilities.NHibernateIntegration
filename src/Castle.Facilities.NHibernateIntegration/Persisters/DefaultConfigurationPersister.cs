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

namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    /// <summary>
    ///     Default <see cref="IConfigurationPersister" /> implementation
    ///     backed by <see cref="ObjectPersisterFactory.Create{T}" />.
    /// </summary>
    public class DefaultConfigurationPersister : IConfigurationPersister
    {
        private readonly IObjectPersister<NHibernateConfiguration> _persister =
            ObjectPersisterFactory.Create<NHibernateConfiguration>();

        /// <inheritdoc />
        public virtual NHibernateConfiguration ReadConfiguration(string filePath)
        {
            return _persister.Read(filePath);
        }

        /// <inheritdoc />
        public virtual void WriteConfiguration(string filePath, NHibernateConfiguration configuration)
        {
            _persister.Write(filePath, configuration);
        }

        /// <inheritdoc />
        public virtual bool IsNewConfigurationRequired(string filePath, IList<string> dependentFilePaths)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            if (dependentFilePaths is null || dependentFilePaths.Count == 0)
            {
                return false;
            }

            var fileLastWriteTimeUtc = File.GetLastWriteTimeUtc(filePath);

            foreach (var dependentFilePath in dependentFilePaths)
            {
                if (!File.Exists(dependentFilePath))
                {
                    continue;
                }

                if (File.GetLastWriteTimeUtc(dependentFilePath) > fileLastWriteTimeUtc)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
