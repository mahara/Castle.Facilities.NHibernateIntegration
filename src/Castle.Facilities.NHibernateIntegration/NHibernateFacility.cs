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

namespace Castle.Facilities.NHibernateIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;

    using Builders;

    using Core.Configuration;
    using Core.Logging;

    using Internal;

    using MicroKernel;
    using MicroKernel.Facilities;
    using MicroKernel.Registration;
    using MicroKernel.SubSystems.Conversion;

    using NHibernate;

    using Services.Transaction;

    using SessionStores;

    using IInterceptor = NHibernate.IInterceptor;
    using ILogger = Core.Logging.ILogger;
    using ILoggerFactory = Core.Logging.ILoggerFactory;

    /// <summary>
    /// Provides a basic level of integration with the NHibernate project.
    /// </summary>
    /// <remarks>
    /// This facility allows components to gain access to the NHibernate's objects:
    /// <list type="bullet">
    /// <item><description>NHibernate.Cfg.Configuration</description></item>
    /// <item><description>NHibernate.ISessionFactory</description></item>
    /// </list>
    /// <para>
    /// It also allow you to obtain the ISession instance through the component <see cref="ISessionManager" />,
    /// which is transaction aware and save you the burden of sharing session or using a singleton.
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
    ///         using(ISession session = _sessionManager.OpenSession())
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

        internal const string ConfigurationBuilderConfigurationKey = "configurationBuilder";
        internal const string SessionFactoryIdConfigurationKey = "id";
        internal const string SessionFactoryAliasConfigurationKey = "alias";
        internal const string IsWebConfigurationKey = "isWeb";
        internal const string CustomSessionStoreConfigurationKey = "customStore";
        internal const string DefaultFlushModeConfigurationKey = "defaultFlushMode";

        private const string DefaultConfigurationBuilderKey = "nhfacility.configuration.builder";
        private const string TransactionManagerKey = "nhibernate.transaction.manager";
        private const string SessionFactoryResolverKey = "nhfacility.sessionfactory.resolver";
        private const string SessionInterceptorKey = "nhibernate.sessionfactory.interceptor";
        private const string SessionStoreKey = "nhfacility.sessionstore";
        private const string SessionManagerKey = "nhfacility.sessionmanager";
        private const string ConfigurationBuilderForFactoryFormat = "{0}.configurationBuilder";

        private ILogger _logger = NullLogger.Instance;

        private readonly IConfigurationBuilder _configurationBuilder;
        private Type _customConfigurationBuilderType;
        private readonly NHibernateFacilityConfiguration _facilityConfiguration;

        /// <summary>
        /// Instantiates the facility with the specified configuration builder.
        /// </summary>
        /// <param name="configurationBuilder"></param>
        public NHibernateFacility(IConfigurationBuilder configurationBuilder)
        {
            _configurationBuilder = configurationBuilder;
            _facilityConfiguration = new NHibernateFacilityConfiguration(configurationBuilder);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NHibernateFacility" /> class.
        /// </summary>
        public NHibernateFacility() : this(new DefaultConfigurationBuilder())
        {
        }

        /// <summary>
        /// The custom initialization for the Facility.
        /// </summary>
        /// <remarks>It must be overriden.</remarks>
        protected override void Init()
        {
            if (Kernel.HasComponent(typeof(ILoggerFactory)))
            {
                _logger = Kernel.Resolve<ILoggerFactory>().Create(GetType());
            }

            _facilityConfiguration.Init(Kernel, FacilityConfig);

            AssertHasConfig();
            AssertHasAtLeastOneFactoryConfigured();
            RegisterComponents();
            ConfigureFacility();
        }

        #region Set up of components

        /// <summary>
        /// Registers the session factory resolver, the session store,
        /// the session manager, and the transaction manager.
        /// </summary>
        protected virtual void RegisterComponents()
        {
            //Kernel.Register(Component.For<NHibernateSessionInterceptor>().Named("nhsession.interceptor"));
            Kernel.Register(Component.For<NHibernateSessionInterceptor>());
            Kernel.ComponentModelBuilder.AddContributor(new NHibernateSessionComponentInspector());

            RegisterDefaultConfigurationBuilder();
            RegisterSessionFactoryResolver();
            RegisterSessionStore();
            RegisterSessionManager();
            RegisterTransactionManager();
        }

        /// <summary>
        /// Register <see cref="IConfigurationBuilder" /> the default ConfigurationBuilder
        /// or (if present) the one specified via "configurationBuilder" attribute.
        /// </summary>
        private void RegisterDefaultConfigurationBuilder()
        {
            if (!_facilityConfiguration.HasConcreteConfigurationBuilder())
            {
                _customConfigurationBuilderType = _facilityConfiguration.GetConfigurationBuilderType();

                if (_facilityConfiguration.HasConfigurationBuilderType())
                {
                    if (!typeof(IConfigurationBuilder).IsAssignableFrom(_customConfigurationBuilderType))
                    {
                        throw new FacilityException(
                            string.Format(
                                "ConfigurationBuilder type '{0}' is invalid. The type must implement the IConfigurationBuilder contract.",
                                _customConfigurationBuilderType.FullName));
                    }
                }

                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(_customConfigurationBuilderType)
                             .Named(DefaultConfigurationBuilderKey));
            }
            else
            {
                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .Instance(_configurationBuilder)
                             .Named(DefaultConfigurationBuilderKey));
            }
        }

        /// <summary>
        /// Registers <see cref="SessionFactoryResolver" /> as the session factory resolver.
        /// </summary>
        protected void RegisterSessionFactoryResolver()
        {
            Kernel.Register(
                Component.For<ISessionFactoryResolver>()
                         .ImplementedBy<SessionFactoryResolver>()
                         .Named(SessionFactoryResolverKey)
                         .LifeStyle.Singleton);
        }

        /// <summary>
        /// Registers the configured session store.
        /// </summary>
        protected void RegisterSessionStore()
        {
            Kernel.Register(
                Component.For<ISessionStore>()
                         .ImplementedBy(_facilityConfiguration.GetSessionStoreType())
                         .Named(SessionStoreKey));
        }

        /// <summary>
        /// Registers <see cref="DefaultSessionManager" /> as the session manager.
        /// </summary>
        protected void RegisterSessionManager()
        {
            var defaultFlushMode = _facilityConfiguration.FlushMode;

            if (!string.IsNullOrEmpty(defaultFlushMode))
            {
                var configNode = new MutableConfiguration(SessionManagerKey);

                IConfiguration properties = new MutableConfiguration("parameters");
                configNode.Children.Add(properties);

                properties.Children.Add(new MutableConfiguration("DefaultFlushMode", defaultFlushMode));

                Kernel.ConfigurationStore.AddComponentConfiguration(SessionManagerKey, configNode);
            }

            Kernel.Register(
                Component.For<ISessionManager>()
                         .ImplementedBy<DefaultSessionManager>()
                         .Named(SessionManagerKey));
        }

        /// <summary>
        /// Registers <see cref="DefaultTransactionManager" /> as the transaction manager.
        /// </summary>
        protected void RegisterTransactionManager()
        {
            if (!Kernel.HasComponent(typeof(ITransactionManager)))
            {
                _logger.Info($"No {nameof(ITransactionManager)} implementation registered on Kernel, registering default {nameof(ITransactionManager)} implementation.");

                Kernel.Register(
                    Component.For<ITransactionManager>()
                             .ImplementedBy<DefaultTransactionManager>()
                             .Named(TransactionManagerKey));
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Configures the facility.
        /// </summary>
        protected void ConfigureFacility()
        {
            var sessionFactoryResolver = Kernel.Resolve<ISessionFactoryResolver>();

            ConfigureReflectionOptimizer();

            var firstFactory = true;

            foreach (var factoryConfig in _facilityConfiguration.Factories)
            {
                ConfigureFactories(factoryConfig, sessionFactoryResolver, firstFactory);

                firstFactory = false;
            }
        }

        /// <summary>
        /// Reads the attribute <c>useReflectionOptimizer</c>
        /// and configure the reflection optimizer accordingly.
        /// </summary>
        /// <remarks>
        /// As reported on Jira (FACILITIES-39), the reflection optimizer slow things down.
        /// So by default it will be disabled.
        /// You can use the attribute <c>useReflectionOptimizer</c> to turn it on.
        /// </remarks>
        private void ConfigureReflectionOptimizer()
        {
            NHibernate.Cfg.Environment.UseReflectionOptimizer = _facilityConfiguration.ShouldUseReflectionOptimizer();
        }

        /// <summary>
        /// Configures the factories.
        /// </summary>
        /// <param name="factoryConfiguration">The config.</param>
        /// <param name="sessionFactoryResolver">The session factory resolver.</param>
        /// <param name="firstFactory">if set to <c>true</c> [first factory].</param>
        protected void ConfigureFactories(NHibernateFactoryConfiguration factoryConfiguration, ISessionFactoryResolver sessionFactoryResolver, bool firstFactory)
        {
            var id = factoryConfiguration.Id;

            if (string.IsNullOrEmpty(id))
            {
                var message = "You must provide a valid 'id' attribute for the 'factory' node. " +
                              "This id is used as key for the ISessionFactory component registered on the container.";
                throw new ConfigurationErrorsException(message);
            }

            var alias = factoryConfiguration.Alias;

            if (!firstFactory && string.IsNullOrEmpty(alias))
            {
                var message = "You must provide a valid 'alias' attribute for the 'factory' node. " +
                              "This id is used to obtain the ISession implementation from the SessionManager.";
                throw new ConfigurationErrorsException(message);
            }

            if (string.IsNullOrEmpty(alias))
            {
                alias = Constants.DefaultAlias;
            }

            var configurationBuilderType = factoryConfiguration.ConfigurationBuilderType;
            var configurationBuilderKey = string.Format(ConfigurationBuilderForFactoryFormat, id);
            IConfigurationBuilder configurationBuilder;
            if (string.IsNullOrEmpty(configurationBuilderType))
            {
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>();
            }
            else
            {
                Kernel.Register(
                    Component.For<IConfigurationBuilder>()
                             .ImplementedBy(Type.GetType(configurationBuilderType))
                             .Named(configurationBuilderKey));
                configurationBuilder = Kernel.Resolve<IConfigurationBuilder>(configurationBuilderKey);
            }

            var configuration = configurationBuilder.GetConfiguration(factoryConfiguration.GetConfiguration());

            // Registers the Configuration object.
            Kernel.Register(
                Component.For<NHibernate.Cfg.Configuration>()
                         .Instance(configuration)
                         .Named(string.Format("{0}.cfg", id)));

            // If a Session Factory level interceptor was provided, we use it.
            if (Kernel.HasComponent(SessionInterceptorKey))
            {
                configuration.Interceptor = Kernel.Resolve<IInterceptor>(SessionInterceptorKey);
            }

            // Registers the ISessionFactory as a component.
            Kernel.Register(
                Component.For<ISessionFactory>()
                         .Named(id)
                         .Activator<SessionFactoryActivator>()
                         .ExtendedProperties(Property.ForKey(Constants.SessionFactoryConfiguration).Eq(configuration))
                         .LifeStyle.Singleton);

            sessionFactoryResolver.RegisterAliasComponentIdMapping(alias, id);
        }

        #endregion

        #region Helper methods

        private void AssertHasAtLeastOneFactoryConfigured()
        {
            if (_facilityConfiguration.HasValidFactory())
            {
                return;
            }

            var factoriesConfig = FacilityConfig.Children["factory"];
            if (factoriesConfig == null)
            {
                var message = $"You need to configure at least one factory to use the {nameof(NHibernateFacility)}.";
                throw new ConfigurationErrorsException(message);
            }
        }

        private void AssertHasConfig()
        {
            if (!_facilityConfiguration.IsValid())
            {
                var message = $"The {nameof(NHibernateFacility)} requires configuration.";
                throw new ConfigurationErrorsException(message);
            }
        }

        #endregion

        #region FluentConfiguration

        /// <summary>
        /// Sets a custom <see cref="IConfigurationBuilder" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public NHibernateFacility ConfigurationBuilder<T>()
            where T : IConfigurationBuilder
        {
            return ConfigurationBuilder(typeof(T));
        }

        /// <summary>
        /// Sets a custom <see cref="IConfigurationBuilder" />.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public NHibernateFacility ConfigurationBuilder(Type type)
        {
            _facilityConfiguration.ConfigurationBuilder(type);

            return this;
        }

        /// <summary>
        /// Sets the facility to work on a web context.
        /// </summary>
        /// <returns></returns>
        public NHibernateFacility IsWeb()
        {
            _facilityConfiguration.IsWeb();

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
            _facilityConfiguration.SessionStore(typeof(T));

            return this;
        }

        #endregion
    }

    internal class NHibernateFacilityConfiguration
    {
        private IKernel _kernel;
        private IConfiguration _facilityConfiguration;
        private IConfigurationBuilder _configurationBuilder;
        private Type _configurationBuilderType;
        private Type _customSessionStoreType;
        private bool _isWeb;
        private readonly bool _useReflectionOptimizer = false;

        public IEnumerable<NHibernateFactoryConfiguration> Factories { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="configurationBuilder"></param>
        public NHibernateFacilityConfiguration(IConfigurationBuilder configurationBuilder)
        {
            _configurationBuilder = configurationBuilder;

            Factories = Enumerable.Empty<NHibernateFactoryConfiguration>();
        }

        public bool OnWeb =>
            _isWeb;

        public string FlushMode { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="kernel"></param>
        /// <param name="facilityConfiguration"></param>
        public void Init(IKernel kernel, IConfiguration facilityConfiguration)
        {
            _kernel = kernel;
            _facilityConfiguration = facilityConfiguration;

            if (ConfigurationIsValid())
            {
                ConfigureWithExternalConfiguration();
            }
            else
            {
                Factories =
                    new[]
                    {
                        new NHibernateFactoryConfiguration(new MutableConfiguration("factory"))
                        {
                            Id = "factory_1"
                        }
                    };
            }
        }

        private void ConfigureWithExternalConfiguration()
        {
            var builder = _facilityConfiguration.Attributes[NHibernateFacility.ConfigurationBuilderConfigurationKey];

            if (!string.IsNullOrEmpty(builder))
            {
                var converter = (IConversionManager) _kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);

                try
                {
                    ConfigurationBuilder((Type) converter.PerformConversion(builder, typeof(Type)));
                }
                catch (ConverterException)
                {
                    throw new FacilityException(
                        string.Format(
                            "ConfigurationBuilder type '{0}' is invalid or not found.",
                            builder));
                }
            }

            BuildFactories();

            if (_facilityConfiguration.Attributes[NHibernateFacility.CustomSessionStoreConfigurationKey] != null)
            {
                var customStoreType = _facilityConfiguration.Attributes[NHibernateFacility.CustomSessionStoreConfigurationKey];
                var converter = (ITypeConverter) _kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);
                SessionStore((Type) converter.PerformConversion(customStoreType, typeof(Type)));
            }

            FlushMode = _facilityConfiguration.Attributes[NHibernateFacility.DefaultFlushModeConfigurationKey];

            bool.TryParse(_facilityConfiguration.Attributes[NHibernateFacility.IsWebConfigurationKey], out _isWeb);
        }

        private bool ConfigurationIsValid()
        {
            return _facilityConfiguration != null && _facilityConfiguration.Children.Count > 0;
        }

        private void BuildFactories()
        {
            Factories =
                _facilityConfiguration.Children
                                      .Select(config => new NHibernateFactoryConfiguration(config));
        }

        public void ConfigurationBuilder(Type type)
        {
            _configurationBuilder = null;
            _configurationBuilderType = type;
        }

        public void SessionStore(Type type)
        {
            if (!typeof(ISessionStore).IsAssignableFrom(type))
            {
                var message = $"The specified customSessionStore type '{type}' " +
                              $"does not implement the {nameof(ISessionStore)} interface.";
                throw new ConfigurationErrorsException(message);
            }

            _customSessionStoreType = type;
        }

        public void ConfigurationBuilder(IConfigurationBuilder configurationBuilder)
        {
            _configurationBuilder = configurationBuilder;
        }

        public void IsWeb()
        {
            _isWeb = true;
        }

        public bool IsValid()
        {
            return _facilityConfiguration != null
                || _configurationBuilder != null
                || _configurationBuilderType != null;
        }

        public bool HasValidFactory()
        {
            return Factories.Count() > 0;
        }

        public bool ShouldUseReflectionOptimizer()
        {
            if (_facilityConfiguration != null)
            {
                if (bool.TryParse(_facilityConfiguration.Attributes["useReflectionOptimizer"], out var result))
                {
                    return result;
                }

                return false;
            }

            return _useReflectionOptimizer;
        }

        public bool HasConcreteConfigurationBuilder()
        {
            return _configurationBuilder != null && !HasConfigurationBuilderType();
        }

        public Type GetConfigurationBuilderType()
        {
            return _configurationBuilderType;
        }

        public bool HasConfigurationBuilderType()
        {
            return _configurationBuilderType != null;
        }

        public Type GetSessionStoreType()
        {
            var sessionStoreType = NHibernateFacility.DefaultSessionStoreType;

            if (_isWeb)
            {
                sessionStoreType = typeof(WebSessionStore);
            }

            if (_customSessionStoreType != null)
            {
                sessionStoreType = _customSessionStoreType;
            }

            return sessionStoreType;
        }
    }

    /// <summary>
    /// </summary>
    public class NHibernateFactoryConfiguration
    {
        private readonly IConfiguration _facilityConfiguration;

        /// <summary>
        /// </summary>
        /// <param name="facilityConfiguration"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public NHibernateFactoryConfiguration(IConfiguration facilityConfiguration)
        {
            _facilityConfiguration =
                facilityConfiguration ??
                throw new ArgumentNullException(nameof(facilityConfiguration));

            Id = facilityConfiguration.Attributes[NHibernateFacility.SessionFactoryIdConfigurationKey];
            Alias = facilityConfiguration.Attributes[NHibernateFacility.SessionFactoryAliasConfigurationKey];
            ConfigurationBuilderType = facilityConfiguration.Attributes[NHibernateFacility.ConfigurationBuilderConfigurationKey];
        }

        /// <summary>
        /// Get or sets the factory Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the factory Alias.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the factory ConfigurationBuilder.
        /// </summary>
        public string ConfigurationBuilderType { get; set; }

        /// <summary>
        /// Constructs an IConfiguration instance for this factory.
        /// </summary>
        /// <returns></returns>
        public IConfiguration GetConfiguration()
        {
            return _facilityConfiguration;
        }
    }
}