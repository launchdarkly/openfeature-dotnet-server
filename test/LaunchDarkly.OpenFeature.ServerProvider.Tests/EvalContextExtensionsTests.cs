using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class EvalContextExtensionsTests
    {
        private readonly EvalContextConverter _converter =
            new EvalContextConverter(Components.NoLogging.CreateLoggingConfiguration().LogAdapter.Logger("test"));
        
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
        }

        [Fact]
        public void WhatHappens()
        {
            _converter.ToLdUser(new EvaluationContext());
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
