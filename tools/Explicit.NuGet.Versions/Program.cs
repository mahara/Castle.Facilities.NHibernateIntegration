#region License
// Copyright (c) 2004-2025 Castle Project - https://www.castleproject.org/
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
        // UTF8 Encoding without BOM
        private static readonly Encoding Utf8Encoding = new UTF8Encoding(false);
        // UTF8 Encoding with BOM
        //private static readonly Encoding Utf8Encoding = Encoding.UTF8;

        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Usage: nev.exe <packages-directory> <package-id-prefix-filter>");

                return 1;
            }

            var packageDiscoveryDirectoryPath = Path.Combine(Environment.CurrentDirectory, args[0]);
            var packageDiscoveryDirectory = new DirectoryInfo(packageDiscoveryDirectoryPath);

            if (!packageDiscoveryDirectory.Exists)
            {
                Console.Error.WriteLine($"Directory not found: '{packageDiscoveryDirectory.FullName}'.");

                return 1;
            }

            var packageMetaData = ReadNuspecFromPackages(packageDiscoveryDirectory);
            UpdateNuspecManifestContent(packageMetaData, args[1]);
            WriteNuspecToPackages(packageMetaData);

            return 0;
        }

        private static Dictionary<string, NuspecContentEntry> ReadNuspecFromPackages(DirectoryInfo packageDiscoveryDirectory)
        {
            var packageNuspecDictionary = new Dictionary<string, NuspecContentEntry>();

            foreach (var packageFilePath in packageDiscoveryDirectory.GetFiles("*.nupkg", SearchOption.AllDirectories))
            {
                using var zipFile = ZipFile.Read(packageFilePath.FullName);

                foreach (var zipEntry in zipFile.Entries)
                {
                    if (zipEntry.FileName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
                    {
                        using var zipEntryReader = new StreamReader(zipEntry.OpenReader(), Utf8Encoding);

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
            foreach (var packageFile in packageMetaData)
            {
                var nuspecXmlDocument = new XmlDocument();
                nuspecXmlDocument.LoadXml(packageFile.Value.Contents);

                SetPackageDependencyVersionsToBeExplicitForXmlDocument(nuspecXmlDocument, dependencyPackageIdPrefixFilter);

                string updatedNuspecXmlDocument;

                using (var writer = new StringWriterWithEncoding(Utf8Encoding))
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
                if (node is XmlElement element &&
                    string.Equals(element.Name, "dependency", StringComparison.OrdinalIgnoreCase) &&
                    element.GetAttribute("id").StartsWith(packageIdPrefixFilter, StringComparison.OrdinalIgnoreCase))
                {
                    var version = element.GetAttribute("version");
                    if (!string.IsNullOrEmpty(version) &&
                        !version.Contains('*', StringComparison.Ordinal) &&
                        !version.StartsWith('[') && !version.StartsWith('(') &&
                        !version.EndsWith(']') && !version.EndsWith(')'))
                    {
                        element.SetAttribute("version", $"[{version}]");
                    }
                }
            });
        }

        private static void WalkDocumentNodes(XmlNodeList nodes, Action<XmlNode> callback)
        {
            foreach (XmlNode node in nodes)
            {
                callback(node);

                if (node.HasChildNodes)
                {
                    WalkDocumentNodes(node.ChildNodes, callback);
                }
            }
        }

        private static void WriteNuspecToPackages(Dictionary<string, NuspecContentEntry> packageMetaData)
        {
            foreach (var packageFile in packageMetaData)
            {
                using var zipFile = ZipFile.Read(packageFile.Key);

                zipFile.UpdateEntry(packageFile.Value.EntryName, Utf8Encoding.GetBytes(packageFile.Value.Contents));
                zipFile.Save();
            }
        }

        record NuspecContentEntry
        {
            public string EntryName { get; init; } = string.Empty;

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
