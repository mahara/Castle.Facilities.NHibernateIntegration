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

using System;
using System.Collections.Generic;

using Castle.Facilities.NHibernateIntegration.Utilities;

using NUnit.Framework;

namespace Castle.Facilities.NHibernateIntegration.Tests.Components
{
    [TestFixture]
    public class ReflectionUtilityTests
    {
        private enum MyEnum
        {
        }

        [Test]
        public void CanGetPropertiesAsDictionary()
        {
            var blog = new Blog
            {
                Name = "osman",
                Items = new List<BlogItem>()
                {
                    new() { }
                },
            };

            var dictionary = ReflectionUtility.GetPropertiesDictionary(blog);

            Assert.That(dictionary.ContainsKey(nameof(Blog.Id)));
            Assert.That(dictionary.ContainsKey(nameof(Blog.Name)));
            Assert.That(dictionary.ContainsKey(nameof(Blog.Items)));
            Assert.That(dictionary[nameof(Blog.Name)], Is.EqualTo("osman"));
        }

        [Test]
        public void SimpleTypeReturnsTrueFor_String_Primitivetypes_Datetime_And_Enum()
        {
            Assert.That(ReflectionUtility.IsSimpleType(typeof(string)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(char)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(int)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(double)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(DateTimeOffset)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(DateTime)));
            Assert.That(ReflectionUtility.IsSimpleType(typeof(MyEnum)));
        }
    }
}
