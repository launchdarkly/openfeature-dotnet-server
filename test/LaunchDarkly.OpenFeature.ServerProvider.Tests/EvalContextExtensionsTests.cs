using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class EvalContextExtensionsTests
    {
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

            var convertedUser = evaluationContext.ToLdUser();

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
        public void ItSupportsCustomAttributes()
        {
            const string attributeKey = "some-custom-attribute";
            const string attributeValue = "the attribute value";
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("targetingKey", "the-key");
            evaluationContext.Add(attributeKey, attributeValue);

            var ldUser = evaluationContext.ToLdUser();
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
            Assert.Equal("the-key", evaluationContext.ToLdUser().Key);
        }
        
        [Fact]
        public void ItUsesTheTargetingKeyInFavorOfKey()
        {
            var evaluationContext = new EvaluationContext();
            evaluationContext.Add("key", "key");
            evaluationContext.Add("targetingKey", "targeting-key");
            Assert.Equal("targeting-key", evaluationContext.ToLdUser().Key);
        }
    }
}
