using System;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature.DependencyInjection;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection.Tests
{
    public class OpenFeatureBuilderExtensionsTests
    {
        private const string TestSdkKey = "test-sdk-key";
        private const string TestDomain = "test-domain";

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
            
            // Build the service provider to verify registrations
            var serviceProvider = services.BuildServiceProvider(true);
            
            // Verify Configuration is registered as singleton
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
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
                configBuilder.Offline(true); // Set some configuration for testing
            });

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify configuration was applied
            var serviceProvider = services.BuildServiceProvider(true);
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
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(null));
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
        public void UseLaunchDarkly_WithDomainAndSdkKey_RegistersDomainScopedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.UseLaunchDarkly(TestDomain, TestSdkKey);

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify registrations
            var serviceProvider = services.BuildServiceProvider(true);
            
            // Verify Configuration is registered as keyed singleton
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
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
                configBuilder.Offline(true); // Set some configuration for testing
            });

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify configuration was applied
            var serviceProvider = services.BuildServiceProvider(true);
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void UseLaunchDarkly_WithNullDomain_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(null, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithEmptyDomain_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(string.Empty, TestSdkKey));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndNullSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(TestDomain, null));
        }

        [Fact]
        public void UseLaunchDarkly_WithDomainAndEmptySdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            try
            {
                builder.UseLaunchDarkly(TestDomain, string.Empty);
            }
            catch (Exception ex)
            {

            }

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.UseLaunchDarkly(TestDomain, string.Empty));
        }

        [Fact]
        public void UseLaunchDarkly_MultipleCallsWithSameSdkKey_UsesSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.UseLaunchDarkly(TestSdkKey);
            builder.UseLaunchDarkly(TestSdkKey); // Second call should not replace the first due to TryAddSingleton

            // Assert
            var serviceProvider = services.BuildServiceProvider(true);
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_MultipleCallsWithSameDomain_UsesSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.UseLaunchDarkly(TestDomain, TestSdkKey);
            builder.UseLaunchDarkly(TestDomain, TestSdkKey); // Second call should not replace the first due to TryAddKeyedSingleton

            // Assert
            var serviceProvider = services.BuildServiceProvider(true);
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_WithDifferentDomains_RegistersSeparateConfigurations()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            const string domain1 = "domain1";
            const string domain2 = "domain2";

            // Act
            builder.UseLaunchDarkly(domain1, TestSdkKey);
            builder.UseLaunchDarkly(domain2, TestSdkKey);

            // Assert
            var serviceProvider = services.BuildServiceProvider(true);
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(domain1);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(domain2);
            Assert.NotSame(config1, config2);
        }

        [Fact]
        public void UseLaunchDarkly_ConfigurationDelegateException_PropagatesException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act
            builder.UseLaunchDarkly(TestSdkKey, _ => throw expectedException);

            // Assert
            var serviceProvider = services.BuildServiceProvider(true);
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                serviceProvider.GetRequiredService<Configuration>());
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void UseLaunchDarkly_DomainConfigurationDelegateException_PropagatesException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act
            builder.UseLaunchDarkly(TestDomain, TestSdkKey, _ => throw expectedException);

            // Assert
            var serviceProvider = services.BuildServiceProvider(true);
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain));
            Assert.Same(expectedException, actualException);
        }
    }
} 