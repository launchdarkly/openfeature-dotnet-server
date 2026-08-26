using System;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenFeature;
using OpenFeature.Hosting;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting
{
    /// <summary>
    /// Extension methods which register the LaunchDarkly provider with the OpenFeature dependency
    /// injection integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In addition to the provider these methods register the <see cref="ILdClient"/> used by the provider,
    /// which can be used for LaunchDarkly functionality that OpenFeature does not expose, such as migration
    /// flags and tracking. For a provider registered with a domain the client is registered as a keyed service
    /// using that domain as the key.
    /// </para>
    /// <para>
    /// The provider, and its client, are shut down when the host stops.
    /// </para>
    /// </remarks>
    public static class FeatureBuilderExtensions
    {
        /// <summary>
        /// Registers the LaunchDarkly provider as the default OpenFeature provider using options configured
        /// elsewhere, for instance by binding <see cref="LaunchDarklyProviderOptions"/> to configuration.
        /// </summary>
        /// <param name="builder">The OpenFeature builder</param>
        /// <returns>The OpenFeature builder</returns>
        public static OpenFeatureBuilder AddLaunchDarklyProvider(this OpenFeatureBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return AddDefaultProvider(builder);
        }

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

            builder.Services.Configure(ProviderRegistry.DefaultDomain, configureOptions);
            return AddDefaultProvider(builder);
        }

        /// <summary>
        /// Registers the LaunchDarkly provider for the given domain using options configured elsewhere,
        /// for instance by binding named <see cref="LaunchDarklyProviderOptions"/> to configuration.
        /// </summary>
        /// <param name="builder">The OpenFeature builder</param>
        /// <param name="domain">The OpenFeature domain the provider is bound to, which is also the name of
        /// the options used to configure it</param>
        /// <returns>The OpenFeature builder</returns>
        public static OpenFeatureBuilder AddLaunchDarklyProvider(this OpenFeatureBuilder builder, string domain)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (string.IsNullOrWhiteSpace(domain)) throw new ArgumentNullException(nameof(domain));

            return AddDomainProvider(builder, domain);
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

            builder.Services.Configure(domain, configureOptions);
            return AddDomainProvider(builder, domain);
        }

        private static OpenFeatureBuilder AddDefaultProvider(OpenFeatureBuilder builder)
        {
            AddRegistry(builder);
            builder.Services.TryAddSingleton(sp =>
                sp.GetRequiredService<ProviderRegistry>().Get(ProviderRegistry.DefaultDomain).GetClient());

            return builder.AddProvider(sp =>
                sp.GetRequiredService<ProviderRegistry>().Get(ProviderRegistry.DefaultDomain));
        }

        private static OpenFeatureBuilder AddDomainProvider(OpenFeatureBuilder builder, string domain)
        {
            AddRegistry(builder);
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
