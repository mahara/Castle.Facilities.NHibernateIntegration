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

using NHibernate;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Exposes constants used by the facility and its internal components.
    /// </summary>
    public class Constants
    {
        public const string DefaultAlias = "nhibernate.facility.alias.default";

        public const string TransactionManager_ComponentName = "nhibernate.facility.transactionManager";

        public const string ConfigurationBuilder_ComponentName = "nhibernate.facility.configuration.configurationBuilder";
        public const string ConfigurationBuilderType_ComponentNameFormat = "nhibernate.facility.configuration.configurationBuilder.{0}.configurationBuilderType";
        public const string ConfigurationBuilderType_ConfigurationElementAttributeName = "configurationBuilderType";

        public const string UseReflectionOptimizer_ConfigurationElementAttributeName = "useReflectionOptimizer";

        public const string SessionFactoryResolver_ComponentName = "nhibernate.facility.sessionFactory.sessionFactoryResolver";
        public const string SessionFactoryInterceptor_ComponentName = "nhibernate.facility.sessionFactory.sessionFactoryInterceptor";
        /// <summary>
        /// The property name at which the configuration for a specific <see cref="ISessionFactory" /> is stored.
        /// </summary>
        public const string SessionFactory_Configuration_ComponentPropertyName = "nhibernate.facility.sessionFactory.configuration";
        public const string SessionFactory_ConfigurationElementName = "sessionFactory";
        public const string SessionFactory_Id_ConfigurationElementAttributeName = "id";
        public const string SessionFactory_Alias_ConfigurationElementAttributeName = "alias";
        public const string SessionFactory_FileName_ConfigurationElementAttributeName = "fileName";
        public const string SessionFactory_NHibernateConfigurationFilePath_ConfigurationElementAttributeName = "nhibernateConfigurationFilePath";
        public const string SessionFactory_Settings_ConfigurationElementName = "settings";
        public const string SessionFactory_Settings_Key_ConfigurationElementAttributeName = "key";
        public const string SessionFactory_Assemblies_ConfigurationElementName = "assemblies";
        public const string SessionFactory_DependsOn_ConfigurationElementName = "dependsOn";
        public const string SessionFactory_Resources_ConfigurationElementName = "resources";
        public const string SessionFactory_Resources_Name_ConfigurationElementAttributeName = "name";
        public const string SessionFactory_Resources_Assembly_ConfigurationElementAttributeName = "assembly";
        public const string SessionFactory_Listeners_ConfigurationElementName = "listeners";
        public const string SessionFactory_Listeners_Event_ConfigurationElementAttributeName = "event";
        public const string SessionFactory_Listeners_Type_ConfigurationElementAttributeName = "type";

        public const string Interceptor_SessionInterceptor_DependencyModelName = "nhibernate.facility.interceptor.sessionInterceptorDependencyModel";

        public const string Contributor_SessionComponentInspector_SessionRequiredMethods_PropertyName = "nhibernate.facility.contributor.sessionComponentInspector.sessionRequiredMethods";

        public const string SessionManager_ComponentName = "nhibernate.facility.sessionManager";

        public const string SessionStore_ComponentName = "nhibernate.facility.sessionStore";
        public const string SessionStoreType_ConfigurationElementAttributeName = "sessionStoreType";
        public const string SessionStore_IsWeb_ConfigurationElementAttributeName = "isWeb";
        public const string SessionStore_SessionStacks_SlotNameFormat = "nhibernate.facility.sessionStore.stacks.session.{0}";
        public const string SessionStore_StatelessSessionStacks_SlotNameFormat = "nhibernate.facility.sessionStore.stacks.statelessSession.{0}";

        public const string Session_TransactionEnlistment_TransactionContextKey = "nhibernate.facility.session.transactionEnlistment";
        public const string StatelessSession_TransactionEnlistment_TransactionContextKey = "nhibernate.facility.statelessSession.transactionEnlistment";
        public const string Session_DefaultFlushMode_ConfigurationElementAttributeName = "defaultFlushMode";

        public const string SessionInterceptor_ComponentName = "nhibernate.facility.session.sessionInterceptor";
        public const string SessionInterceptor_ComponentNameFormat = "nhibernate.facility.session.sessionInterceptor.{0}";
    }
}
