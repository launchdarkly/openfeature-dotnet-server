using System.Threading.Tasks;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server.Interfaces;
using OpenFeature.SDK;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    public class Provider : FeatureProvider
    {
        private readonly Metadata _metadata = new Metadata("launchdarkly-dotnet-server-provider");
        private readonly ILdClient _client;

        public Provider(ILdClient client)
        {
            _client = client;
        }

        #region FeatureProvider Implementation

        public override Metadata GetMetadata() => _metadata;

        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null)
        {
            return Task.FromResult(_client.BoolVariationDetail(flagKey, context.ToLdUser(), defaultValue)
                .ToResolutionDetails(flagKey));
        }

        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null)
        {
            return Task.FromResult(_client.StringVariationDetail(flagKey, context.ToLdUser(), defaultValue)
                .ToResolutionDetails(flagKey));
        }

        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null)
        {
            return Task.FromResult(_client.IntVariationDetail(flagKey, context.ToLdUser(), defaultValue)
                .ToResolutionDetails(flagKey));
        }

        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null)
        {
            return Task.FromResult(_client.DoubleVariationDetail(flagKey, context.ToLdUser(), defaultValue)
                .ToResolutionDetails(flagKey));
        }

        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null)
        {
            return Task.FromResult(_client.JsonVariationDetail(flagKey, context.ToLdUser(), LdValue.Null)
                .ToValueDetail(defaultValue).ToResolutionDetails(flagKey));
        }

        #endregion
    }
}
