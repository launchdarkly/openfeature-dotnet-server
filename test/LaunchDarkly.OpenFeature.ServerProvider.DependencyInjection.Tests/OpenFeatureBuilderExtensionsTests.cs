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
        public void AddLaunchDarkly_WithSdkKey_RegistersDefaultProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.AddLaunchDarkly(TestSdkKey);

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify registrations
            var serviceProvider = services.BuildServiceProvider();
            
            // Verify Configuration is registered as singleton
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
        }

        [Fact]
        public void AddLaunchDarkly_WithSdkKeyAndConfiguration_RegistersDefaultProviderWithConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var configureWasCalled = false;
            ConfigurationBuilder capturedBuilder = null;

            // Act
            var result = builder.AddLaunchDarkly(TestSdkKey, configBuilder =>
            {
                configureWasCalled = true;
                capturedBuilder = configBuilder;
                configBuilder.Offline(true); // Set some configuration for testing
            });

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify configuration was applied
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<Configuration>();
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void AddLaunchDarkly_WithNullSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(null));
        }

        [Fact]
        public void AddLaunchDarkly_WithEmptySdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(string.Empty));
        }

        [Fact]
        public void AddLaunchDarkly_WithDomainAndSdkKey_RegistersDomainScopedProvider()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            var result = builder.AddLaunchDarkly(TestDomain, TestSdkKey);

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify registrations
            var serviceProvider = services.BuildServiceProvider();
            
            // Verify Configuration is registered as keyed singleton
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
        }

        [Fact]
        public void AddLaunchDarkly_WithDomainSdkKeyAndConfiguration_RegistersDomainScopedProviderWithConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var configureWasCalled = false;
            ConfigurationBuilder capturedBuilder = null;

            // Act
            var result = builder.AddLaunchDarkly(TestDomain, TestSdkKey, configBuilder =>
            {
                configureWasCalled = true;
                capturedBuilder = configBuilder;
                configBuilder.Offline(true); // Set some configuration for testing
            });

            // Assert
            Assert.Same(builder, result);
            
            // Build the service provider to verify configuration was applied
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            
            Assert.True(configureWasCalled);
            Assert.NotNull(capturedBuilder);
            Assert.True(config.Offline);
        }

        [Fact]
        public void AddLaunchDarkly_WithNullDomain_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(null, TestSdkKey));
        }

        [Fact]
        public void AddLaunchDarkly_WithEmptyDomain_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(string.Empty, TestSdkKey));
        }

        [Fact]
        public void AddLaunchDarkly_WithDomainAndNullSdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(TestDomain, null));
        }

        [Fact]
        public void AddLaunchDarkly_WithDomainAndEmptySdkKey_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddLaunchDarkly(TestDomain, string.Empty));
        }

        [Fact]
        public void AddLaunchDarkly_MultipleCallsWithSameSdkKey_UsesSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.AddLaunchDarkly(TestSdkKey);
            builder.AddLaunchDarkly(TestSdkKey); // Second call should not replace the first due to TryAddSingleton

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();
            Assert.Same(config1, config2);
        }

        [Fact]
        public void AddLaunchDarkly_MultipleCallsWithSameDomain_UsesSameConfigurationInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);

            // Act
            builder.AddLaunchDarkly(TestDomain, TestSdkKey);
            builder.AddLaunchDarkly(TestDomain, TestSdkKey); // Second call should not replace the first due to TryAddKeyedSingleton

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            Assert.Same(config1, config2);
        }

        [Fact]
        public void AddLaunchDarkly_WithDifferentDomains_RegistersSeparateConfigurations()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            const string domain1 = "domain1";
            const string domain2 = "domain2";

            // Act
            builder.AddLaunchDarkly(domain1, TestSdkKey);
            builder.AddLaunchDarkly(domain2, TestSdkKey);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(domain1);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(domain2);
            Assert.NotSame(config1, config2);
        }

        [Fact]
        public void AddLaunchDarkly_ConfigurationDelegateException_PropagatesException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act
            builder.AddLaunchDarkly(TestSdkKey, _ => throw expectedException);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                serviceProvider.GetRequiredService<Configuration>());
            Assert.Same(expectedException, actualException);
        }

        [Fact]
        public void AddLaunchDarkly_DomainConfigurationDelegateException_PropagatesException()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = new OpenFeatureBuilder(services);
            var expectedException = new InvalidOperationException("Test exception");

            // Act
            builder.AddLaunchDarkly(TestDomain, TestSdkKey, _ => throw expectedException);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var actualException = Assert.Throws<InvalidOperationException>(() => 
                serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain));
            Assert.Same(expectedException, actualException);
        }
    }
} 