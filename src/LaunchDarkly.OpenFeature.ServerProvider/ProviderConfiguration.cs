using LaunchDarkly.Sdk.Server.Interfaces;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Configuration options for <see cref="Provider"/>. This class should normally be constructed with
    /// <see cref="ProviderConfiguration.Builder()"/>.
    /// </summary>
    /// <remarks>
    /// Instances of <see cref="ProviderConfiguration"/> are immutable once created. They can be created using a builder
    /// pattern with <see cref="ProviderConfiguration.Builder()"/>.
    /// </remarks>
    public sealed class ProviderConfiguration
    {
        /// <summary>
        /// Creates a <see cref="ProviderConfigurationBuilder"/> for constructing a configuration object using a fluent
        /// syntax.
        /// </summary>
        /// <remarks>
        ///  The <see cref="ProviderConfigurationBuilder"/> has methods for setting any number of
        /// properties, after which you call <see cref="ProviderConfigurationBuilder.Build"/> to get the resulting
        /// <c>ProviderConfiguration</c> instance.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var config = ProviderConfigurationBuilder.Builder()
        ///         .Logging(Components.NoLogging)
        ///         .Build();
        /// </code>
        /// </example>
        /// <returns>a builder object</returns>
        public static ProviderConfigurationBuilder Builder()
        {
            return new ProviderConfigurationBuilder();
        }

        /// <summary>
        /// Creates an <see cref="ProviderConfigurationBuilder"/> based on an existing configuration.
        /// </summary>
        /// <remarks>
        /// Modifying properties of the builder will not affect the original configuration object.
        /// </remarks>
        /// <example>
        /// <code>
        ///     var configWithCustomEventProperties = Configuration.Builder(originalConfig)
        ///         .Logging(Components.NoLogging)
        ///         .Build();
        /// </code>
        /// </example>
        /// <param name="fromConfiguration">the existing configuration</param>
        /// <returns>a builder object</returns>
        public static ProviderConfigurationBuilder Builder(ProviderConfiguration fromConfiguration)
        {
            return new ProviderConfigurationBuilder(fromConfiguration);
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
