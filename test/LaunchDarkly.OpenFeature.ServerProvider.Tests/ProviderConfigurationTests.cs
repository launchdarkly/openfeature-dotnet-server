using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.TestHelpers;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ProviderConfigurationTests
    {
        private readonly BuilderBehavior.BuildTester<ProviderConfigurationBuilder, ProviderConfiguration> _tester =
            BuilderBehavior.For(ProviderConfiguration.Builder, b => b.Build())
                .WithCopyConstructor(ProviderConfiguration.Builder);

        [Fact]
        public void Logging()
        {
            var prop = _tester.Property(c => c.LoggingConfigurationFactory, (b, v) => b.Logging(v));
            prop.AssertDefault(null);
            prop.AssertCanSet(Components.Logging(Logs.ToWriter(Console.Out)));
        }

        [Fact]
        public void LoggingAdapterShortcut()
        {
            var adapter = Logs.ToWriter(Console.Out);
            var config = ProviderConfiguration.Builder().Logging(adapter).Build();
            var logConfig = config.LoggingConfigurationFactory.CreateLoggingConfiguration();
            Assert.Same(adapter, logConfig.LogAdapter);
        }
    }
}
