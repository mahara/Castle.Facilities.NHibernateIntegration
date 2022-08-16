#region License
// Copyright (c) 2004-2022 Castle Project - https://www.castleproject.org/
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

using Castle.Facilities.NHibernateIntegration.Configuration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

using MicrosoftConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Castle.Facilities.NHibernateIntegration
{
    public static class NHibernateFacilityExtensions
    {
        public static IWindsorContainer RegisterMicrosoftConfigurationMapper(
            this IWindsorContainer container)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }
#endif

            return container.RegisterMicrosoftConfigurationMapper<DefaultMicrosoftConfigurationMapper>();
        }

        public static IWindsorContainer RegisterMicrosoftConfigurationMapper<T>(
            this IWindsorContainer container)
            where T : IMicrosoftConfigurationMapper
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
#else
            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }
#endif

            if (!container.Kernel.HasComponent(typeof(IMicrosoftConfigurationMapper)))
            {
                container.Kernel.Register(
                    Component.For<IMicrosoftConfigurationMapper>()
                             .ImplementedBy<T>()
                             .Named(Constants.MicrosoftConfigurationMapper_ComponentName));
            }

            return container;
        }

        public static IWindsorContainer AddNHibernateFacility<T>(
            this IWindsorContainer container,
            MicrosoftConfiguration configuration,
            Action<T> onCreate)
            where T : NHibernateFacility, new()
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(container);
            ArgumentNullException.ThrowIfNull(configuration);
#else
            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
#endif

            container.AddNHibernateFacilityConfiguration<T>(configuration);

            container.AddFacility(onCreate);

            return container;
        }

        internal static IWindsorContainer AddNHibernateFacilityConfiguration<T>(
            this IWindsorContainer container,
            MicrosoftConfiguration configuration)
            where T : NHibernateFacility, new()
        {
            var configurationMapper = container.Resolve<IMicrosoftConfigurationMapper>();

            var facilityType = typeof(T);
            var facilityTypeFromConfiguration = configurationMapper.GetFacilityType(configuration);

            if (facilityType != facilityTypeFromConfiguration)
            {
                var message = $"The facility type specified by the generic type argument is '{facilityType.FullName}', " +
                              $"but the facility type specified in the configuration is '{facilityTypeFromConfiguration.FullName}'.";
                throw new ConfigurationErrorsException(message);
            }

            var facilityConfiguration = configurationMapper.Map(configuration);

            container.Kernel.ConfigurationStore.AddFacilityConfiguration(
                facilityType.FullName,
                facilityConfiguration);

            return container;
        }
    }
}
