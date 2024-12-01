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

namespace Castle.Facilities.NHibernateIntegration;

using System.Configuration;

using Castle.Core.Configuration;
using Castle.Core.Logging;
using Castle.Facilities.NHibernateIntegration.Builders;
using Castle.Facilities.NHibernateIntegration.Internal;
using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.MicroKernel;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Conversion;
using Castle.Services.Transaction;

using NHibernate;

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

    internal const string ConfigurationBuilderConfigurationKey = "configurationBuilder";
    internal const string UseReflectionOptimizerConfigurationKey = "useReflectionOptimizer";
    internal const string DefaultFlushModeConfigurationKey = "defaultFlushMode";
    internal const string IsWebConfigurationKey = "isWeb";
    internal const string SessionFactoryIdConfigurationKey = "id";
    internal const string SessionFactoryAliasConfigurationKey = "alias";
    internal const string SessionStoreConfigurationKey = "sessionStore";

    private const string ConfigurationBuilderKey = "nhfacility.configuration.builder";
    private const string ConfigurationBuilderFactoryKeyFormat = "{0}.configurationBuilder";
    private const string TransactionManagerKey = "nhfacility.transaction.manager";
    private const string SessionFactoryResolverKey = "nhfacility.sessionfactory.resolver";
    private const string SessionInterceptorKey = "nhibernate.sessionfactory.interceptor";
    private const string SessionStoreKey = "nhfacility.session.store";
    private const string SessionManagerKey = "nhfacility.session.manager";

    private ILogger _logger = NullLogger.Instance;

    private readonly IConfigurationBuilder _configurationBuilder;
    private Type? _configurationBuilderType;
    private readonly NHibernateFacilityConfiguration _facilityConfiguration;

    /// <summary>
    /// Instantiates the facility with the specified configuration builder.
    /// </summary>
    /// <param name="configurationBuilder"></param>
    public NHibernateFacility(IConfigurationBuilder configurationBuilder)
    {
        _configurationBuilder = configurationBuilder ??
                                throw new ArgumentNullException(nameof(configurationBuilder));
        _facilityConfiguration = new NHibernateFacilityConfiguration(configurationBuilder);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NHibernateFacility" /> class.
    /// </summary>
    public NHibernateFacility() :
        this(new DefaultConfigurationBuilder())
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

        AssertHasConfiguration();
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
        if (!_facilityConfiguration.HasConcreteConfigurationBuilder)
        {
            _configurationBuilderType = _facilityConfiguration.GetConfigurationBuilderType();

            if (_facilityConfiguration.HasConfigurationBuilderType)
            {
                if (!typeof(IConfigurationBuilder).IsAssignableFrom(_configurationBuilderType))
                {
                    throw new FacilityException(
                        $"ConfigurationBuilder type '{_configurationBuilderType.FullName}' is invalid. The type must implement the IConfigurationBuilder contract.");
                }
            }

            Kernel.Register(
                Component.For<IConfigurationBuilder>()
                         .ImplementedBy(_configurationBuilderType)
                         .Named(ConfigurationBuilderKey));
        }
        else
        {
            Kernel.Register(
                Component.For<IConfigurationBuilder>()
                         .Instance(_configurationBuilder)
                         .Named(ConfigurationBuilderKey));
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
        var defaultFlushMode = _facilityConfiguration.DefaultFlushMode;

        if (!string.IsNullOrEmpty(defaultFlushMode))
        {
            var configNode = new MutableConfiguration(SessionManagerKey);

            var properties = new MutableConfiguration("parameters");
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
            _logger.Info($"No '{nameof(ITransactionManager)}' implementation registered on Kernel, registering default '{nameof(ITransactionManager)}' implementation.");

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

        foreach (var factoryConfiguration in _facilityConfiguration.FactoryConfigurations)
        {
            ConfigureFactories(factoryConfiguration, sessionFactoryResolver, firstFactory);

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
        NHibernate.Cfg.Environment.UseReflectionOptimizer = _facilityConfiguration.UseReflectionOptimizer;
    }

    /// <summary>
    /// Configures the factories.
    /// </summary>
    /// <param name="factoryConfiguration">The config.</param>
    /// <param name="sessionFactoryResolver">The session factory resolver.</param>
    /// <param name="firstFactory">if set to <c>true</c> [first factory].</param>
    protected void ConfigureFactories(NHibernateFactoryConfiguration factoryConfiguration,
                                      ISessionFactoryResolver sessionFactoryResolver,
                                      bool firstFactory)
    {
        var id = factoryConfiguration.Id;

        if (string.IsNullOrEmpty(id))
        {
            var message = "You must provide a valid 'id' attribute for the 'factory' node. " +
                          "This id is used as key for the 'ISessionFactory' component registered on the container.";
            throw new ConfigurationErrorsException(message);
        }

        var alias = factoryConfiguration.Alias;

        if (!firstFactory && string.IsNullOrEmpty(alias))
        {
            var message = "You must provide a valid 'alias' attribute for the 'factory' node. " +
                          "This id is used to obtain the 'ISession' implementation from the 'SessionManager'.";
            throw new ConfigurationErrorsException(message);
        }

        if (string.IsNullOrEmpty(alias))
        {
            alias = Constants.DefaultAlias;
        }

        var configurationBuilderType = factoryConfiguration.ConfigurationBuilderType;
        var configurationBuilderFactoryKey = string.Format(ConfigurationBuilderFactoryKeyFormat, id);
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
                         .Named(configurationBuilderFactoryKey));
            configurationBuilder = Kernel.Resolve<IConfigurationBuilder>(configurationBuilderFactoryKey);
        }

        var configuration = configurationBuilder.GetConfiguration(factoryConfiguration.GetConfiguration());

        // Registers NHibernate Configuration.
        Kernel.Register(
            Component.For<NHibernate.Cfg.Configuration>()
                     .Instance(configuration)
                     .Named($"{id}.cfg"));

        // If a Session Factory level interceptor was provided, we use it.
        if (Kernel.HasComponent(SessionInterceptorKey))
        {
            configuration.Interceptor = Kernel.Resolve<IInterceptor>(SessionInterceptorKey);
        }

        // Registers NHibernate ISessionFactory.
        Kernel.Register(
            Component.For<ISessionFactory>()
                     .Named(id)
                     .Activator<SessionFactoryActivator>()
                     .ExtendedProperties(Property.ForKey(Constants.SessionFactoryConfiguration).Eq(configuration))
                     .LifeStyle.Singleton);

        sessionFactoryResolver.RegisterAliasComponentIdMapping(alias!, id!);
    }

    #endregion

    #region Helper methods

    private void AssertHasAtLeastOneFactoryConfigured()
    {
        if (_facilityConfiguration.HasValidFactory)
        {
            return;
        }

        var factoriesConfig = FacilityConfig.Children["factory"];
        if (factoriesConfig is null)
        {
            const string message = $"You need to configure at least one factory to use the '{nameof(NHibernateFacility)}'.";
            throw new ConfigurationErrorsException(message);
        }
    }

    private void AssertHasConfiguration()
    {
        if (!_facilityConfiguration.IsValid)
        {
            var message = $"The '{nameof(NHibernateFacility)}' requires configuration.";
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
    private Type _configurationBuilderType = null!;
    private readonly bool _useReflectionOptimizer = false;
    private bool _isWeb;
    private Type _sessionStoreType = null!;

    private IKernel _kernel = null!;
    private IConfiguration _facilityConfiguration = null!;
    private IConfigurationBuilder? _configurationBuilder;

    public IEnumerable<NHibernateFactoryConfiguration> FactoryConfigurations { get; set; }

    /// <summary>
    /// </summary>
    /// <param name="configurationBuilder"></param>
    public NHibernateFacilityConfiguration(IConfigurationBuilder configurationBuilder)
    {
        _configurationBuilder = configurationBuilder;

        FactoryConfigurations = [];
    }

    public string? DefaultFlushMode { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="kernel"></param>
    /// <param name="facilityConfiguration"></param>
    public void Init(IKernel kernel, IConfiguration facilityConfiguration)
    {
        _kernel = kernel;
        _facilityConfiguration = facilityConfiguration;

        if (ConfigurationIsValid)
        {
            ConfigureWithExternalConfiguration();
        }
        else
        {
            FactoryConfigurations = new[]
            {
                new NHibernateFactoryConfiguration(new MutableConfiguration("factory"))
                {
                    Id = "factory_1"
                },
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
                ConfigurationBuilder(converter.PerformConversion<Type>(builder));
            }
            catch (ConverterException)
            {
                throw new FacilityException(
                    $"ConfigurationBuilder type '{builder}' is invalid or not found.");
            }
        }

        BuildFactories();

        DefaultFlushMode = _facilityConfiguration.Attributes[NHibernateFacility.DefaultFlushModeConfigurationKey];

        _ = bool.TryParse(_facilityConfiguration.Attributes[NHibernateFacility.IsWebConfigurationKey], out _isWeb);

        if (_facilityConfiguration.Attributes[NHibernateFacility.SessionStoreConfigurationKey] is not null)
        {
            var sessionStoreType = _facilityConfiguration.Attributes[NHibernateFacility.SessionStoreConfigurationKey];
            var converter = (ITypeConverter) _kernel.GetSubSystem(SubSystemConstants.ConversionManagerKey);
            SessionStore(converter.PerformConversion<Type>(sessionStoreType));
        }
    }

    public bool IsValid =>
        _facilityConfiguration is not null ||
        _configurationBuilder is not null ||
        _configurationBuilderType is not null;

    private bool ConfigurationIsValid =>
        _facilityConfiguration is not null &&
        _facilityConfiguration.Children.Count > 0;

    public bool HasValidFactory =>
        FactoryConfigurations.Any();

    private void BuildFactories()
    {
        FactoryConfigurations =
            _facilityConfiguration.Children
                                  .Select(configuration =>
                                          new NHibernateFactoryConfiguration(configuration));
    }

    public bool HasConfigurationBuilderType =>
        _configurationBuilderType is not null;

    public bool HasConcreteConfigurationBuilder =>
        _configurationBuilder is not null && !HasConfigurationBuilderType;

    public Type GetConfigurationBuilderType()
    {
        return _configurationBuilderType;
    }

    public void ConfigurationBuilder(Type type)
    {
        _configurationBuilderType = type;
        _configurationBuilder = null;
    }

    public void ConfigurationBuilder(IConfigurationBuilder configurationBuilder)
    {
        _configurationBuilder = configurationBuilder;
    }

    public bool UseReflectionOptimizer
    {
        get
        {
            if (_facilityConfiguration is not null)
            {
                if (bool.TryParse(_facilityConfiguration.Attributes[NHibernateFacility.UseReflectionOptimizerConfigurationKey], out var result))
                {
                    return result;
                }

                return false;
            }

            return _useReflectionOptimizer;
        }
    }

    public void IsWeb()
    {
        _isWeb = true;
    }

    public Type GetSessionStoreType()
    {
        var sessionStoreType = NHibernateFacility.DefaultSessionStoreType;

        if (_isWeb)
        {
            sessionStoreType = typeof(WebSessionStore);
        }

        if (_sessionStoreType is not null)
        {
            sessionStoreType = _sessionStoreType;
        }

        return sessionStoreType;
    }

    public void SessionStore(Type type)
    {
        if (!typeof(ISessionStore).IsAssignableFrom(type))
        {
            var message = $"The specified sessionStore type '{type}' " +
                          $"does not implement the '{nameof(ISessionStore)}' interface.";
            throw new ConfigurationErrorsException(message);
        }

        _sessionStoreType = type;
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
        _facilityConfiguration = facilityConfiguration ??
                                 throw new ArgumentNullException(nameof(facilityConfiguration));

        Id = facilityConfiguration.Attributes[NHibernateFacility.SessionFactoryIdConfigurationKey];
        Alias = facilityConfiguration.Attributes[NHibernateFacility.SessionFactoryAliasConfigurationKey];
        ConfigurationBuilderType = facilityConfiguration.Attributes[NHibernateFacility.ConfigurationBuilderConfigurationKey];
    }

    /// <summary>
    /// Get or sets the factory Id.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the factory Alias.
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// Gets or sets the factory ConfigurationBuilder.
    /// </summary>
    public string? ConfigurationBuilderType { get; set; }

    /// <summary>
    /// Constructs an IConfiguration instance for this factory.
    /// </summary>
    /// <returns></returns>
    public IConfiguration GetConfiguration()
    {
        return _facilityConfiguration;
    }
}
