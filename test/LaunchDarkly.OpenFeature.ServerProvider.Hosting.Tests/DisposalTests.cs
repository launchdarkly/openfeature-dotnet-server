using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenFeature;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting.Tests
{
    public class DisposalTests
    {
        [Fact]
        public async Task DisposingTheHostAfterOpenFeatureShutdownDoesNotThrow()
        {
            var testData = TestData.DataSource();
            testData.Update(testData.Flag("enabled-flag").VariationForAll(true));

            var host = new HostBuilder().ConfigureServices(services =>
                services.AddOpenFeature(builder => builder.AddLaunchDarklyProvider(options =>
                {
                    options.SdkKey = "fake-key";
                    options.ConfigureSdk = config =>
                        config.DataSource(testData).Events(Components.NoEvents);
                }))).Build();

            await host.StartAsync();
            var ldClient = host.Services.GetRequiredService<ILdClient>();
            await host.StopAsync();

            host.Dispose();

            Assert.NotNull(ldClient);
        }
    }
}
