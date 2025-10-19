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

using System.Diagnostics.CodeAnalysis;

namespace Castle.Facilities.NHibernateIntegration.Utilities
{
    public static class DictionaryExtensions
    {
        public static bool TryGetValueAs<TKey, TValue, TValueAs>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            [MaybeNullWhen(false)] out TValueAs? valueAs)
            where TValueAs : TValue
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                if (value is TValueAs validValueAs)
                {
                    valueAs = validValueAs;

                    return true;
                }
            }

            valueAs = default;

            return false;
        }
    }
}
