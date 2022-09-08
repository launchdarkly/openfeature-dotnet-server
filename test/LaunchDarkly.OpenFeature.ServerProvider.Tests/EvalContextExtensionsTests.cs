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

            var ldUser = evaluationContext.ToLdUser();
            
            Assert.Equal("secondary", ldUser.Secondary);
            Assert.Equal("name", ldUser.Name);
            Assert.Equal("firstName", ldUser.FirstName);
            Assert.Equal("lastName", ldUser.LastName);
            Assert.Equal("email", ldUser.Email);
            Assert.Equal("avatar", ldUser.Avatar);
            Assert.Equal("ip", ldUser.IPAddress);
            Assert.Equal("country", ldUser.Country);
            Assert.True(ldUser.Anonymous);
        }
    }
}