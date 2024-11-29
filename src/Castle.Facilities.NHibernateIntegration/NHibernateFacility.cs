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

        private ILogger _logger = NullLogger.Instance;

        private readonly NHibernateFacilityConfiguration _facilityConfiguration;
        private readonly IConfigurationBuilder _configurationBuilder;
        private Type? _configurationBuilderType;

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
        public NHibernateFacility(IConfigurationBuilder configurationBuilder)
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
            _facilityConfiguration = new NHibernateFacilityConfiguration(configurationBuilder);
        }

        /// <summary>
        /// Runs custom initialization for the <see cref="NHibernateFacility" />.
        /// </summary>
        /// <remarks>It must be overriden.</remarks>
        protected override void Init()
        {
            if (Kernel.HasComponent(typeof(ILoggerFactory)))
            {
                _logger = Kernel.Resolve<ILoggerFactory>().Create(GetType());
            }

            _facilityConfiguration.Init(Kernel, FacilityConfig);

            AssertHasConfiguration();
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

            RegisterTransactionManager();
            RegisterConfigurationBuilder();
            RegisterSessionFactoryResolver();
            RegisterSessionStore();
            RegisterSessionManager();
        }

        /// <summary>
        /// Registers <see cref="DefaultTransactionManager" /> as the default <see cref="ITransactionManager" />.
        /// </summary>
        protected void RegisterTransactionManager()
        {
            if (!Kernel.HasComponent(typeof(ITransactionManager)))
            {
                _logger.Info($"No '{nameof(ITransactionManager)}' registered on kernel, registering default '{nameof(DefaultTransactionManager)}'.");

                Kernel.Register(
                    Component.For<ITransactionManager>()
                             .ImplementedBy<DefaultTransactionManager>()
                             .Named(Constants.TransactionManager_ComponentName));
            }
        }

        /// <summary>
        /// Register the default <see cref="IConfigurationBuilder" />,
        /// or (if present) the one specified via "configurationBuilderType" attribute.
        /// </summary>
        private void RegisterConfigurationBuilder()
        {
            if (!_facilityConfiguration.HasConcreteConfigurationBuilder())
            {
                _configurationBuilderType = _facilityConfiguration.GetConfigurationBuilderType();

                if (_facilityConfiguration.HasConfigurationBuilderType())
                {
                    if (!typeof(IConfigurationBuilder).IsAssignableFrom(_configurationBuilderType))
                    {
                        var message = $"'{_configurationBuilderType!.FullName}' must implement the '{nameof(IConfigurationBuilder)}'.";
                        throw new FacilityException(message);
                    }
                }

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(_configurationBuilderType)
                             .Named(Constants.ConfigurationBuilder_ComponentName));
            }
            else
            {
                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .Instance(_configurationBuilder)
                             .Named(Constants.ConfigurationBuilder_ComponentName));
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
                         .ImplementedBy(_facilityConfiguration.GetSessionStoreType())
                         .Named(Constants.SessionStore_ComponentName));
        }

        /// <summary>
        /// Registers <see cref="DefaultSessionManager" /> as the default <see cref="ISessionManager" />.
        /// </summary>
        protected void RegisterSessionManager()
        {
            var defaultFlushMode = _facilityConfiguration.DefaultFlushMode;
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
            foreach (var sessionFactoryConfiguration in _facilityConfiguration.SessionFactoryFacilityConfigurations)
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
            NHibernate.Cfg.Environment.UseReflectionOptimizer = _facilityConfiguration.GetUseReflectionOptimizerValue();
        }

        /// <summary>
        /// Configures the <see cref="ISessionFactory" />.
        /// </summary>
        /// <param name="facilityConfiguration">The <see cref="ISessionFactory" /> configuration.</param>
        /// <param name="sessionFactoryResolver">The <see cref="ISessionFactoryResolver" />.</param>
        /// <param name="firstSessionFactory">If set to <see langword="true" />, it's the first <see cref="ISessionFactory" />.</param>
        protected void ConfigureSessionFactory(NHibernateSessionFactoryFacilityConfiguration facilityConfiguration, ISessionFactoryResolver sessionFactoryResolver, bool firstSessionFactory)
        {
            var id = facilityConfiguration.Id;
            if (id.IsNullOrEmpty())
            {
                var message = "You must provide a valid 'id' attribute for the 'sessionFactory' node. " +
                              $"This ID is used as key/name for the '{nameof(ISessionFactory)}' component registered on the container.";
                throw new ConfigurationErrorsException(message);
            }

            var alias = facilityConfiguration.Alias;
            if (!firstSessionFactory && alias.IsNullOrEmpty())
            {
                var message = "You must provide a valid 'alias' attribute for the 'sessionFactory' node. " +
                              $"This alias is used to obtain the '{nameof(ISession)}' implementation from the '{nameof(ISessionManager)}'.";
                throw new ConfigurationErrorsException(message);
            }
            if (alias.IsNullOrEmpty())
            {
                alias = Constants.DefaultAlias;
            }

            var configurationBuilderTypeFullName = facilityConfiguration.ConfigurationBuilderTypeFullName;
            IConfigurationBuilder configurationBuilder;
            if (configurationBuilderTypeFullName.IsNullOrEmpty())
            {
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>();
            }
            else
            {
                var configurationBuilderType_ComponentName = string.Format(Constants.ConfigurationBuilderType_ComponentNameFormat, id);

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(Type.GetType(configurationBuilderTypeFullName))
                             .Named(configurationBuilderType_ComponentName));
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>(configurationBuilderType_ComponentName);
            }

            var configuration = configurationBuilder.GetConfiguration(facilityConfiguration.GetConfiguration());

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

        private void AssertHasConfiguration()
        {
            if (!_facilityConfiguration.IsValid())
            {
                var message = $"The '{nameof(NHibernateFacility)}' requires configuration.";
                throw new ConfigurationErrorsException(message);
            }
        }

        private void AssertHasAtLeastOneSessionFactoryConfigured()
        {
            if (_facilityConfiguration.HasValidSessionFactoryConfiguration())
            {
                return;
            }

            var facilityConfiguration = FacilityConfig.Children[Constants.SessionFactory_ConfigurationElementName];
            if (facilityConfiguration is null)
            {
                var message = $"You need to configure at least one '{nameof(ISessionFactory)}' to use the '{nameof(NHibernateFacility)}'.";
                throw new ConfigurationErrorsException(message);
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
            _facilityConfiguration.SetConfigurationBuilderType(configurationBuilderType);

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
            _facilityConfiguration.SetSessionStoreType(typeof(T));

            return this;
        }

        /// <summary>
        /// Sets the facility to work on a ASP.NET web context.
        /// </summary>
        /// <returns></returns>
        public NHibernateFacility IsWeb()
        {
            _facilityConfiguration.IsWeb();

            return this;
        }

        #endregion
    }

    internal class NHibernateFacilityConfiguration
    {
        private IKernel? _kernel;
        private IConfigurationBuilder? _configurationBuilder;
        private Type? _configurationBuilderType;
        private IConfiguration? _configuration;
        private Type? _sessionStoreType;
        private bool _isWeb;

        public IEnumerable<NHibernateSessionFactoryFacilityConfiguration> SessionFactoryFacilityConfigurations { get; set; }

        public NHibernateFacilityConfiguration(IConfigurationBuilder configurationBuilder)
        {
            _configurationBuilder = configurationBuilder;

            SessionFactoryFacilityConfigurations = Enumerable.Empty<NHibernateSessionFactoryFacilityConfiguration>();
        }

        public string? DefaultFlushMode { get; set; }

        public void Init(IKernel kernel, IConfiguration configuration)
        {
            _kernel = kernel;
            _configuration = configuration;

            if (ConfigurationIsValid())
            {
                ConfigureWithExternalConfiguration();
            }
            else
            {
                SessionFactoryFacilityConfigurations = new[]
                {
                    new NHibernateSessionFactoryFacilityConfiguration(new MutableConfiguration(Constants.SessionFactory_ConfigurationElementName))
                    {
                        Id = $"{Constants.SessionFactory_ConfigurationElementName}_1",
                    },
                };
            }
        }

        private void ConfigureWithExternalConfiguration()
        {
            var configurationBuilderTypeFullName = _configuration!.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName];
            if (!configurationBuilderTypeFullName.IsNullOrEmpty())
            {
                try
                {
                    var converter = (IConversionManager) _kernel!.GetSubSystem(SubSystemConstants.ConversionManagerKey);
                    SetConfigurationBuilderType(converter.PerformConversion<Type>(configurationBuilderTypeFullName));
                }
                catch (ConverterException)
                {
                    var message = $"'ConfigurationBuilder' of type '{configurationBuilderTypeFullName}' is invalid or can not be found.";
                    throw new FacilityException(message);
                }
            }

            BuildSessionFactoryConfigurations();

            var sessionStoreTypeFullName = _configuration.Attributes[Constants.SessionStoreType_ConfigurationElementAttributeName];
            if (!sessionStoreTypeFullName.IsNullOrEmpty())
            {
                try
                {
                    var converter = (IConversionManager) _kernel!.GetSubSystem(SubSystemConstants.ConversionManagerKey);
                    SetSessionStoreType(converter.PerformConversion<Type>(sessionStoreTypeFullName));
                }
                catch (ConverterException)
                {
                    var message = $"'SessionStore' of type '{sessionStoreTypeFullName}' is invalid or can not be found.";
                    throw new FacilityException(message);
                }
            }

            DefaultFlushMode = _configuration.Attributes[Constants.Session_DefaultFlushMode_ConfigurationElementAttributeName];

            _ = bool.TryParse(_configuration.Attributes[Constants.SessionStore_IsWeb_ConfigurationElementAttributeName], out _isWeb);
        }

        public void SetConfigurationBuilder(IConfigurationBuilder configurationBuilder)
        {
            _configurationBuilder = configurationBuilder;
        }

        public void SetConfigurationBuilderType(Type configurationBuilderType)
        {
            _configurationBuilder = null;
            _configurationBuilderType = configurationBuilderType;
        }

        private bool ConfigurationIsValid()
        {
            return _configuration is not null &&
                   _configuration.Children.Count > 0;
        }

        public bool IsValid()
        {
            return _configuration is not null ||
                   _configurationBuilder is not null || _configurationBuilderType is not null;
        }

        public bool HasValidSessionFactoryConfiguration()
        {
            return SessionFactoryFacilityConfigurations.Any();
        }

        public bool HasConcreteConfigurationBuilder()
        {
            return _configurationBuilder is not null && !HasConfigurationBuilderType();
        }

        public bool HasConfigurationBuilderType()
        {
            return _configurationBuilderType is not null;
        }

        public Type? GetConfigurationBuilderType()
        {
            return _configurationBuilderType;
        }

        public bool GetUseReflectionOptimizerValue()
        {
            if (_configuration is not null)
            {
                if (bool.TryParse(_configuration.Attributes[Constants.UseReflectionOptimizer_ConfigurationElementAttributeName], out var value))
                {
                    return value;
                }
            }

            return NHibernateFacility.DefaultUseReflectionOptimizerValue;
        }

        private void BuildSessionFactoryConfigurations()
        {
            SessionFactoryFacilityConfigurations =
                _configuration!.Children
                               .Select(configuration => new NHibernateSessionFactoryFacilityConfiguration(configuration));
        }

        public Type GetSessionStoreType()
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
                var message = $"'{sessionStoreType.FullName}' must implement the '{nameof(ISessionStore)}'.";
                throw new FacilityException(message);
            }

            _sessionStoreType = sessionStoreType;
        }

        public void IsWeb()
        {
            _isWeb = true;
        }
    }

    public class NHibernateSessionFactoryFacilityConfiguration
    {
        private readonly IConfiguration _configuration;

        public NHibernateSessionFactoryFacilityConfiguration(IConfiguration configuration)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(configuration);
#else
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
#endif

            _configuration = configuration;

            Id = configuration.Attributes[Constants.SessionFactory_Id_ConfigurationElementAttributeName]!;
            Alias = configuration.Attributes[Constants.SessionFactory_Alias_ConfigurationElementAttributeName]!;
            ConfigurationBuilderTypeFullName = configuration.Attributes[Constants.ConfigurationBuilderType_ConfigurationElementAttributeName]!;
        }

        /// <summary>
        /// Get or sets the <see cref="ISessionFactory" /> ID.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISessionFactory" /> alias.
        /// </summary>
        public string? Alias { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ISessionFactory" />'s <see cref="IConfigurationBuilder" /> full type name.
        /// </summary>
        public string? ConfigurationBuilderTypeFullName { get; set; }

        /// <summary>
        /// Builds an <see cref="IConfiguration" /> instance for this <see cref="ISessionFactory" />.
        /// </summary>
        /// <returns></returns>
        public IConfiguration GetConfiguration()
        {
            return _configuration;
        }
    }
}
