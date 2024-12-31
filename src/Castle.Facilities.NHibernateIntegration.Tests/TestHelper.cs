#region License
// Copyright 2004-2025 Castle Project - https://www.castleproject.org/
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

#if NET
using NUnit.Framework;
#endif

namespace Castle.Facilities.NHibernateIntegration.Tests
{
    internal class TestHelper
    {
        public static void AssertApplicationConfigurationFileExists()
        {
#if NET
            var configurationFilePath = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None).FilePath;
            var configurationFileName = Path.GetFileName(configurationFilePath);

            Assert.That(configurationFileName, Is.AnyOf("testhost.dll.config",
                                                        "testhost.x86.dll.config")
                                                 .IgnoreCase);
#endif
        }
    }
}
