using System.Collections.Generic;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using OpenFeature.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class EvalContextConverterTests
    {
        private readonly LogCapture _logCapture;
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
                .Set("name", "name")
                .Set("firstName", "firstName")
                .Set("lastName", "lastName")
                .Set("email", "email")
                .Set("avatar", "avatar")
                .Set("ip", "ip")
                .Set("country", "country")
                .Set("anonymous", true).Build();

            var convertedUser = _converter.ToLdContext(evaluationContext);

            var expectedUser = User.Builder("the-key")
                .Name("name")
                .FirstName("firstName")
                .LastName("lastName")
                .Email("email")
                .Avatar("avatar")
                .IPAddress("ip")
                .Country("country")
                .Anonymous(true)
                .Build();

            Assert.Equal(Context.FromUser(expectedUser), convertedUser);
            // Nothing is wrong with this, so it shouldn't have produced any messages.
            Assert.Empty(_logCapture.GetMessages());
        }

        [Fact]
        public void ItAllowsNullForBuiltInAttributes()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
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

            var convertedUser = _converter.ToLdContext(evaluationContext);

            var expectedUser = User.Builder("the-key")
                .Build();

            Assert.Equal(Context.FromUser(expectedUser), convertedUser);
            // Nothing is wrong with this, so it shouldn't have produced any messages.
            Assert.Empty(_logCapture.GetMessages());
        }

        [Fact]
        public void ItLogsAndErrorWhenThereIsNoTargetingKey()
        {
            _converter.ToLdContext(EvaluationContext.Empty);
            Assert.True(_logCapture.HasMessageWithText(LogLevel.Error,
                "The EvaluationContext must contain either a 'targetingKey' or a 'key' and the type" +
                " must be a string."));
        }

        [Fact]
        public void ItLogsAWarningWhenBothTargetingKeyAndKeyAreDefined()
        {
            _converter.ToLdContext(EvaluationContext.Builder().Set("targetingKey", "key").Set("key", "key").Build());
            Assert.True(_logCapture.HasMessageWithText(LogLevel.Warn,
                "The EvaluationContext contained both a 'targetingKey' and a 'key' attribute. The 'key'" +
                " attribute will be discarded."));
        }

        [Theory]
        [InlineData("name", "string")]
        [InlineData("anonymous", "bool")]
        public void ItLogsErrorsWhenTypesAreIncorrectForBuiltInAttributes(string attr, string type)
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "key")
                //Number isn't valid for any built-in,
                .Set(attr, 1).Build();

            _converter.ToLdContext(evaluationContext);

            Assert.True(_logCapture.HasMessageWithRegex(LogLevel.Error, $".*attribute '{attr}'.*type {type}.*"));

            _converter.ToLdContext(evaluationContext);
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

            var ldUser = _converter.ToLdContext(evaluationContext);
            Assert.Equal(
                attributeValue,
                ldUser.GetValue(attributeKey).AsString
            );
        }

        [Fact]
        public void ItCanUseKeyAttribute()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("key", "the-key")
                .Build();
            Assert.Equal("the-key", _converter.ToLdContext(evaluationContext).Key);
        }

        [Fact]
        public void ItUsesTheTargetingKeyInFavorOfKey()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("key", "key")
                .Set("targetingKey", "targeting-key")
                .Build();
            Assert.Equal("targeting-key", _converter.ToLdContext(evaluationContext).Key);
        }

        [Fact]
        public void ItCanBuildASingleContext()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("kind", "organization")
                .Set("name", "the-org-name")
                .Set("targetingKey", "my-org-key")
                .Set("anonymous", true)
                .Set("myCustomAttribute", "myCustomValue")
                .Set("privateAttributes", new Value(new List<Value>{new Value("myCustomAttribute")}))
                .Build();

            var expectedContext = Context.Builder(ContextKind.Of("organization"), "my-org-key")
                .Name("the-org-name")
                .Anonymous(true)
                .Set("myCustomAttribute", "myCustomValue")
                .Private(new[] {"myCustomAttribute"})
                .Build();

            Assert.Equal(expectedContext, _converter.ToLdContext(evaluationContext));
        }

        [Fact]
        public void ItCanBuildAMultiContext()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("kind", "multi")
                .Set("organization", new Structure(new Dictionary<string, Value>
                {
                    {"targetingKey", new Value("my-org-key")},
                    {"name", new Value("the-org-name")},
                    {"myCustomAttribute", new Value("myAttributeValue")},
                    {"privateAttributes", new Value(new List<Value>{new Value("myCustomAttribute")})}
                }))
                .Set("user", new Structure(new Dictionary<string, Value> {
                    {"targetingKey", new Value("my-user-key")},
                    {"anonymous", new Value(true)}
                }))
                .Build();

            var expectedContext = Context.MultiBuilder()
                .Add(Context.Builder(ContextKind.Of("organization"), "my-org-key")
                    .Name("the-org-name")
                    .Set("myCustomAttribute", "myAttributeValue")
                    .Private(new []{"myCustomAttribute"})
                    .Build())
                .Add(Context.Builder("my-user-key")
                    .Anonymous(true)
                    .Build())
                .Build();

            Assert.Equal(expectedContext, _converter.ToLdContext(evaluationContext));
        }
    }
}
