using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server.Interfaces;
using Moq;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ProviderTests
    {
        [Fact]
        public void ItCanProvideMetaData()
        {
            var mock = new Mock<ILdClient>();
            var provider = new Provider(mock.Object);

            Assert.Equal("launchdarkly-dotnet-server-provider", provider.GetMetadata().Name);
        }

        [Fact]
        public void ItCanDoABooleanEvaluation()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.BoolVariationDetail("flag-key", evaluationContext.ToLdUser(), false))
                .Returns(new EvaluationDetail<bool>(true, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveBooleanValue("flag-key", false, evaluationContext).Result;
            Assert.True(res.Value);
        }
        
        [Fact]
        public void ItCanDoAStringEvaluation()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.StringVariationDetail("flag-key", evaluationContext.ToLdUser(), "default"))
                .Returns(new EvaluationDetail<string>("notDefault", 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveStringValue("flag-key", "default", evaluationContext).Result;
            Assert.Equal("notDefault", res.Value);
        }
        
        [Fact]
        public void ItCanDoAnIntegerEvaluation()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.IntVariationDetail("flag-key", evaluationContext.ToLdUser(), 0))
                .Returns(new EvaluationDetail<int>(1, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveIntegerValue("flag-key", 0, evaluationContext).Result;
            Assert.Equal(1, res.Value);
        }
        
        [Fact]
        public void ItCanDoADoubleEvaluation()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.DoubleVariationDetail("flag-key", evaluationContext.ToLdUser(), 0))
                .Returns(new EvaluationDetail<double>(1.7, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveDoubleValue("flag-key", 0, evaluationContext).Result;
            Assert.Equal(1.7, res.Value);
        }
        
        [Fact]
        public void ItCanDoAValueEvaluation()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.JsonVariationDetail("flag-key", It.IsAny<User>(), It.IsAny<LdValue>()))
                .Returns(new EvaluationDetail<LdValue>(LdValue.Of("true"), 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveStructureValue("flag-key", new Value("false"), evaluationContext).Result;
            Assert.Equal("true", res.Value.AsString());
        }
    }
}