using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    public sealed class ProviderConfigurationBuilder
    {
        #region Internal properties

        // Rider/ReSharper would prefer these not use the private naming convention.
        // ReSharper disable once InconsistentNaming
        internal ILoggingConfigurationFactory _loggingConfigurationFactory;

        #endregion

        #region Internal constructor

        internal ProviderConfigurationBuilder() {}

        #endregion
        /// <summary>
        /// Sets the provider's logging configuration, using a factory object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This object is normally a configuration builder obtained from <see cref="Components.Logging()"/>
        /// which has methods for setting individual logging-related properties. As a shortcut for disabling
        /// logging, you may use <see cref="Components.NoLogging"/> instead. If all you want to do is to set
        /// the basic logging destination, and you do not need to set other logging properties, you can use
        /// <see cref="Logging(ILogAdapter)"/> instead.
        /// </para>
        /// <para>
        /// The provider uses the same logging mechanism as the LaunchDarkly Server-Side SDK for .NET.
        ///
        /// For more about how logging works in the SDK, see the <a href="https://docs.launchdarkly.com/sdk/features/logging#net">SDK
        /// SDK reference guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        ///     var config = ProviderConfigurationBuilder.Builder()
        ///         .Logging(Components.Logging().Level(LogLevel.Warn)))
        ///         .Build();
        /// </example>
        /// <param name="loggingConfigurationFactory">the factory object</param>
        /// <returns>the same builder</returns>
        /// <seealso cref="Components.Logging()" />
        /// <seealso cref="Components.Logging(ILogAdapter) "/>
        /// <seealso cref="Components.NoLogging" />
        /// <seealso cref="Logging(ILogAdapter)"/>
        public ProviderConfigurationBuilder Logging(ILoggingConfigurationFactory loggingConfigurationFactory)
        {
            _loggingConfigurationFactory = loggingConfigurationFactory;
            return this;
        }

        /// <summary>
        /// Sets the provider's logging destination.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a shortcut for <c>Logging(Components.Logging(logAdapter))</c>. You can use it when you
        /// only want to specify the basic logging destination, and do not need to set other log properties.
        /// </para>
        /// <para>
        /// For more about how logging works in the SDK, see the <a href="https://docs.launchdarkly.com/sdk/features/logging#net">SDK
        /// SDK reference guide</a>.
        /// </para>
        /// </remarks>
        /// <example>
        ///     var config = ProviderConfigurationBuilder.Builder()
        ///         .Logging(Logs.ToWriter(Console.Out))
        ///         .Build();
        /// </example>
        /// <param name="logAdapter">an <c>ILogAdapter</c> for the desired logging implementation</param>
        /// <returns>the same builder</returns>
        public ProviderConfigurationBuilder Logging(ILogAdapter logAdapter) =>
            Logging(Components.Logging(logAdapter));

        /// <summary>
        /// Creates a <see cref="ProviderConfiguration"/> based on the properties that have been set on the builder.
        /// Modifying the builder after this point does not affect the returned <see cref="ProviderConfiguration"/>.
        /// </summary>
        /// <returns>the configured <c>ProviderConfiguration</c> object</returns>
        public ProviderConfiguration Build()
        {
            return new ProviderConfiguration(this);
        }
    }
}
