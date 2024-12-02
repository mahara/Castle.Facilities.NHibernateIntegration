#region License
// Copyright (c) 2004-2024 Castle Project - https://www.castleproject.org/
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

using System.Configuration;

using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Internals;
using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Conversion;
using Castle.Services.Transaction;
using Castle.Services.Transaction.Utilities;

using NHibernate;

using CastleConfiguration = Castle.Core.Configuration.IConfiguration;
using IInterceptor = NHibernate.IInterceptor;
using ILogger = Castle.Core.Logging.ILogger;
using ILoggerFactory = Castle.Core.Logging.ILoggerFactory;

namespace Castle.Facilities.NHibernateIntegration
{
    /// <summary>
    /// Provides a basic level of integration with the NHibernate project.
    /// </summary>
    /// <remarks>
    /// This facility allows components to gain access to the NHibernate's instances:
    /// <list type="bullet">
    ///   <item><description>NHibernate.Cfg.Configuration</description></item>
    ///   <item><description>NHibernate.ISessionFactory</description></item>
    /// </list>
    /// <para>
    /// It also allow you to obtain a <see cref="ISession" /> or <see cref="IStatelessSession" /> instance through <see cref="ISessionManager" />,
    /// which is transaction-aware and save you the burden of sharing session or using a singleton.
    /// </para>
    /// </remarks>
    /// <example>
    /// The following sample illustrates how a component can access the session.
    /// <code>
    /// public class MyDao
    /// {
    ///     private ISessionManager _sessionManager;
    ///
    ///     public MyDao(ISessionManager sessionManager)
    ///     {
    ///         _sessionManager = sessionManager;
    ///     }
    ///
    ///     public void Save(Data data)
    ///     {
    ///         using (var session = _sessionManager.OpenSession())
    ///         {
    ///             session.Save(data);
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class NHibernateFacility : AbstractFacility
    {
        public static readonly Type DefaultSessionStoreType = typeof(AsyncLocalSessionStore);
        public static readonly Type DefaultWebSessionStoreType = typeof(WebSessionStore);
        public static readonly bool DefaultUseReflectionOptimizerValue = false;

        private readonly INHibernateFacilityConfiguration _nHibernateFacilityConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateFacility" /> class.
        /// </summary>
        public NHibernateFacility() :
            this(new DefaultConfigurationBuilder())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateFacility" /> class
        /// with the specified <see cref="IConfigurationBuilder" />.
        /// </summary>
        /// <param name="configurationBuilder"></param>
        public NHibernateFacility(IConfigurationBuilder configurationBuilder) :
            this(new NHibernateFacilityConfiguration(configurationBuilder))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateFacility" /> class
        /// with the specified <see cref="IConfigurationBuilder" />.
        /// </summary>
        /// <param name="nHibernateFacilityConfiguration"></param>
        internal NHibernateFacility(INHibernateFacilityConfiguration nHibernateFacilityConfiguration)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(nHibernateFacilityConfiguration);
#else
            if (nHibernateFacilityConfiguration is null)
            {
                throw new ArgumentNullException(nameof(nHibernateFacilityConfiguration));
            }
#endif

            _nHibernateFacilityConfiguration = nHibernateFacilityConfiguration;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Runs custom initialization for the <see cref="NHibernateFacility" />.
        /// </summary>
        /// <remarks>It must be overriden.</remarks>
        protected override void Init()
        {
            if (Kernel.HasComponent(typeof(ILoggerFactory)))
            {
                Logger = Kernel.Resolve<ILoggerFactory>().Create(GetType());
            }

            _nHibernateFacilityConfiguration.Init(Kernel, FacilityConfig);

            AssertHasConfigurationBuilderOrConfigurationBuilderType();
            AssertHasAtLeastOneSessionFactoryConfigured();
            RegisterComponents();
            ConfigureFacility();
        }

        /// <summary>
        /// Registers <see cref="ITransactionManager" />, <see cref="IConfigurationBuilder" />,
        /// <see cref="SessionFactoryResolver" />, <see cref="ISessionStore" />, and <see cref="ISessionManager" />.
        /// </summary>
        protected virtual void RegisterComponents()
        {
            RegisterConfigurationBuilder();
            RegisterTransactionManager();
            RegisterSessionFactoryResolver();
            RegisterSessionStore();
            RegisterSessionInterceptor();
            RegisterSessionManager();
        }

        /// <summary>
        /// Register the default <see cref="IConfigurationBuilder" />,
        /// or (if present) the one specified via "configurationBuilderType" attribute.
        /// </summary>
        private void RegisterConfigurationBuilder()
        {
            if (_nHibernateFacilityConfiguration.HasConfigurationBuilderOnly())
            {
                var configurationBuilder = _nHibernateFacilityConfiguration.GetConfigurationBuilder();

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .Instance(configurationBuilder)
                             .Named(Constants.ConfigurationBuilder_ComponentName));
            }
            else
            {
                var configurationBuilderType = _nHibernateFacilityConfiguration.GetConfigurationBuilderType();

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(configurationBuilderType)
                             .Named(Constants.ConfigurationBuilder_ComponentName));
            }
        }

        /// <summary>
        /// Registers <see cref="DefaultTransactionManager" /> as the default <see cref="ITransactionManager" />.
        /// </summary>
        protected void RegisterTransactionManager()
        {
            if (!Kernel.HasComponent(typeof(ITransactionManager)))
            {
                Logger.Info($"No '{nameof(ITransactionManager)}' registered on kernel, registering default '{nameof(DefaultTransactionManager)}'.");

                Kernel.Register(
                    Component.For<ITransactionManager>()
                             .ImplementedBy<DefaultTransactionManager>()
                             .Named(Constants.TransactionManager_ComponentName));
            }
        }

        /// <summary>
        /// Registers <see cref="SessionFactoryResolver" /> as the default <see cref="ISessionFactory" /> resolver.
        /// </summary>
        protected void RegisterSessionFactoryResolver()
        {
            Kernel.Register(
                Component.For<ISessionFactoryResolver>()
                         .ImplementedBy<SessionFactoryResolver>()
                         .Named(Constants.SessionFactoryResolver_ComponentName)
                         .LifeStyle.Singleton);
        }

        /// <summary>
        /// Registers the configured <see cref="ISessionStore" />.
        /// </summary>
        protected void RegisterSessionStore()
        {
            Kernel.Register(
                Component.For<ISessionStore>()
                         .ImplementedBy(_nHibernateFacilityConfiguration.ResolveSessionStoreType())
                         .Named(Constants.SessionStore_ComponentName));
        }

        protected void RegisterSessionInterceptor()
        {
            //
            //  NOTE:   Naming the following components using Named() method,
            //          especially TransactionInterceptor,
            //          will cause property dependencies of a resolved instance
            //          not being injected in NHibernateFacility.
            //
            Kernel.Register(
                //Component.For<NHibernateSessionInterceptor>()
                //         .Named(Constants.SessionInterceptor_ComponentName),
                Component.For<NHibernateSessionInterceptor>());

            Kernel.ComponentModelBuilder.AddContributor(new NHibernateSessionComponentInspector());
        }

        /// <summary>
        /// Registers <see cref="DefaultSessionManager" /> as the default <see cref="ISessionManager" />.
        /// </summary>
        protected void RegisterSessionManager()
        {
            var defaultFlushMode = _nHibernateFacilityConfiguration.DefaultFlushMode;

            if (!defaultFlushMode.IsNullOrEmpty())
            {
                var configurationNode = new MutableConfiguration(Constants.SessionManager_ComponentName);

                var properties = new MutableConfiguration("parameters");

                properties.Children.Add(new MutableConfiguration(nameof(ISessionManager.DefaultFlushMode), defaultFlushMode));

                configurationNode.Children.Add(properties);

                Kernel.ConfigurationStore.AddComponentConfiguration(Constants.SessionManager_ComponentName, configurationNode);
            }

            Kernel.Register(
                Component.For<ISessionManager>()
                         .ImplementedBy<DefaultSessionManager>()
                         .Named(Constants.SessionManager_ComponentName));
        }

        #region Configuration Methods

        /// <summary>
        /// Configures the facility.
        /// </summary>
        protected void ConfigureFacility()
        {
            ConfigureReflectionOptimizer();

            var sessionFactoryResolver = Kernel.Resolve<ISessionFactoryResolver>();

            var firstSessionFactory = true;

            foreach (var sessionFactoryConfiguration in _nHibernateFacilityConfiguration.SessionFactoryConfigurations)
            {
                ConfigureSessionFactory(sessionFactoryConfiguration, sessionFactoryResolver, firstSessionFactory);

                firstSessionFactory = false;
            }
        }

        /// <summary>
        /// Reads the attribute <c>useReflectionOptimizer</c> and configures the reflection optimizer accordingly.
        /// </summary>
        /// <remarks>
        /// As reported on Jira (FACILITIES-39), the reflection optimizer slow things down,
        /// so it is disabled by default.
        /// You can use the attribute <c>useReflectionOptimizer</c> to turn it on.
        /// </remarks>
        private void ConfigureReflectionOptimizer()
        {
            NHibernate.Cfg.Environment.UseReflectionOptimizer = _nHibernateFacilityConfiguration.ResolveUseReflectionOptimizer();
        }

        /// <summary>
        /// Configures the <see cref="ISessionFactory" />.
        /// </summary>
        /// <param name="sessionFactoryConfiguration">The <see cref="ISessionFactory" /> configuration.</param>
        /// <param name="sessionFactoryResolver">The <see cref="ISessionFactoryResolver" />.</param>
        /// <param name="firstSessionFactory">If set to <see langword="true" />, it's the first <see cref="ISessionFactory" />.</param>
        protected void ConfigureSessionFactory(
            INHibernateFacilitySessionFactoryConfiguration sessionFactoryConfiguration,
            ISessionFactoryResolver sessionFactoryResolver,
            bool firstSessionFactory)
        {
            var id = sessionFactoryConfiguration.Id;

            if (id.IsNullOrEmpty())
            {
                const string Message = $"The '{Constants.SessionFactory_ConfigurationElementName}' node requires the '{nameof(Constants.SessionFactory_Id_ConfigurationElementAttributeName)}' attribute. " +
                                       $"This ID is used as key/name for the '{nameof(ISessionFactory)}' component registered on the container.";
                throw new ConfigurationErrorsException(Message);
            }

            var alias = sessionFactoryConfiguration.Alias;

            if (!firstSessionFactory && alias.IsNullOrEmpty())
            {
                const string Message = $"The '{Constants.SessionFactory_ConfigurationElementName}' node requires the '{nameof(Constants.SessionFactory_Alias_ConfigurationElementAttributeName)}' attribute. " +
                                       $"This alias is used to obtain the '{nameof(ISession)}' implementation from the '{nameof(ISessionManager)}'.";
                throw new ConfigurationErrorsException(Message);
            }
            if (alias.IsNullOrEmpty())
            {
                alias = Constants.DefaultAlias;
            }

            IConfigurationBuilder configurationBuilder;

            var configurationBuilderTypeFullName = sessionFactoryConfiguration.ConfigurationBuilderTypeFullName;

            if (configurationBuilderTypeFullName.IsNullOrEmpty())
            {
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>();
            }
            else
            {
                Type configurationBuilderType = null!;

                try
                {
                    var converter = Kernel.GetConversionManager();

                    configurationBuilderType = converter.PerformConversion<Type>(configurationBuilderTypeFullName);
                }
                catch (ConverterException ex)
                {
                    var message = $"The 'ConfigurationBuilder' of type '{configurationBuilderTypeFullName}' could not be resolved.";
                    throw new FacilityException(message, ex);
                }

                var configurationBuilderType_ComponentName = string.Format(Constants.ConfigurationBuilderType_ComponentNameFormat, id);

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(configurationBuilderType)
                             .Named(configurationBuilderType_ComponentName));
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>(configurationBuilderType_ComponentName);
            }

            //
            //  NOTE:   Extensibility point for passing Castle.Core.Configuration.IConfiguration
            //          to the Castle.Facilities.NHibernateIntegration.IConfigurationBuilder.GetConfiguration() method.
            //
            var facilityConfiguration = sessionFactoryConfiguration.GetFacilityConfiguration();

            var configuration = configurationBuilder.GetConfiguration(facilityConfiguration);

            // Register NHibernate Configuration instance.
            Kernel.Register(
                Component.For<NHibernate.Cfg.Configuration>()
                         .Instance(configuration)
                         .Named($"{id}.cfg"));

            // If an NHibernate SessionFactory-level interceptor was provided, use it.
            if (Kernel.HasComponent(Constants.SessionFactoryInterceptor_ComponentName))
            {
                configuration.Interceptor = Kernel.Resolve<IInterceptor>(Constants.SessionFactoryInterceptor_ComponentName);
            }

            // Register NHibernate ISessionFactory.
            Kernel.Register(
                Component.For<ISessionFactory>()
                         .Named(id)
                         .Activator<SessionFactoryActivator>()
                         .ExtendedProperties(Property.ForKey(Constants.SessionFactory_Configuration_ComponentPropertyName).Eq(configuration))
                         .LifeStyle.Singleton);

            sessionFactoryResolver.RegisterAliasToIdMapping(alias, id);
        }

        #endregion

        #region Helper Methods

        private void AssertHasConfigurationBuilderOrConfigurationBuilderType()
        {
            if (!_nHibernateFacilityConfiguration.HasConfigurationBuilderOrConfigurationBuilderType())
            {
                const string Message = $"At least one of '{nameof(IConfigurationBuilder)}' or '{Constants.ConfigurationBuilderType_ConfigurationElementAttributeName}' is required.";
                throw new ConfigurationErrorsException(Message);
            }
        }

        private void AssertHasAtLeastOneSessionFactoryConfigured()
        {
            if (_nHibernateFacilityConfiguration.HasSessionFactoryConfigurations())
            {
                return;
            }

            if (!_nHibernateFacilityConfiguration.HasSessionFactoriesFacilityConfiguration())
            {
                const string Message = $"At least one '{nameof(ISessionFactory)}' is required.";
                throw new ConfigurationErrorsException(Message);
            }
        }

        #endregion

        #region Fluent Configuration Methods

        /// <summary>
        /// Sets a custom <see cref="IConfigurationBuilder" /> for the facility.
        /// </summary>
        /// <typeparam name="T">The implementation type of the <see cref="IConfigurationBuilder" />.</typeparam>
        /// <returns></returns>
        public NHibernateFacility ConfigurationBuilder<T>()
            where T : IConfigurationBuilder
        {
            return ConfigurationBuilder(typeof(T));
        }

        /// <summary>
        /// Sets a custom <see cref="IConfigurationBuilder" /> for the facility.
        /// </summary>
        /// <param name="configurationBuilderType">The implementation type of the <see cref="IConfigurationBuilder" />.</param>
        /// <returns></returns>
        public NHibernateFacility ConfigurationBuilder(Type configurationBuilderType)
        {
            _nHibernateFacilityConfiguration.SetConfigurationBuilderType(configurationBuilderType);

            return this;
        }

        /// <summary>
        /// Sets a custom <see cref="ISessionStore" /> for the facility.
        /// </summary>
        /// <typeparam name="T">The implementation type of the <see cref="ISessionStore" />.</typeparam>
        /// <returns><see cref="NHibernateFacility" /></returns>
        public NHibernateFacility SessionStore<T>()
            where T : ISessionStore
        {
            _nHibernateFacilityConfiguration.SetSessionStoreType(typeof(T));

            return this;
        }

        /// <summary>
        /// Sets the facility to work on a ASP.NET web context.
        /// </summary>
        /// <returns></returns>
        public NHibernateFacility IsWeb()
        {
            _nHibernateFacilityConfiguration.SetIsWeb();

            return this;
        }

        #endregion
    }

    internal interface INHibernateFacilityConfiguration
    {
        string? DefaultFlushMode { get; set; }

        IEnumerable<INHibernateFacilitySessionFactoryConfiguration> SessionFactoryConfigurations { get; set; }

        void Init(IKernel kernel, CastleConfiguration facilityConfiguration);

        bool HasConfigurationBuilderOrConfigurationBuilderType();

        bool HasConfigurationBuilderOnly();

        IConfigurationBuilder GetConfigurationBuilder();

        //void SetConfigurationBuilder(IConfigurationBuilder configurationBuilder);

        bool HasConfigurationBuilderType();

        Type? GetConfigurationBuilderType();

        void SetConfigurationBuilderType(Type configurationBuilderType);

        Type ResolveSessionStoreType();

        void SetSessionStoreType(Type sessionStoreType);

        void SetIsWeb();

        bool ResolveUseReflectionOptimizer();

        bool HasSessionFactoriesFacilityConfiguration();

        bool HasSessionFactoryConfigurations();
    }

    internal class NHibernateFacilityConfiguration : INHibernateFacilityConfiguration
    {
        private IKernel _kernel = null!;
        private CastleConfiguration _facilityConfiguration = null!;
        private IConfigurationBuilder _configurationBuilder;
        private Type? _configurationBuilderType;
        private Type? _sessionStoreType;
        private bool _isWeb;

        public IEnumerable<INHibernateFacilitySessionFactoryConfiguration> SessionFactoryConfigurations { get; set; }

        public NHibernateFacilityConfiguration(IConfigurationBuilder configurationBuilder)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(configurationBuilder);
#else
            if (configurationBuilder is null)
            {
                throw new ArgumentNullException(nameof(configurationBuilder));
            }
#endif

            _configurationBuilder = configurationBuilder;

            SessionFactoryConfigurations = [];
        }

        public string? DefaultFlushMode { get; set; }

        public void Init(IKernel kernel, CastleConfiguration facilityConfiguration)
        {
            _kernel = kernel;
            _facilityConfiguration = facilityConfiguration;

            if (HasFacilityConfiguration())
            {
                ConfigureWithFacilityConfiguration();
            }
            else
            {
                SessionFactoryConfigurations =
                [
                    new NHibernateFacilitySessionFactoryConfiguration(
                        new MutableConfiguration(Constants.SessionFactory_ConfigurationElementName))
                    {
                        Id = $"{Constants.SessionFactory_ConfigurationElementName}_1",
                    },
                ];
            }
        }

        private void ConfigureWithFacilityConfiguration()
        {
            var configurationBuilderTypeFullName = _facilityConfiguration.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName];

            if (!configurationBuilderTypeFullName.IsNullOrEmpty())
            {
                try
                {
                    var converter = _kernel.GetConversionManager();

                    var configurationBuilderType = converter.PerformConversion<Type>(configurationBuilderTypeFullName);

                    SetConfigurationBuilderType(configurationBuilderType);
                }
                catch (ConverterException ex)
                {
                    var message = $"The 'ConfigurationBuilder' of type '{configurationBuilderTypeFullName}' could not be resolved.";
                    throw new FacilityException(message, ex);
                }
            }

            var sessionStoreTypeFullName = _facilityConfiguration.Attributes[Constants.SessionStoreType_ConfigurationElementAttributeName];

            if (!sessionStoreTypeFullName.IsNullOrEmpty())
            {
                try
                {
                    var converter = _kernel.GetConversionManager();

                    var sessionStoreType = converter.PerformConversion<Type>(sessionStoreTypeFullName);

                    SetSessionStoreType(sessionStoreType);
                }
                catch (ConverterException ex)
                {
                    var message = $"The 'SessionStore' of type '{sessionStoreTypeFullName}' could not be resolved.";
                    throw new FacilityException(message, ex);
                }
            }

            _ = bool.TryParse(_facilityConfiguration.Attributes[Constants.SessionStore_IsWeb_ConfigurationElementAttributeName], out _isWeb);

            DefaultFlushMode = _facilityConfiguration.Attributes[Constants.Session_DefaultFlushMode_ConfigurationElementAttributeName];

            BuildSessionFactoryConfigurations();
        }

        private bool HasFacilityConfiguration()
        {
            return _facilityConfiguration is not null &&
                   _facilityConfiguration.Children.Count > 0;
        }

        public bool HasConfigurationBuilderOrConfigurationBuilderType()
        {
            return HasFacilityConfiguration() ||
                   _configurationBuilder is not null || _configurationBuilderType is not null;
        }

        public bool HasConfigurationBuilderOnly()
        {
            return _configurationBuilder is not null && !HasConfigurationBuilderType();
        }

        public IConfigurationBuilder GetConfigurationBuilder()
        {
            return _configurationBuilder;
        }

        //public void SetConfigurationBuilder(IConfigurationBuilder configurationBuilder)
        //{
        //    _configurationBuilder = configurationBuilder;
        //}

        public bool HasConfigurationBuilderType()
        {
            return _configurationBuilderType is not null;
        }

        public Type? GetConfigurationBuilderType()
        {
            return _configurationBuilderType;
        }

        public void SetConfigurationBuilderType(Type configurationBuilderType)
        {
            if (!typeof(IConfigurationBuilder).IsAssignableFrom(configurationBuilderType))
            {
                var message = $"'{configurationBuilderType.FullName}' must implement '{nameof(IConfigurationBuilder)}'.";
                throw new FacilityException(message);
            }

            //_configurationBuilder = null!;
            _configurationBuilderType = configurationBuilderType;
        }

        public Type ResolveSessionStoreType()
        {
            var sessionStoreType = NHibernateFacility.DefaultSessionStoreType;

            if (_isWeb)
            {
                sessionStoreType = NHibernateFacility.DefaultWebSessionStoreType;
            }

            if (_sessionStoreType is not null)
            {
                sessionStoreType = _sessionStoreType;
            }

            return sessionStoreType;
        }

        public void SetSessionStoreType(Type sessionStoreType)
        {
            if (!typeof(ISessionStore).IsAssignableFrom(sessionStoreType))
            {
                var message = $"'{sessionStoreType.FullName}' must implement '{nameof(ISessionStore)}'.";
                throw new FacilityException(message);
            }

            _sessionStoreType = sessionStoreType;
        }

        public void SetIsWeb()
        {
            _isWeb = true;
        }

        public bool ResolveUseReflectionOptimizer()
        {
            if (HasFacilityConfiguration() &&
                bool.TryParse(_facilityConfiguration.Attributes[Constants.UseReflectionOptimizer_ConfigurationElementAttributeName], out var value))
            {
                return value;
            }

            return NHibernateFacility.DefaultUseReflectionOptimizerValue;
        }

        public bool HasSessionFactoriesFacilityConfiguration()
        {
            CastleConfiguration sessionFactoryNode;

            //
            // <sessionFactories><sessionFactory>...</sessionFactory></sessionFactories>
            //
            var sessionFactoriesNode = _facilityConfiguration.Children[Constants.SessionFactories_ConfigurationElementName];

            if (sessionFactoriesNode is not null)
            {
                sessionFactoryNode = sessionFactoriesNode.Children[Constants.SessionFactory_ConfigurationElementName];

                return sessionFactoryNode is not null;
            }

            //
            // <sessionFactory>...</sessionFactory>
            //
            sessionFactoryNode = _facilityConfiguration.Children[Constants.SessionFactory_ConfigurationElementName];

            return sessionFactoryNode is not null;
        }

        public bool HasSessionFactoryConfigurations()
        {
            return SessionFactoryConfigurations.Any();
        }

        private void BuildSessionFactoryConfigurations()
        {
            var sessionFactoriesNode = _facilityConfiguration.Children[Constants.SessionFactories_ConfigurationElementName];

            var sessionFactoryNodes = sessionFactoriesNode is not null ?
                                      sessionFactoriesNode.Children :       // <sessionFactories><sessionFactory>...</sessionFactory></sessionFactories>
                                      _facilityConfiguration.Children;      // <sessionFactory>...</sessionFactory>

            SessionFactoryConfigurations = sessionFactoryNodes.Select(static configuration => new NHibernateFacilitySessionFactoryConfiguration(configuration));
        }
    }

    public interface INHibernateFacilitySessionFactoryConfiguration
    {
        /// <summary>
        /// Get or sets the <see cref="ISessionFactory" /> ID.
        /// </summary>
        string? Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISessionFactory" /> alias.
        /// </summary>
        string? Alias { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISessionFactory" />'s <see cref="IConfigurationBuilder" /> full type name.
        /// </summary>
        string? ConfigurationBuilderTypeFullName { get; set; }

        /// <summary>
        /// Gets the facility <see cref="CastleConfiguration" /> instance for this <see cref="ISessionFactory" />.
        /// </summary>
        /// <returns></returns>
        CastleConfiguration GetFacilityConfiguration();
    }

    internal class NHibernateFacilitySessionFactoryConfiguration : INHibernateFacilitySessionFactoryConfiguration
    {
        private readonly CastleConfiguration _facilityConfiguration;

        public NHibernateFacilitySessionFactoryConfiguration(CastleConfiguration facilityConfiguration)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(facilityConfiguration);
#else
            if (facilityConfiguration is null)
            {
                throw new ArgumentNullException(nameof(facilityConfiguration));
            }
#endif

            _facilityConfiguration = facilityConfiguration;

            Id = facilityConfiguration.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName]!;
            Alias = facilityConfiguration.Attributes[Constants.SessionFactory_Alias_ConfigurationElementAttributeName]!;
            ConfigurationBuilderTypeFullName = facilityConfiguration.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName]!;
        }

        public string? Id { get; set; }

        public string? Alias { get; set; }

        public string? ConfigurationBuilderTypeFullName { get; set; }

        public CastleConfiguration GetFacilityConfiguration()
        {
            return _facilityConfiguration;
        }
    }
}
