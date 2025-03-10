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

namespace Castle.Facilities.NHibernateIntegration.Persisters
{
    public class DefaultConfigurationPersister : IConfigurationPersister
    {
        private readonly IObjectPersister<Configuration> _persister =
            ObjectPersisterFactory.Create<Configuration>();

        public virtual Configuration ReadConfiguration(string filePath)
        {
            return _persister.Read(filePath);
        }

        public virtual void WriteConfiguration(string filePath, Configuration configuration)
        {
            _persister.Write(filePath, configuration);
        }

        public virtual bool IsNewConfigurationRequired(string filePath, IList<string> dependencies)
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            var requiresNew = false;

            var lastModified = File.GetLastWriteTime(filePath);
            for (var i = 0; i < dependencies.Count && !requiresNew; i++)
            {
                var dependencyLastModified = File.GetLastWriteTime(dependencies[i]);
                requiresNew |= dependencyLastModified > lastModified;
            }

            return requiresNew;
        }
    }
}
