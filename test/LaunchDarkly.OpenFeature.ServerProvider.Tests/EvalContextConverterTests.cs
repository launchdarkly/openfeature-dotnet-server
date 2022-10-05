using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using OpenFeatureSDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class EvalContextConverterTests
    {
        private LogCapture _logCapture;
        private readonly EvalContextConverter _converter;

        public EvalContextConverterTests()
        {
            _logCapture = new LogCapture();
            _converter = new EvalContextConverter(_logCapture.Logger("test"));
        }

        [Fact]
        public void ItCanHandleBuiltInAttributes()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Set("secondary", "secondary")
                .Set("name", "name")
                .Set("firstName", "firstName")
                .Set("lastName", "lastName")
                .Set("email", "email")
                .Set("avatar", "avatar")
                .Set("ip", "ip")
                .Set("country", "country")
                .Set("anonymous", true).Build();

            var convertedUser = _converter.ToLdUser(evaluationContext);

            var expectedUser = User.Builder("the-key")
                .Secondary("secondary")
                .Name("name")
                .FirstName("firstName")
                .LastName("lastName")
                .Email("email")
                .Avatar("avatar")
                .IPAddress("ip")
                .Country("country")
                .Anonymous(true)
                .Build();

            Assert.Equal(expectedUser, convertedUser);
            // Nothing is wrong with this, so it shouldn't have produced any messages.
            Assert.Empty(_logCapture.GetMessages());
        }

        [Fact]
        public void ItAllowsNullForBuiltInAttributes()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Set("secondary", (string) null)
                .Set("name", (string) null)
                .Set("firstName", (string) null)
                .Set("lastName", (string) null)
                .Set("email", (string) null)
                .Set("avatar", (string) null)
                .Set("ip", (string) null)
                .Set("country", (string) null)
                // Cannot just pass in null, cannot pass in a nullable bool, have to either case to a reference type like
                // string, or construct a value instance and pass that.
                .Set("anonymous", new Value())
                .Build();

            var convertedUser = _converter.ToLdUser(evaluationContext);

            var expectedUser = User.Builder("the-key")
                .Build();

            Assert.Equal(expectedUser, convertedUser);
            // Nothing is wrong with this, so it shouldn't have produced any messages.
            Assert.Empty(_logCapture.GetMessages());
        }

        [Fact]
        public void ItLogsAndErrorWhenThereIsNoTargetingKey()
        {
            _converter.ToLdUser(EvaluationContext.Empty);
            Assert.True(_logCapture.HasMessageWithText(LogLevel.Error,
                "The EvaluationContext must contain either a 'targetingKey' or a 'key' and the type" +
                "must be a string."));
        }

        [Fact]
        public void ItLogsAWarningWhenBothTargetingKeyAndKeyAreDefined()
        {
            _converter.ToLdUser(EvaluationContext.Builder().Set("targetingKey", "key").Set("key", "key").Build());
            Assert.True(_logCapture.HasMessageWithText(LogLevel.Warn,
                "The EvaluationContext contained both a 'targetingKey' and a 'key' attribute. The 'key'" +
                "attribute will be discarded."));
        }

        [Theory]
        [InlineData("secondary", "string")]
        [InlineData("name", "string")]
        [InlineData("firstName", "string")]
        [InlineData("lastName", "string")]
        [InlineData("email", "string")]
        [InlineData("avatar", "string")]
        [InlineData("ip", "string")]
        [InlineData("country", "string")]
        [InlineData("anonymous", "bool")]
        public void ItLogsErrorsWhenTypesAreIncorrectForBuiltInAttributes(string attr, string type)
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "key")
                //Number isn't valid for any built-in,
                .Set(attr, 1).Build();

            _converter.ToLdUser(evaluationContext);

            Assert.True(_logCapture.HasMessageWithRegex(LogLevel.Error, $".*attribute '{attr}'.*type {type}.*"));

            _converter.ToLdUser(evaluationContext);
        }

        [Fact]
        public void ItSupportsCustomAttributes()
        {
            const string attributeKey = "some-custom-attribute";
            const string attributeValue = "the attribute value";
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Set(attributeKey, attributeValue)
                .Build();

            var ldUser = _converter.ToLdUser(evaluationContext);
            Assert.Equal(
                attributeValue,
                ldUser.GetAttribute(UserAttribute.ForName(attributeKey)).AsString
            );
        }

        [Fact]
        public void ItCanUseKeyAttribute()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("key", "the-key")
                .Build();
            Assert.Equal("the-key", _converter.ToLdUser(evaluationContext).Key);
        }

        [Fact]
        public void ItUsesTheTargetingKeyInFavorOfKey()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("key", "key")
                .Set("targetingKey", "targeting-key")
                .Build();
            Assert.Equal("targeting-key", _converter.ToLdUser(evaluationContext).Key);
        }
    }
}
