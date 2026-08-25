using System;
using LaunchDarkly.Sdk.Server;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting
{
    /// <summary>
    /// Options used to configure a LaunchDarkly <see cref="Provider"/> registered through dependency injection.
    /// </summary>
    public sealed class LaunchDarklyProviderOptions
    {
        /// <summary>
        /// The LaunchDarkly SDK key.
        /// </summary>
        public string SdkKey { get; set; }

        /// <summary>
        /// An optional delegate which can further configure the LaunchDarkly SDK.
        /// </summary>
        public Action<ConfigurationBuilder> ConfigureSdk { get; set; }

        internal Configuration BuildConfiguration()
        {
            if (string.IsNullOrWhiteSpace(SdkKey))
            {
                throw new InvalidOperationException(
                    $"{nameof(LaunchDarklyProviderOptions)}.{nameof(SdkKey)} must be configured.");
            }

            var configurationBuilder = Configuration.Builder(SdkKey);
            ConfigureSdk?.Invoke(configurationBuilder);
            return configurationBuilder.Build();
        }
    }
}
