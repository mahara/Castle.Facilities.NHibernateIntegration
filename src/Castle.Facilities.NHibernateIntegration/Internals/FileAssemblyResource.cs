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

using System.Text;

using Castle.Core.Resource;

namespace Castle.Facilities.NHibernateIntegration.Internals
{
    /// <summary>
    /// Resource for a file or an assembly resource.
    /// </summary>
    public class FileAssemblyResource : IResource
    {
        private readonly IResource _innerResource;

        /// <summary>
        /// Depending on the resource type, <see cref="AssemblyResource" /> or <see cref="FileResource" /> is decorated.
        /// </summary>
        /// <param name="resource"></param>
        public FileAssemblyResource(string resource)
        {
            if (File.Exists(resource))
            {
                _innerResource = new FileResource(resource);
            }
            else
            {
                _innerResource = new AssemblyResource(resource);
            }
        }

        public void Dispose()
        {
            _innerResource.Dispose();

            GC.SuppressFinalize(this);
        }

        public string FileBasePath => _innerResource.FileBasePath;

        public IResource CreateRelative(string relativePath)
        {
            return _innerResource.CreateRelative(relativePath);
        }

        public TextReader GetStreamReader()
        {
            return _innerResource.GetStreamReader();
        }

        public TextReader GetStreamReader(Encoding encoding)
        {
            return _innerResource.GetStreamReader(encoding);
        }
    }
}
