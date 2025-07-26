using System;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature.DependencyInjection;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection.Tests
{
    public class LaunchDarklyOpenFeatureBuilderExtensionsTests
    {
        private const string TestSdkKey = "test-sdk-key";
        private const string TestDomain = "test-domain";

        #region UseLaunchDarkly(Configuration) - Default Provider Tests

        [Fact]
        public void UseLaunchDarklyWithConfiguration_WhenCalled_ShouldReturnSameBuilderInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            var result = builder.UseLaunchDarkly(config);

            // Assert
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseLaunchDarklyWithConfiguration_WhenCalled_ShouldRegisterConfigurationAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(config);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithConfiguration_WhenCalledMultipleTimes_ShouldShareSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(config);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarklyWithConfiguration_WhenCalledMultipleTimesWithDifferentConfigs_ShouldUseFirstConfiguration()
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
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.True(registeredConfig.Offline); // Should still be the first configuration
        }

        #endregion

        #region UseLaunchDarkly(domain, Configuration) - Domain Provider Tests

        [Fact]
        public void UseLaunchDarklyWithDomainAndConfiguration_WhenCalled_ShouldReturnSameBuilderInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, config);

            // Assert
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndConfiguration_WhenCalled_ShouldRegisterConfigurationAsKeyedSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(TestDomain, config);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndConfiguration_WhenCalledMultipleTimes_ShouldShareSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(TestDomain, config);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void UseLaunchDarklyWithDomainAndConfiguration_WhenDomainIsNullOrWhitespace_ShouldReturnBuilder(string domain)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act
            var result = builder.UseLaunchDarkly(domain, config);

            // Assert
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndConfiguration_WhenDifferentDomainsUsed_ShouldRegisterSeparateConfigurations()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config1 = Configuration.Builder(TestSdkKey).Offline(true).Build();
            var config2 = Configuration.Builder(TestSdkKey).Offline(false).Build();
            const string domain1 = "domain1";
            const string domain2 = "domain2";

            // Act
            builder.UseLaunchDarkly(domain1, config1);
            builder.UseLaunchDarkly(domain2, config2);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig1 = serviceProvider.GetRequiredKeyedService<Configuration>(domain1);
            var registeredConfig2 = serviceProvider.GetRequiredKeyedService<Configuration>(domain2);
            Assert.NotSame(registeredConfig1, registeredConfig2);
            Assert.True(registeredConfig1.Offline);
            Assert.False(registeredConfig2.Offline);
        }

        #endregion

        #region UseLaunchDarkly(sdkKey) - SDK Key Default Provider Tests

        [Fact]
        public void UseLaunchDarklyWithSdkKey_WhenCalled_ShouldReturnSameBuilderInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestSdkKey);

            // Assert
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseLaunchDarklyWithSdkKey_WhenCalled_ShouldRegisterValidConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.UseLaunchDarkly(TestSdkKey);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(config);
        }

        [Fact]
        public void UseLaunchDarklyWithSdkKeyAndDelegate_WhenCalled_ShouldApplyCustomConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var configureWasCalled = false;
            ConfigurationBuilder capturedBuilder = null;

            // Act
            var result = builder.UseLaunchDarkly(TestSdkKey, configBuilder =>
            {
                configureWasCalled = true;
                capturedBuilder = configBuilder;
                configBuilder.Offline(true);
            });

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredService<Configuration>();
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithSdkKeyAndDelegate_WhenConfigurationDelegateThrows_ShouldPropagateExceptionImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act & Assert
            // The exception should be thrown immediately during registration due to early validation
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                builder.UseLaunchDarkly(TestSdkKey, _ => throw expectedException));
            Assert.Same(expectedException, actualException);
        }

        #endregion

        #region UseLaunchDarkly(domain, sdkKey) - SDK Key Domain Provider Tests

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKey_WhenCalled_ShouldReturnSameBuilderInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey);

            // Assert
            Assert.Same(builder, result);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKey_WhenCalled_ShouldRegisterValidKeyedConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.UseLaunchDarkly(TestDomain, TestSdkKey);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKeyAndDelegate_WhenCalled_ShouldApplyCustomConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var configureWasCalled = false;

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey, configBuilder =>
            {
                configureWasCalled = true;
                configBuilder.Offline(true);
            });

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            
            Assert.True(configureWasCalled);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKeyAndDelegate_WhenConfigurationDelegateThrows_ShouldPropagateExceptionImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act
            InvalidOperationException registerWithThrowing() => Assert.Throws<InvalidOperationException>(() =>
                builder.UseLaunchDarkly(TestDomain, TestSdkKey, _ => throw expectedException));

            // Assert
            // The exception should be thrown immediately during registration due to early validation
            var actualException = registerWithThrowing();
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKeyAndDelegate_WhenNullConfigurationDelegate_ShouldNotThrowAndReturnBuilder()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert - should not throw
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey, null);
            
            Assert.Same(builder, result);
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
        }

        [Fact]
        public void UseLaunchDarklyWithSdkKeyAndNullDelegate_WhenNullConfigurationPassed_ShouldThrowNullReferenceException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            Configuration configuration = null;

            // Assert
            Assert.Throws<NullReferenceException>(() => builder.UseLaunchDarkly(TestSdkKey, configuration));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void UseLaunchDarklyWithDomainAndSdkKey_WhenSdkKeyIsNullOrWhitespace_ShouldReturnBuilder(string sdkKey)
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, sdkKey);

            // Assert
            Assert.Same(builder, result);
        }

        #endregion

        #region Null Builder Validation Tests

        [Fact]
        public void UseLaunchDarklyWithNullBuilder_WhenSdkKeyOverloadUsed_ShouldThrowNullReferenceException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => builder.UseLaunchDarkly(TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarklyWithNullBuilder_WhenConfigurationOverloadUsed_ShouldThrowNullReferenceException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => builder.UseLaunchDarkly(config));
        }

        [Fact]
        public void UseLaunchDarklyWithNullBuilder_WhenDomainSdkKeyOverloadUsed_ShouldThrowNullReferenceException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => builder.UseLaunchDarkly(TestDomain, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarklyWithNullBuilder_WhenDomainConfigurationOverloadUsed_ShouldThrowNullReferenceException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => builder.UseLaunchDarkly(TestDomain, config));
        }

        #endregion

        #region Configuration Property Preservation Tests

        [Fact]
        public void UseLaunchDarklyWithConfiguration_WhenCustomPropertiesSet_ShouldPreserveConfigurationProperties()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var originalConfig = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(originalConfig);

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            
            // The registered config should be a rebuilt version, not the same instance
            // but should have the same properties
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithSdkKeyAndDelegate_WhenCustomPropertiesSet_ShouldPreserveConfigurationProperties()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(5);

            // Act
            builder.UseLaunchDarkly(TestSdkKey, config =>
            {
                config.Offline(true);
                config.StartWaitTime(startWaitTime);
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKeyAndDelegate_WhenCustomPropertiesSet_ShouldPreserveConfigurationProperties()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var startWaitTime = TimeSpan.FromSeconds(10);

            // Act
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, config =>
            {
                config.Offline(true);
                config.StartWaitTime(startWaitTime);
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region Early Validation Behavior Tests

        [Fact]
        public void UseLaunchDarklyWithSdkKeyAndDelegate_WhenEarlyValidationPasses_ShouldPreventRuntimeFailures()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act - Early validation should catch configuration issues immediately
            builder.UseLaunchDarkly(TestSdkKey, cfg => cfg.Offline(true));

            // Assert - If we reach here, early validation passed
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(config);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarklyWithDomainAndSdkKeyAndDelegate_WhenEarlyValidationPasses_ShouldPreventRuntimeFailures()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act - Early validation should catch configuration issues immediately
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, cfg => cfg.Offline(true));

            // Assert - If we reach here, early validation passed
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
            Assert.True(config.Offline);
        }

        #endregion
    }
}
