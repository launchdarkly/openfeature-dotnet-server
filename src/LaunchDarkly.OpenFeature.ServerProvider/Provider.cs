using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using OpenFeature.SDK;
using OpenFeature.SDK.Model;

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
    ///     OpenFeature.SDK.OpenFeature.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.SDK.OpenFeature.Instance.GetClient();
    /// </example>
    public sealed class Provider : FeatureProvider
    {
        private readonly Metadata _metadata = new Metadata("LaunchDarkly.OpenFeature.ServerProvider");
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
                .CreateLoggingConfiguration();

            var log = logConfig.LogAdapter.Logger(logConfig.BaseLoggerName ?? _metadata.Name);
            _contextConverter = new EvalContextConverter(log);
        }

        #region FeatureProvider Implementation

        public override Metadata GetMetadata() => _metadata;

        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .BoolVariationDetail(flagKey, _contextConverter.ToLdUser(context), defaultValue)
            .ToResolutionDetails(flagKey));

        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .StringVariationDetail(flagKey, _contextConverter.ToLdUser(context), defaultValue)
            .ToResolutionDetails(flagKey));

        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .IntVariationDetail(flagKey, _contextConverter.ToLdUser(context), defaultValue)
            .ToResolutionDetails(flagKey));

        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .DoubleVariationDetail(flagKey, _contextConverter.ToLdUser(context), defaultValue)
            .ToResolutionDetails(flagKey));

        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .JsonVariationDetail(flagKey, _contextConverter.ToLdUser(context), LdValue.Null)
            .ToValueDetail(defaultValue).ToResolutionDetails(flagKey));

        #endregion
    }
}
