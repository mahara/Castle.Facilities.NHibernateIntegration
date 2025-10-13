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

using System.Reflection;

namespace Castle.Facilities.NHibernateIntegration.Utilities
{
    /// <summary>
    /// Utility classes for NHibernate.
    /// Contains methods to get properties of an entity etc.
    /// </summary>
    public class ReflectionUtility
    {
        private const BindingFlags PropertyBindingFlags =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.GetProperty;

        /// <summary>
        /// Gets the readable (non indexed) properties names and values.
        /// The keys holds the names of the properties.
        /// The values are the values of the properties.
        /// </summary>
        public static IDictionary<string, object> GetPropertiesDictionary(object obj)
        {
            Dictionary<string, object> dictionary = [];
            foreach (var property in obj.GetType().GetProperties(PropertyBindingFlags))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0)
                {
                    dictionary[property.Name] = property.GetValue(obj, null)!;
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Determines whether type is simple enough to need just ToString() to show its state.
        /// string, int, bool, and enums are simple; anything else is false.
        /// </summary>
        public static bool IsSimpleType(Type type)
        {
            return type == typeof(string) ||
                   type.IsPrimitive ||
                   type == typeof(DateTimeOffset) || type == typeof(DateTime) ||
                   type.IsEnum;
        }
    }
}
