using System;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;
using OpenFeature.Hosting;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting
{
    /// <summary>
    /// Extension methods for registering the LaunchDarkly OpenFeature provider with the OpenFeature
    /// dependency injection integration.
    /// </summary>
    public static class FeatureBuilderExtensions
    {
        /// <summary>
        /// Registers the LaunchDarkly provider as the default OpenFeature provider.
        /// </summary>
        /// <param name="builder">The OpenFeature builder</param>
        /// <param name="configureOptions">Configures the LaunchDarkly provider options</param>
        /// <returns>The OpenFeature builder</returns>
        public static OpenFeatureBuilder AddLaunchDarklyProvider(this OpenFeatureBuilder builder,
            Action<LaunchDarklyProviderOptions> configureOptions)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            AddRegistry(builder);
            builder.Services.Configure(ProviderRegistry.DefaultDomain, configureOptions);
            builder.Services.TryAddSingleton(sp =>
                sp.GetRequiredService<ProviderRegistry>().Get(ProviderRegistry.DefaultDomain).GetClient());

            return builder.AddProvider(sp =>
                sp.GetRequiredService<ProviderRegistry>().Get(ProviderRegistry.DefaultDomain));
        }

        /// <summary>
        /// Registers the LaunchDarkly provider for the given domain.
        /// </summary>
        /// <param name="builder">The OpenFeature builder</param>
        /// <param name="domain">The OpenFeature domain the provider is bound to</param>
        /// <param name="configureOptions">Configures the LaunchDarkly provider options</param>
        /// <returns>The OpenFeature builder</returns>
        public static OpenFeatureBuilder AddLaunchDarklyProvider(this OpenFeatureBuilder builder, string domain,
            Action<LaunchDarklyProviderOptions> configureOptions)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentNullException(nameof(domain));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            AddRegistry(builder);
            builder.Services.Configure(domain, configureOptions);
            builder.Services.TryAddKeyedSingleton<ILdClient>(domain, (sp, key) =>
                sp.GetRequiredService<ProviderRegistry>().Get(key.ToString()).GetClient());

            return builder.AddProvider(domain,
                (sp, providerDomain) => sp.GetRequiredService<ProviderRegistry>().Get(providerDomain));
        }

        private static void AddRegistry(OpenFeatureBuilder builder)
        {
            builder.Services.AddOptions();
            builder.Services.TryAddSingleton<ProviderRegistry>();
        }
    }
}
