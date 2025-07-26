using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;
using OpenFeature.DependencyInjection;
using System;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring the <see cref="OpenFeatureBuilder"/> with LaunchDarkly support.
    /// </summary>
    public static partial class OpenFeatureBuilderExtensions
    {
        /// <summary>
        /// Registers the LaunchDarkly <see cref="Provider"/> as the default <see cref="FeatureProvider"/> in the OpenFeature system,
        /// using the specified standard key and an optional configuration delegate.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> used to configure the OpenFeature system.</param>
        /// <param name="stdKey">The standard key used to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate for customizing the <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>The <see cref="OpenFeatureBuilder"/> instance with the LaunchDarkly provider registered.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            EnsureValidConfiguration(stdKey, configure);

            builder.Services.TryAddSingleton(_ => CreateConfiguration(stdKey, configure));

            return builder.AddProvider(serviceProvider => {
                var config = serviceProvider.GetRequiredService<Configuration>();
                return new Provider(config);
            });
        }

        /// <summary>
        /// Registers the LaunchDarkly <see cref="Provider"/> as a domain-scoped <see cref="FeatureProvider"/> within the OpenFeature system,
        /// allowing for isolated configurations per domain using the specified standard key and optional configuration.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance used for configuration.</param>
        /// <param name="domain">The domain identifier to associate with the provider (e.g., tenant or environment).</param>
        /// <param name="stdKey">The standard key employed to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to further configure the LaunchDarkly <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>The configured <see cref="OpenFeatureBuilder"/> instance with the domain-scoped LaunchDarkly provider registered.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string domain, string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            EnsureValidConfiguration(stdKey, configure);

            builder.Services.TryAddKeyedSingleton(domain, (_, obj) => CreateConfiguration(stdKey, configure));

            return builder.AddProvider(domain, (serviceProvider, key) => {
                var config = serviceProvider.GetRequiredKeyedService<Configuration>(key);
                return new Provider(config);
            });
        }

        /// <summary>
        /// Ensures that the LaunchDarkly configuration can be created successfully using the provided key and optional configuration delegate.
        /// Throws an exception if the configuration is invalid.
        /// </summary>
        /// <param name="stdKey">The SDK key used to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to customize the <see cref="ConfigurationBuilder"/>.</param>
        private static void EnsureValidConfiguration(string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            CreateConfiguration(stdKey, configure);
        }

        /// <summary>
        /// Creates a LaunchDarkly <see cref="Configuration"/> using the specified SDK key and optional configuration logic.
        /// </summary>
        /// <param name="stdKey">The SDK key used to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to customize the <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>A fully built <see cref="Configuration"/> instance.</returns>
        private static Configuration CreateConfiguration(string stdKey, Action<ConfigurationBuilder> configure)
        {
            var configBuilder = Configuration.Builder(stdKey);
            configure?.Invoke(configBuilder);
            return configBuilder.Build();
        }

    }
}
