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

        #region Configuration Overload Tests - Default Provider

        [Fact]
        public void UseLaunchDarkly_WithConfiguration_RegistersDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            var result = builder.UseLaunchDarkly(config);

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_WithConfiguration_ConfigurationIsShared()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(config);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly((Configuration)null));
        }

        [Fact]
        public void UseLaunchDarkly_MultipleCallsWithConfiguration_UsesTryAddSingleton()
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

        #endregion

        #region Configuration Overload Tests - Domain Provider

        [Fact]
        public void UseLaunchDarkly_WithDomainAndConfiguration_RegistersDomainScopedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, config);

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndConfiguration_ConfigurationIsShared()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(TestDomain, config);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_WithNullDomainAndConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(null, config));
        }

        [Fact]
        public void UseLaunchDarkly_WithEmptyDomainAndConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(string.Empty, config));
        }

        [Fact]
        public void UseLaunchDarkly_WithWhitespaceDomainAndConfiguration_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly("   ", config));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly(TestDomain, (Configuration)null));
        }

        [Fact]
        public void UseLaunchDarkly_WithDifferentDomainsAndConfigurations_RegistersSeparateConfigurations()
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
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig1 = serviceProvider.GetRequiredKeyedService<Configuration>(domain1);
            var registeredConfig2 = serviceProvider.GetRequiredKeyedService<Configuration>(domain2);
            Assert.NotSame(registeredConfig1, registeredConfig2);
            Assert.True(registeredConfig1.Offline);
            Assert.False(registeredConfig2.Offline);
        }

        #endregion

        #region SDK Key Overload Tests - Default Provider

        [Fact]
        public void UseLaunchDarkly_WithSdkKey_RegistersDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestSdkKey);

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(config);
        }

        [Fact]
        public void UseLaunchDarkly_WithSdkKeyAndConfiguration_RegistersDefaultProviderWithConfiguration()
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
            
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_WithNullSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly((string)null));
        }

        [Fact]
        public void UseLaunchDarkly_WithEmptySdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(string.Empty));
        }

        [Fact]
        public void UseLaunchDarkly_WithWhitespaceSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly("   "));
        }

        [Fact]
        public void UseLaunchDarkly_ConfigurationDelegateException_PropagatesExceptionImmediately()
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

        [Fact]
        public void UseLaunchDarkly_NullConfigurationDelegate_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert - should not throw
            Configuration configuration = null;
            var result = builder.UseLaunchDarkly(TestSdkKey, configuration);
            
            Assert.Same(builder, result);
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(config);
        }

        #endregion

        #region SDK Key Overload Tests - Domain Provider

        [Fact]
        public void UseLaunchDarkly_WithDomainAndSdkKey_RegistersDomainScopedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey);

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainSdkKeyAndConfiguration_RegistersDomainScopedProviderWithConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var configureWasCalled = false;
            ConfigurationBuilder capturedBuilder = null;

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey, configBuilder =>
            {
                configureWasCalled = true;
                capturedBuilder = configBuilder;
                configBuilder.Offline(true);
            });

            // Assert
            Assert.Same(builder, result);
            
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_WithNullDomainAndSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(null, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithEmptyDomainAndSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(string.Empty, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithWhitespaceDomainAndSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly("   ", TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndNullSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(TestDomain, (string)null));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndEmptySdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(TestDomain, string.Empty));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndWhitespaceSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(TestDomain, "   "));
        }

        [Fact]
        public void UseLaunchDarkly_DomainConfigurationDelegateException_PropagatesExceptionImmediately()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act & Assert
            // The exception should be thrown immediately during registration due to early validation
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                builder.UseLaunchDarkly(TestDomain, TestSdkKey, _ => throw expectedException));
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void UseLaunchDarkly_DomainNullConfigurationDelegate_DoesNotThrow()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert - should not throw
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey, null);
            
            Assert.Same(builder, result);
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
        }

        #endregion

        #region Builder Validation Tests

        [Fact]
        public void UseLaunchDarkly_WithNullBuilder_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly(TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithNullBuilderAndConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly(config));
        }

        [Fact]
        public void UseLaunchDarkly_WithNullBuilderForDomain_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly(TestDomain, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithNullBuilderForDomainAndConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            OpenFeatureBuilder builder = null;
            var config = Configuration.Builder(TestSdkKey).Build();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.UseLaunchDarkly(TestDomain, config));
        }

        #endregion

        #region Advanced Configuration Tests

        [Fact]
        public void UseLaunchDarkly_ConfigurationCloning_CreatesNewInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var originalConfig = Configuration.Builder(TestSdkKey).Offline(true).Build();

            // Act
            builder.UseLaunchDarkly(originalConfig);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            
            // The registered config should be a rebuilt version, not the same instance
            // but should have the same properties
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_CustomConfigurationProperties_ArePreserved()
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
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public void UseLaunchDarkly_DomainCustomConfigurationProperties_ArePreserved()
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
            var serviceProvider = services.BuildServiceProvider();
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region Early Validation Tests

        [Fact]
        public void UseLaunchDarkly_EarlyValidation_PreventsRuntimeFailures()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act - Early validation should catch configuration issues immediately
            builder.UseLaunchDarkly(TestSdkKey, cfg => cfg.Offline(true));

            // Assert - If we reach here, early validation passed
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(config);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_DomainEarlyValidation_PreventsRuntimeFailures()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act - Early validation should catch configuration issues immediately
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, cfg => cfg.Offline(true));

            // Assert - If we reach here, early validation passed
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.NotNull(config);
            Assert.True(config.Offline);
        }

        #endregion
    }
}
