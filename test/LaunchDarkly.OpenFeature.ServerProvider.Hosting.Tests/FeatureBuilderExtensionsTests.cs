using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Integrations;
using LaunchDarkly.Sdk.Server.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenFeature;
using OpenFeature.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting.Tests
{
    public class FeatureBuilderExtensionsTests
    {
        private static TestData FlagData(string flagKey, bool value)
        {
            var testData = TestData.DataSource();
            testData.Update(testData.Flag(flagKey).VariationForAll(value));
            return testData;
        }

        private static void ConfigureOffline(TestData testData, ConfigurationBuilder builder) =>
            builder.DataSource(testData).Events(Components.NoEvents);

        [Fact]
        public async Task ResolvesFlagsThroughTheDefaultClient()
        {
            var testData = FlagData("enabled-flag", true);

            var host = new HostBuilder().ConfigureServices(services =>
                services.AddOpenFeature(builder => builder
                    .AddContext(context => context.SetTargetingKey("user-key"))
                    .AddLaunchDarklyProvider(options =>
                    {
                        options.SdkKey = "fake-key";
                        options.ConfigureSdk = config => ConfigureOffline(testData, config);
                    }))).Build();

            await host.StartAsync();

            using (var scope = host.Services.CreateScope())
            {
                var client = scope.ServiceProvider.GetRequiredService<IFeatureClient>();
                Assert.True(await client.GetBooleanValueAsync("enabled-flag", false));
                Assert.NotEqual("LaunchDarkly.OpenFeature.ServerProvider",
                    Api.Instance.GetProviderMetadata().Name);
            }

            await host.StopAsync();
        }

        [Fact]
        public async Task SharesASingleLdClientBetweenTheProviderAndTheContainer()
        {
            var testData = FlagData("enabled-flag", true);

            var host = new HostBuilder().ConfigureServices(services =>
                services.AddOpenFeature(builder => builder.AddLaunchDarklyProvider(options =>
                {
                    options.SdkKey = "fake-key";
                    options.ConfigureSdk = config => ConfigureOffline(testData, config);
                }))).Build();

            await host.StartAsync();

            var provider = (Provider)host.Services.GetRequiredService<FeatureProvider>();
            var providerAgain = (Provider)host.Services.GetRequiredService<FeatureProvider>();
            var ldClient = host.Services.GetRequiredService<ILdClient>();

            Assert.Same(provider, providerAgain);
            Assert.Same(provider.GetClient(), ldClient);

            await host.StopAsync();
        }

        [Fact]
        public async Task SupportsDomainScopedProviders()
        {
            var defaultData = FlagData("shared-flag", false);
            var betaData = FlagData("shared-flag", true);

            var host = new HostBuilder().ConfigureServices(services =>
                services.AddOpenFeature(builder => builder
                    .AddContext(context => context.SetTargetingKey("user-key"))
                    .AddLaunchDarklyProvider("default", options =>
                    {
                        options.SdkKey = "fake-key-default";
                        options.ConfigureSdk = config => ConfigureOffline(defaultData, config);
                    })
                    .AddLaunchDarklyProvider("beta", options =>
                    {
                        options.SdkKey = "fake-key-beta";
                        options.ConfigureSdk = config => ConfigureOffline(betaData, config);
                    })
                    .AddPolicyName(options => options.DefaultNameSelector = _ => "default"))).Build();

            await host.StartAsync();

            using (var scope = host.Services.CreateScope())
            {
                var defaultClient = scope.ServiceProvider.GetRequiredService<IFeatureClient>();
                var betaClient = scope.ServiceProvider.GetRequiredKeyedService<IFeatureClient>("beta");

                Assert.False(await defaultClient.GetBooleanValueAsync("shared-flag", false));
                Assert.True(await betaClient.GetBooleanValueAsync("shared-flag", false));
            }

            await host.StopAsync();
        }
    }
}
