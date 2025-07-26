using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;
using OpenFeature.DependencyInjection;
using System;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as a <see cref="FeatureProvider"/>.
    /// </summary>
    public static partial class LaunchDarklyOpenFeatureBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as the default provider
        /// using the specified <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="configuration">A pre-built LaunchDarkly <see cref="Configuration"/>.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, Configuration configuration)
            => RegisterLaunchDarklyProvider(builder, () => CreateConfiguration(configuration), sp => sp.GetRequiredService<Configuration>());

        /// <summary>
        /// Configures the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as a domain-scoped provider
        /// using the specified <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="domain">A domain identifier (e.g., tenant or environment).</param>
        /// <param name="configuration">A pre-built LaunchDarkly <see cref="Configuration"/> specific to the domain.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string domain, Configuration configuration)
            => RegisterLaunchDarklyProviderForDomain(
                builder,
                domain,
                () => CreateConfiguration(configuration),
                (sp, key) => sp.GetRequiredKeyedService<Configuration>(key));

        /// <summary>
        /// Configures the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as the default provider
        /// using the specified SDK key and optional configuration delegate.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="stdKey">The SDK key used to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to customize the <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string stdKey, Action<ConfigurationBuilder> configure = null)
            => RegisterLaunchDarklyProvider(builder, () => CreateConfiguration(stdKey, configure), sp => sp.GetRequiredService<Configuration>());

        /// <summary>
        /// Configures the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as a domain-scoped provider
        /// using the specified SDK key and optional configuration delegate.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="domain">A domain identifier (e.g., tenant or environment).</param>
        /// <param name="stdKey">The SDK key used to initialize the LaunchDarkly configuration.</param>
        /// <param name="configure">An optional delegate to customize the <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string domain, string stdKey, Action<ConfigurationBuilder> configure = null)
            => RegisterLaunchDarklyProviderForDomain(
                builder,
                domain,
                () => CreateConfiguration(stdKey, configure),
                (sp, key) => sp.GetRequiredKeyedService<Configuration>(key));

        /// <summary>
        /// Registers LaunchDarkly as the default feature provider using the given configuration factory and resolution logic.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="createConfiguration">A delegate that returns a <see cref="Configuration"/> instance.</param>
        /// <param name="resolveConfiguration">A delegate that resolves the <see cref="Configuration"/> from the service provider.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        private static OpenFeatureBuilder RegisterLaunchDarklyProvider(
            OpenFeatureBuilder builder,
            Func<Configuration> createConfiguration,
            Func<IServiceProvider, Configuration> resolveConfiguration)
        {
            // Perform early configuration validation to ensure the provider is correctly constructed.
            // This avoids runtime failures by eagerly building the configuration during setup.
            var config = createConfiguration();
            builder.Services.TryAddSingleton(_ => config);

            return builder.AddProvider(serviceProvider => new Provider(resolveConfiguration(serviceProvider)));
        }

        /// <summary>
        /// Registers LaunchDarkly as a domain-scoped feature provider using the given configuration factory and resolution logic.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="domain">A domain identifier (e.g., tenant or environment).</param>
        /// <param name="createConfiguration">A delegate that returns a domain-specific <see cref="Configuration"/> instance.</param>
        /// <param name="resolveConfiguration">A delegate that resolves the domain-scoped <see cref="Configuration"/> from the service provider.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        private static OpenFeatureBuilder RegisterLaunchDarklyProviderForDomain(
            OpenFeatureBuilder builder,
            string domain,
            Func<Configuration> createConfiguration,
            Func<IServiceProvider, object, Configuration> resolveConfiguration)
        {
            // Applies the same early validation strategy as the default registration path,
            // ensuring domain-scoped configurations fail fast if misconfigured.
            var config = createConfiguration();
            builder.Services.TryAddKeyedSingleton(domain, (_, obj) => config);

            return builder.AddProvider(domain, (serviceProvider, key) => new Provider(resolveConfiguration(serviceProvider, key)));
        }

        /// <summary>
        /// Creates a new <see cref="Configuration"/> by cloning the specified instance and rebuilding it.
        /// </summary>
        /// <param name="configuration">An existing <see cref="Configuration"/> instance.</param>
        /// <returns>A rebuilt <see cref="Configuration"/> instance.</returns>
        private static Configuration CreateConfiguration(Configuration configuration)
        {
            var configBuilder = Configuration.Builder(configuration);
            return configBuilder.Build();
        }

        /// <summary>
        /// Creates a new <see cref="Configuration"/> using the specified SDK key and optional configuration delegate.
        /// </summary>
        /// <param name="stdKey">The SDK key used to initialize the configuration.</param>
        /// <param name="configure">An optional delegate to customize the <see cref="ConfigurationBuilder"/>.</param>
        /// <returns>A fully constructed <see cref="Configuration"/> instance.</returns>
        private static Configuration CreateConfiguration(string stdKey, Action<ConfigurationBuilder> configure = null)
        {
            var configBuilder = Configuration.Builder(stdKey);
            configure?.Invoke(configBuilder);
            return configBuilder.Build();
        }
    }
}
