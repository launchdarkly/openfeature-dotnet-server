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
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            evaluationContext.Add("secondary", "secondary");
            evaluationContext.Add("name", "name");
            evaluationContext.Add("firstName", "firstName");
            evaluationContext.Add("lastName", "lastName");
            evaluationContext.Add("email", "email");
            evaluationContext.Add("avatar", "avatar");
            evaluationContext.Add("ip", "ip");
            evaluationContext.Add("country", "country");
            evaluationContext.Add("anonymous", true);

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
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            evaluationContext.Add("secondary", (string)null);
            evaluationContext.Add("name", (string)null);
            evaluationContext.Add("firstName", (string)null);
            evaluationContext.Add("lastName", (string)null);
            evaluationContext.Add("email", (string)null);
            evaluationContext.Add("avatar", (string)null);
            evaluationContext.Add("ip", (string)null);
            evaluationContext.Add("country", (string)null);
            // Cannot just pass in null, cannot pass in a nullable bool, have to either case to a reference type like
            // string, or construct a value instance and pass that.
            evaluationContext.Add("anonymous", new Value());

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
            _converter.ToLdUser(new EvaluationContext());
            Assert.True(_logCapture.HasMessageWithText(LogLevel.Error,
                "The EvaluationContext must contain either a 'targetingKey' or a 'key' and the type" +
                "must be a string."));
        }

        [Fact]
        public void ItLogsAWarningWhenBothTargetingKeyAndKeyAreDefined()
        {
            _converter.ToLdUser(new EvaluationContext().Add("targetingKey", "key").Add("key", "key"));
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
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "key");
            //Number isn't valid for any built-in,
            evaluationContext.Add(attr, 1);

            _converter.ToLdUser(evaluationContext);

            Assert.True(_logCapture.HasMessageWithRegex(LogLevel.Error, $".*attribute '{attr}'.*type {type}.*"));

            _converter.ToLdUser(evaluationContext);
        }

        [Fact]
        public void ItSupportsCustomAttributes()
        {
            const string attributeKey = "some-custom-attribute";
            const string attributeValue = "the attribute value";
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            evaluationContext.Add(attributeKey, attributeValue);

            var ldUser = _converter.ToLdUser(evaluationContext);
            Assert.Equal(
                attributeValue,
                ldUser.GetAttribute(UserAttribute.ForName(attributeKey)).AsString
            );
        }

        [Fact]
        public void ItCanUseKeyAttribute()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("key", "the-key");
            Assert.Equal("the-key", _converter.ToLdUser(evaluationContext).Key);
        }

        [Fact]
        public void ItUsesTheTargetingKeyInFavorOfKey()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("key", "key");
            evaluationContext.Add("targetingKey", "targeting-key");
            Assert.Equal("targeting-key", _converter.ToLdUser(evaluationContext).Key);
        }
    }
}
