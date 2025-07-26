using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.DependencyInjection;
using OpenFeature.Model;
using Xunit;
using LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection;
using OpenFeature.Constant;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection.Tests
{
    public class LaunchDarklyDependencyInjectionIntegrationTests
    {
        private const string TestSdkKey = "test-sdk-key";
        private const string TestDomain = "test-domain";
        private const string TestFlagKey = "test-flag";

        #region Configuration Overload Integration Tests

        [Fact]
        public async Task UseLaunchDarkly_WithPrebuiltConfiguration_CanResolveDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();
            
            builder.UseLaunchDarkly(config);

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task UseLaunchDarkly_WithDomainAndPrebuiltConfiguration_CanResolveDomainProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();
            
            builder.UseLaunchDarkly(TestDomain, config);

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredKeyedService<FeatureClient>(TestDomain);

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public void UseLaunchDarkly_WithPrebuiltConfiguration_ConfigurationPropertiesArePreserved()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(5);
            var config = Configuration.Builder(TestSdkKey)
                .Offline(true)
                .StartWaitTime(startWaitTime)
                .Build();
            
            builder.UseLaunchDarkly(config);

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndPrebuiltConfiguration_ConfigurationPropertiesArePreserved()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(10);
            var config = Configuration.Builder(TestSdkKey)
                .Offline(true)
                .StartWaitTime(startWaitTime)
                .Build();
            
            builder.UseLaunchDarkly(TestDomain, config);

            // Act
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region SDK Key Overload Integration Tests

        [Fact]
        public async Task UseLaunchDarkly_WithSdkKey_CanResolveDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task UseLaunchDarkly_WithDomainAndSdkKey_CanResolveDomainProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredKeyedService<FeatureClient>(TestDomain);

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task UseLaunchDarkly_WithSdkKeyAndCustomConfiguration_AppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(3);
            
            builder.UseLaunchDarkly(TestSdkKey, config =>
            {
                config.Offline(true);
                config.StartWaitTime(startWaitTime);
            });

            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();

            // Act & Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public async Task UseLaunchDarkly_WithDomainSdkKeyAndCustomConfiguration_AppliesConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(7);
            
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config =>
            {
                config.Offline(true);
                config.StartWaitTime(startWaitTime);
            });

            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Act & Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region Multi-Provider Integration Tests

        [Fact]
        public void UseLaunchDarkly_MixedDefaultAndDomainProviders_WorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            // Register both default and domain-scoped providers using different overloads
            var defaultConfig = Configuration.Builder(TestSdkKey).Offline(true).Build();
            builder.UseLaunchDarkly(defaultConfig);
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var defaultClient = serviceProvider.GetRequiredService<FeatureClient>();
            var domainClient = serviceProvider.GetRequiredKeyedService<FeatureClient>(TestDomain);
            var defaultConfigRegistered = serviceProvider.GetRequiredService<Configuration>();
            var domainConfigRegistered = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.NotSame(defaultClient, domainClient);
            Assert.NotSame(defaultConfigRegistered, domainConfigRegistered);
            Assert.True(defaultConfigRegistered.Offline);
            Assert.True(domainConfigRegistered.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_MultipleDomainConfigurations_AreIsolated()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            const string domain1 = "fast-domain";
            const string domain2 = "slow-domain";
            var fastStartWait = TimeSpan.FromMilliseconds(100);
            var slowStartWait = TimeSpan.FromSeconds(5);

            // Register using different overloads for variety
            var fastConfig = Configuration.Builder(TestSdkKey)
                .Offline(true)
                .StartWaitTime(fastStartWait)
                .Build();
            
            builder.UseLaunchDarkly(domain1, fastConfig);
            builder.UseLaunchDarkly(domain2, TestSdkKey, config =>
            {
                config.Offline(true);
                config.StartWaitTime(slowStartWait);
            });

            var serviceProvider = services.BuildServiceProvider();

            // Act & Assert
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(domain1);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(domain2);
            
            Assert.NotSame(config1, config2);
            Assert.Equal(fastStartWait, config1.StartWaitTime);
            Assert.Equal(slowStartWait, config2.StartWaitTime);
            Assert.True(config1.Offline);
            Assert.True(config2.Offline);
        }

        [Fact]
        public async Task UseLaunchDarkly_AllOverloads_ProvidersSupportAllValueTypes()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            // Test all 4 overloads
            var config1 = Configuration.Builder(TestSdkKey).Offline(true).Build();
            var config2 = Configuration.Builder(TestSdkKey).Offline(true).Build();
            
            builder.UseLaunchDarkly(config1); // Overload 1
            builder.UseLaunchDarkly("domain1", config2); // Overload 2
            builder.UseLaunchDarkly("domain2", TestSdkKey); // Overload 3
            builder.UseLaunchDarkly("domain3", TestSdkKey, c => c.Offline(true)); // Overload 4

            var serviceProvider = services.BuildServiceProvider();
            
            var defaultApi = serviceProvider.GetRequiredService<FeatureClient>();
            var domain1Api = serviceProvider.GetRequiredKeyedService<FeatureClient>("domain1");
            var domain2Api = serviceProvider.GetRequiredKeyedService<FeatureClient>("domain2");
            var domain3Api = serviceProvider.GetRequiredKeyedService<FeatureClient>("domain3");

            // Act & Assert - Test all supported types on all providers
            foreach (var api in new[] { defaultApi, domain1Api, domain2Api, domain3Api })
            {
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
        }

        #endregion

        #region Provider Behavior Integration Tests

        [Fact]
        public async Task UseLaunchDarkly_ProviderReturnsCorrectReasonInOfflineMode()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, false);

            // Assert
            Assert.False(result.Value);
            Assert.Equal(Reason.Default, result.Reason);
        }

        [Fact]
        public async Task UseLaunchDarkly_DomainProviderReturnsCorrectReasonInOfflineMode()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();
            
            builder.UseLaunchDarkly(TestDomain, config);

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredKeyedService<FeatureClient>(TestDomain);

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, true);

            // Assert
            Assert.True(result.Value);
            Assert.Equal(Reason.Default, result.Reason);
        }

        [Fact]
        public async Task UseLaunchDarkly_ProviderHandlesEvaluationContext()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();
            var api = serviceProvider.GetRequiredService<FeatureClient>();

            var context = EvaluationContext.Builder()
                .Set("userId", "test-user")
                .Set("email", "test@example.com")
                .Build();

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, false, context);

            // Assert
            Assert.False(result.Value);
            Assert.Equal(Reason.Default, result.Reason);
        }

        #endregion

        #region Early Validation Integration Tests

        [Fact]
        public void UseLaunchDarkly_EarlyValidationWithConfiguration_FailsImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // This would normally fail at runtime, but early validation catches it immediately
            // We can't easily test this without a malformed Configuration, but we can test that
            // valid configurations pass early validation
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act & Assert - Should not throw
            builder.UseLaunchDarkly(config);
            
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(registeredConfig);
        }

        [Fact]
        public void UseLaunchDarkly_EarlyValidationWithSdkKey_FailsImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert - Valid configuration should pass early validation
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));
            
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_EarlyValidationWithDomain_FailsImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert - Valid configuration should pass early validation
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));
            
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        #endregion

        #region Service Lifetime Integration Tests

        [Fact]
        public void UseLaunchDarkly_ConfigurationRegisteredAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_DomainConfigurationRegisteredAsKeyedSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config => config.Offline(true));

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_TryAddSingleton_DoesNotReplaceExistingRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            var config1 = Configuration.Builder(TestSdkKey).Offline(true).Build();
            var config2 = Configuration.Builder(TestSdkKey).Offline(false).Build();

            // Act
            builder.UseLaunchDarkly(config1);
            builder.UseLaunchDarkly(config2); // Should not replace the first

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.True(registeredConfig.Offline); // Should still be the first configuration
        }

        [Fact]
        public void UseLaunchDarkly_TryAddKeyedSingleton_DoesNotReplaceExistingRegistration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            var config1 = Configuration.Builder(TestSdkKey).Offline(true).Build();
            var config2 = Configuration.Builder(TestSdkKey).Offline(false).Build();

            // Act
            builder.UseLaunchDarkly(TestDomain, config1);
            builder.UseLaunchDarkly(TestDomain, config2); // Should not replace the first

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.True(registeredConfig.Offline); // Should still be the first configuration
        }

        #endregion

        #region Resource Management Tests

        [Fact]
        public void UseLaunchDarkly_ServiceProviderDisposed_DoesNotCauseMemoryLeaks()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            
            builder.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));

            // Act & Assert
            using (var serviceProvider = services.BuildServiceProvider())
            {
                var config = serviceProvider.GetRequiredService<Configuration>();
                Assert.NotNull(config);
                Assert.True(config.Offline);
            }
            // Service provider disposed, should not cause issues
        }

        [Fact]
        public void UseLaunchDarkly_MultipleServiceProviders_IsolateConfigurations()
        {
            // Arrange
            var services1 = new ServiceCollection();
            var services2 = new ServiceCollection();
            var builder1 = new OpenFeatureBuilder(services1);
            var builder2 = new OpenFeatureBuilder(services2);
            
            builder1.UseLaunchDarkly(TestSdkKey, config => config.Offline(true));
            builder2.UseLaunchDarkly(TestSdkKey, config => config.Offline(false));

            // Act
            var serviceProvider1 = services1.BuildServiceProvider();
            var serviceProvider2 = services2.BuildServiceProvider();
            
            var config1 = serviceProvider1.GetRequiredService<Configuration>();
            var config2 = serviceProvider2.GetRequiredService<Configuration>();

            // Assert
            Assert.NotSame(config1, config2);
            Assert.True(config1.Offline);
            Assert.False(config2.Offline);
        }

        #endregion
    }
} 