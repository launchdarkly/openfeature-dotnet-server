using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="configuration"/> argument is <c>null</c>.
        /// </exception>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
            }

            return RegisterLaunchDarklyProvider(
                builder, 
                () => configuration, 
                sp => sp.GetRequiredService<Configuration>()
            );
        }

        /// <summary>
        /// Configures the <see cref="OpenFeatureBuilder"/> to use LaunchDarkly as a domain-scoped provider
        /// using the specified <see cref="Configuration"/> instance.
        /// </summary>
        /// <param name="builder">The <see cref="OpenFeatureBuilder"/> instance to configure.</param>
        /// <param name="domain">A domain identifier (e.g., tenant or environment).</param>
        /// <param name="configuration">A pre-built LaunchDarkly <see cref="Configuration"/> specific to the domain.</param>
        /// <returns>The updated <see cref="OpenFeatureBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the <paramref name="configuration"/> argument is <c>null</c>.
        /// </exception>
        public static OpenFeatureBuilder UseLaunchDarkly(this OpenFeatureBuilder builder, string domain, Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "Configuration cannot be null.");
            }

            return RegisterLaunchDarklyProviderForDomain(
                builder,
                domain,
                () => configuration,
                (sp, key) => sp.GetRequiredKeyedService<Configuration>(key)
            );
        }

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
            // This ensures any misconfiguration is caught during application startup rather than at runtime.
            var config = createConfiguration();
            builder.Services.TryAddSingleton(config);

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
            // Perform early validation of the configuration to ensure it is valid before registration.
            // This approach is consistent with the default (non-domain) registration path and helps fail fast on misconfiguration.
            var config = createConfiguration();

            // Register the domain-scoped configuration as a keyed singleton.
            builder.Services.TryAddKeyedSingleton(domain, (_, key) => config);

            // Register the default configuration provider, which resolves the appropriate domain-scoped configuration
            // using the default name selection policy defined in PolicyNameOptions.
            // This enables resolving Configuration via serviceProvider.GetRequiredService<Configuration>()
            // when no specific domain key is explicitly provided.
            builder.Services.TryAddSingleton(provider =>
            {
                var policy = provider.GetRequiredService<IOptions<PolicyNameOptions>>().Value;
                var name = policy.DefaultNameSelector(provider);
                return provider.GetRequiredKeyedService<Configuration>(name);
            });

            // Register the domain-scoped provider instance.
            return builder.AddProvider(domain, (serviceProvider, key) => new Provider(resolveConfiguration(serviceProvider, key)));
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
