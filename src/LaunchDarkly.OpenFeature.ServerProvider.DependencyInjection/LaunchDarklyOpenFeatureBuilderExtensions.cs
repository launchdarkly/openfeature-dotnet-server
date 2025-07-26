using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;
using OpenFeature.DependencyInjection;
using System;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection
{
    /// <summary>
    /// Contains extension methods for the <see cref="OpenFeatureBuilder"/> class.
    /// </summary>
    public static partial class OpenFeatureBuilderExtensions
    {
        /// <summary>
        /// Registers the LaunchDarkly <see cref="Provider"/> as the default <see cref="FeatureProvider"/> within the OpenFeature system,
        /// utilizing the specified standard key and optional configuration.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance used for configuration.</param>
        /// <param name="stdKey">The standard key employed to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to further configure the LaunchDarkly <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>The configured <see cref="OpenFeatureBuilder"/> instance with the LaunchDarkly provider registered.</returns>
        public static OpenFeatureBuilder AddLaunchDarkly(this OpenFeatureBuilder builder, string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            builder.Services.TryAddSingleton(_ => {
                var configBuilder = Configuration.Builder(stdKey);
                configure?.Invoke(configBuilder);
                return configBuilder.Build();
            });

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
        public static OpenFeatureBuilder AddLaunchDarkly(this OpenFeatureBuilder builder, string domain, string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            builder.Services.TryAddKeyedSingleton(domain, (_, obj) => {
                var configBuilder = Configuration.Builder(stdKey);
                configure?.Invoke(configBuilder);
                return configBuilder.Build();
            });

            return builder.AddProvider(domain, (serviceProvider, key) => {
                var config = serviceProvider.GetRequiredKeyedService<Configuration>(key);
                return new Provider(config);
            });
        }
    }
}
