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

using System.Runtime.Serialization;

namespace Castle.Facilities.NHibernateIntegration.Components.Dao
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// Contributed by Steve Degosserie &lt;steve.degosserie@vn.netika.com&gt;.
    /// </remarks>
#if !NET8_0_OR_GREATER
    [Serializable]
#endif
    public class DataException : Exception
    {
        public DataException(string message) :
            base(message)
        {
        }

        public DataException(string message, Exception innerException) :
            base(message, innerException)
        {
        }

#if NET8_0_OR_GREATER
        //[Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
#endif
        protected DataException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
        }
    }
}
