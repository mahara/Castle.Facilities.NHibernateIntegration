#region License
// Copyright 2004-2023 Castle Project - https://www.castleproject.org/
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
using System.Xml;

using Ionic.Zip;

namespace Explicit.NuGet.Versions
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                return;
            }

            var packageDiscoveryDirectoryPath = Path.Combine(Environment.CurrentDirectory, args[0]);
            var packageDiscoveryDirectory = new DirectoryInfo(packageDiscoveryDirectoryPath);
            var packageMetaData = ReadNuspecFromPackages(packageDiscoveryDirectory);
            UpdateNuspecManifestContent(packageMetaData, args[1]);
            WriteNuspecToPackages(packageMetaData);
        }

        private static Dictionary<string, NuspecContentEntry> ReadNuspecFromPackages(DirectoryInfo packageDiscoverDirectory)
        {
            var packageNuspecDictionary = new Dictionary<string, NuspecContentEntry>();

            foreach (var packageFilePath in packageDiscoverDirectory.GetFiles("*.nupkg", SearchOption.AllDirectories))
            {
                using var zipFile = ZipFile.Read(packageFilePath.FullName);

                foreach (var zipEntry in zipFile.Entries)
                {
                    if (zipEntry.FileName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                    {
                        using var zipEntryReader = new StreamReader(zipEntry.OpenReader());

                        var nuspecXml = zipEntryReader.ReadToEnd();
                        packageNuspecDictionary[packageFilePath.FullName] = new NuspecContentEntry
                        {
                            EntryName = zipEntry.FileName,
                            Contents = nuspecXml,
                        };

                        break;
                    }
                }
            }

            return packageNuspecDictionary;
        }

        private static void UpdateNuspecManifestContent(Dictionary<string, NuspecContentEntry> packageMetaData, string dependencyPackageIdPrefixFilter)
        {
            foreach (var packageFile in packageMetaData.ToList())
            {
                var nuspecXmlDocument = new XmlDocument();
                nuspecXmlDocument.LoadXml(packageFile.Value.Contents);

                SetPackageDependencyVersionsToBeExplicitForXmlDocument(nuspecXmlDocument, dependencyPackageIdPrefixFilter);

                string updatedNuspecXmlDocument;

                // UTF8 Encoding without BOM
                var encoding = new UTF8Encoding();
                // UTF8 Encoding with BOM
                //var encoding = Encoding.UTF8;

                using (var writer = new StringWriterWithEncoding(encoding))
                using (var xmlWriter = new XmlTextWriter(writer) { Formatting = Formatting.Indented })
                {
                    nuspecXmlDocument.Save(xmlWriter);
                    updatedNuspecXmlDocument = writer.ToString();
                }

                packageMetaData[packageFile.Key].Contents = updatedNuspecXmlDocument;
            }
        }

        private static void SetPackageDependencyVersionsToBeExplicitForXmlDocument(XmlDocument nuspecXmlDocument, string packageIdPrefixFilter)
        {
            WalkDocumentNodes(nuspecXmlDocument.ChildNodes, node =>
            {
                if (string.Equals(node.Name, "dependency", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(node.Attributes!["id"]!.Value) &&
                    node.Attributes!["id"]!.Value.StartsWith(packageIdPrefixFilter, StringComparison.OrdinalIgnoreCase))
                {
                    var dependencyVersion = node.Attributes!["version"]!.Value;
                    if (!(dependencyVersion.StartsWith('[') ||
                          dependencyVersion.EndsWith(']')))
                    {
                        node.Attributes!["version"]!.Value = $"[{dependencyVersion}]";
                    }
                }
            });
        }

        private static void WalkDocumentNodes(XmlNodeList nodes, Action<XmlNode> callback)
        {
            foreach (XmlNode node in nodes)
            {
                callback(node);

                WalkDocumentNodes(node.ChildNodes, callback);
            }
        }

        private static void WriteNuspecToPackages(Dictionary<string, NuspecContentEntry> packageMetaData)
        {
            foreach (var packageFile in packageMetaData.ToList())
            {
                using var zipFile = ZipFile.Read(packageFile.Key);

                zipFile.UpdateEntry(packageFile.Value.EntryName, packageFile.Value.Contents);
                zipFile.Save();
            }
        }

        record NuspecContentEntry
        {
            public string EntryName { get; set; } = string.Empty;

            public string Contents { get; set; } = string.Empty;
        }

        sealed class StringWriterWithEncoding : StringWriter
        {
            public StringWriterWithEncoding(Encoding encoding)
            {
                Encoding = encoding;
            }

            public override Encoding Encoding { get; }
        }
    }
}
