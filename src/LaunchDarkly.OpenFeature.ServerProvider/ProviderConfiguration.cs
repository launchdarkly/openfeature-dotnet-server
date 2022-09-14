using LaunchDarkly.Sdk.Server.Interfaces;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    public sealed class ProviderConfiguration
    {
        /// <summary>
        /// Get a <see cref="ProviderConfigurationBuilder"/> instance.
        /// </summary>
        /// <returns>A new <see cref="ProviderConfigurationBuilder"/> instance</returns>
        public static ProviderConfigurationBuilder Builder()
        {
            return new ProviderConfigurationBuilder();
        }

        /// <summary>
        /// A factory object that creates a <see cref="LoggingConfiguration"/>, defining the SDK's
        /// logging configuration.
        /// </summary>
        /// <remarks>
        /// SDK components should not use this property directly; instead, the SDK client will use it to create a
        /// logger instance which will be in <see cref="LdClientContext"/>.
        /// </remarks>
        public ILoggingConfigurationFactory LoggingConfigurationFactory { get; }

        #region Internal constructor

        internal ProviderConfiguration(ProviderConfigurationBuilder builder)
        {
            LoggingConfigurationFactory = builder._loggingConfigurationFactory;
        }

        #endregion
    }
}
