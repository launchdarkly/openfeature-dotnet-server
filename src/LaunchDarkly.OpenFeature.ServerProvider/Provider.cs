using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using LaunchDarkly.Sdk.Server.Subsystems;
using OpenFeature;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// An OpenFeature <see cref="FeatureProvider"/> which enables the use of the LaunchDarkly Server-Side SDK for .NET
    /// with OpenFeature.
    /// </summary>
    /// <example>
    ///     var config = Configuration.Builder("my-sdk-key")
    ///                  .Build();
    ///
    ///     var ldClient  = new LdClient(config);
    ///     var provider = new Provider(ldClient);
    ///
    ///     OpenFeature.Api.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.Api.Instance.GetClient();
    /// </example>
    public sealed class Provider : FeatureProvider
    {
        private const string NameSpace = "OpenFeature.ServerProvider";
        private readonly Metadata _metadata = new Metadata($"LaunchDarkly.{NameSpace}");
        private readonly ILdClient _client;
        private readonly EvalContextConverter _contextConverter;

        /// <summary>
        /// Construct a new instance of the provider.
        /// </summary>
        /// <param name="client">The <see cref="ILdClient"/> instance</param>
        /// <param name="config">An optional <see cref="ProviderConfiguration"/></param>
        public Provider(ILdClient client, ProviderConfiguration config = null)
        {
            _client = client;
            var logConfig = (config?.LoggingConfigurationFactory ?? Components.Logging())
                .Build(null);

            // If there is a base name for the logger, then use the namespace as the name.
            var log = logConfig.LogAdapter.Logger(logConfig.BaseLoggerName != null
                ? $"{logConfig.BaseLoggerName}.{NameSpace}"
                : _metadata.Name);
            _contextConverter = new EvalContextConverter(log);
        }

        #region FeatureProvider Implementation

        /// <inheritdoc />
        public override Metadata GetMetadata() => _metadata;

        /// <inheritdoc />
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .BoolVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .StringVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .IntVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .DoubleVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .JsonVariationDetail(flagKey, _contextConverter.ToLdContext(context), LdValue.Null)
            .ToValueDetail(defaultValue).ToResolutionDetails(flagKey));

        #endregion
    }
}
