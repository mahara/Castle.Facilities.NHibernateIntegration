#region License
// Copyright 2004-2021 Castle Project - https://www.castleproject.org/
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

using NHibernate;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Exposes constants used by the facility and its internal components.
    /// </summary>
    public class Constants
    {
        public const string DefaultAlias = "nhibernate.facility.default";

        public const string TransactionManager_ComponentName = "nhibernate.facility.transactionManager";

        public const string ConfigurationBuilder_ComponentName = "nhibernate.facility.configuration.builder";

        public const string ConfigurationBuilderType_ComponentNameFormat = "{0}.configurationBuilderType";
        public const string ConfigurationBuilderType_ConfigurationElementAttributeName = "configurationBuilderType";

        public const string UseReflectionOptimizer_ConfigurationElementAttributeName = "useReflectionOptimizer";

        public const string SessionFactoryResolver_ComponentName = "nhibernate.facility.sessionFactory.resolver";
        public const string SessionFactoryInterceptor_ComponentName = "nhibernate.sessionFactory.interceptor";
        /// <summary>
        /// The property name at which the configuration for a specific <see cref="ISessionFactory" /> is stored.
        /// </summary>
        public const string SessionFactory_Configuration_ComponentPropertyName = "Configuration";
        public const string SessionFactory_ConfigurationElementName = "sessionFactory";
        public const string SessionFactory_Id_ConfigurationElementAttributeName = "id";
        public const string SessionFactory_Alias_ConfigurationElementAttributeName = "alias";

        public const string SessionStore_ComponentName = "nhibernate.facility.sessionStore";
        public const string SessionStoreType_ConfigurationElementAttributeName = "sessionStoreType";
        public const string SessionStore_IsWeb_ConfigurationElementAttributeName = "isWeb";

        public const string SessionManager_ComponentName = "nhibernate.facility.sessionManager";

        internal const string Session_TransactionEnlistment_TransactionContextKey = "nhibernate.session.transactionEnlistment";
        internal const string StatelessSession_TransactionEnlistment_TransactionContextKey = "nhibernate.statelessSession.transactionEnlistment";

        public const string Session_DefaultFlushMode_ConfigurationElementAttributeName = "defaultFlushMode";
        public const string SessionInterceptor_ComponentName = "nhibernate.session.interceptor";
        public const string SessionInterceptor_ComponentNameFormat = "nhibernate.session.interceptor.{0}";
    }
}
