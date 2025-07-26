using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.DependencyInjection;
using OpenFeature.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection.Tests
{
    public class DependencyInjectionIntegrationTests
    {
        private const string TestSdkKey = "test-sdk-key";
        private const string TestDomain = "test-domain";
        private const string TestFlagKey = "test-flag";

        [Fact]
        public async Task AddLaunchDarkly_CanResolveDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act
            // Since we're using offline mode, the flag will return the default value
            var result = await api.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task AddLaunchDarkly_CanResolveDomainScopedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredKeyedService<FeatureClient>(TestDomain);

            // Act
            // Since we're using offline mode, the flag will return the default value
            var result = await api.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public void AddLaunchDarkly_ProvidersFromDifferentDomainsAreDistinct()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            const string domain1 = "domain1";
            const string domain2 = "domain2";
            
            builder.AddLaunchDarkly(domain1, TestSdkKey, config => config.Offline(true));
            builder.AddLaunchDarkly(domain2, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var client1 = serviceProvider.GetRequiredKeyedService<FeatureClient>(domain1);
            var client2 = serviceProvider.GetRequiredKeyedService<FeatureClient>(domain2);

            // Assert
            Assert.NotSame(client1, client2);
        }

        [Fact]
        public void AddLaunchDarkly_ConfigurationIsSharedWithinSameDomain()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.Same(config1, config2);
            Assert.True(config1.Offline);
        }

        [Fact]
        public void AddLaunchDarkly_DefaultConfigurationIsShared()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.Same(config1, config2);
            Assert.True(config1.Offline);
        }

        [Fact]
        public async Task AddLaunchDarkly_ProviderSupportsAllValueTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act & Assert - Test all supported types
            var boolResult = await api.GetBooleanValueAsync("bool-flag", true);
            Assert.True(boolResult);

            var stringResult = await api.GetStringValueAsync("string-flag", "default");
            Assert.Equal("default", stringResult);

            var intResult = await api.GetIntegerValueAsync("int-flag", 42);
            Assert.Equal(42, intResult);

            var doubleResult = await api.GetDoubleValueAsync("double-flag", 3.14);
            Assert.Equal(3.14, doubleResult);

            var structureResult = await api.GetObjectValueAsync("object-flag", new Value("default"));
            Assert.Equal("default", structureResult.AsString);
        }

        [Fact]
        public async Task AddLaunchDarkly_ProviderReturnsCorrectReasonInOfflineMode()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.AddLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, false);

            // Assert
            Assert.False(result.Value);
            Assert.Equal(Reason.Default, result.Reason);
        }

        [Fact]
        public void AddLaunchDarkly_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.AddLaunchDarkly(TestSdkKey));
        }

        [Fact]
        public void AddLaunchDarkly_WithNullBuilderForDomain_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.AddLaunchDarkly(TestDomain, TestSdkKey));
        }

        [Fact]
        public void AddLaunchDarkly_CustomConfigurationIsApplied()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(1);

            // Act
            builder.AddLaunchDarkly(TestSdkKey, cfg =>
            {
                cfg.Offline(true);
                cfg.StartWaitTime(startWaitTime);
            });

            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.True(config.Offline);
            Assert.Equal(startWaitTime, config.StartWaitTime);
        }

        [Fact]
        public void AddLaunchDarkly_DomainCustomConfigurationIsApplied()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(2);

            // Act
            builder.AddLaunchDarkly(TestDomain, TestSdkKey, cfg =>
            {
                cfg.Offline(true);
                cfg.StartWaitTime(startWaitTime);
            });

            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.True(config.Offline);
            Assert.Equal(startWaitTime, config.StartWaitTime);
        }
    }
} 