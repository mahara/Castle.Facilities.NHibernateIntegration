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

namespace Castle.Facilities.NHibernateIntegration.Tests.Components;

using Castle.Facilities.NHibernateIntegration.Util;

using NUnit.Framework;

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
            Items =
            [
                new() { }
            ]
        };

        var dictionary = ReflectionUtility.GetPropertiesDictionary(blog);
        Assert.That(dictionary.ContainsKey("Name"));
        Assert.That(dictionary.ContainsKey("Id"));
        Assert.That(dictionary.ContainsKey("Items"));
        Assert.That(dictionary["Name"], Is.EqualTo("osman"));
    }

    [Test]
    public void SimpleTypeReturnsTrueForEnumStringDatetimeAndPrimitiveTypes()
    {
        Assert.That(ReflectionUtility.IsSimpleType(typeof(string)), Is.True);
        Assert.That(ReflectionUtility.IsSimpleType(typeof(DateTime)), Is.True);
        Assert.That(ReflectionUtility.IsSimpleType(typeof(MyEnum)), Is.True);
        Assert.That(ReflectionUtility.IsSimpleType(typeof(char)), Is.True);
    }
}
