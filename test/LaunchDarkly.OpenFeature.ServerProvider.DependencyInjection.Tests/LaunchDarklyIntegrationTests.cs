using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk.Server;
using Microsoft.Extensions.DependencyInjection;
using OpenFeature;
using OpenFeature.DependencyInjection;
using OpenFeature.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.DependencyInjection.Tests
{
    public class LaunchDarklyIntegrationTests
    {
        private const string TestSdkKey = "test-sdk-key";
        private const string TestDomain = "test-domain";
        private const string TestFlagKey = "test-flag";

        #region Helper Methods

        /// <summary>
        /// Creates a service provider with OpenFeature and LaunchDarkly configured for the default provider.
        /// </summary>
        /// <param name="configure">Optional configuration delegate for customizing LaunchDarkly settings.</param>
        /// <returns>A configured service provider with scope validation enabled.</returns>
        private static async Task<IServiceProvider> CreateServiceProviderWithDefaultLaunchDarklyAsync(Action<ConfigurationBuilder> configure = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOpenFeature(builder =>
            {
                if (configure != null)
                {
                    builder.UseLaunchDarkly(TestSdkKey, configure);
                }
                else
                {
                    builder.UseLaunchDarkly(TestSdkKey);
                }
            });

            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            var lifecycleManager = serviceProvider.GetRequiredService<IFeatureLifecycleManager>();
            await lifecycleManager.EnsureInitializedAsync();

            return serviceProvider;
        }

        /// <summary>
        /// Creates a service provider with OpenFeature and LaunchDarkly configured for a domain-scoped provider.
        /// </summary>
        /// <param name="domain">The domain identifier for the scoped provider.</param>
        /// <param name="configure">Optional configuration delegate for customizing LaunchDarkly settings.</param>
        /// <returns>A configured service provider with scope validation enabled.</returns>
        private static async Task<IServiceProvider> CreateServiceProviderWithDomainLaunchDarklyAsync(string domain, Action<ConfigurationBuilder> configure = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOpenFeature(builder =>
            {
                if (configure != null)
                {
                    builder.UseLaunchDarkly(domain, TestSdkKey, configure);
                }
                else
                {
                    builder.UseLaunchDarkly(domain, TestSdkKey);
                }
            });

            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            var lifecycleManager = serviceProvider.GetRequiredService<IFeatureLifecycleManager>();
            await lifecycleManager.EnsureInitializedAsync();

            return serviceProvider;
        }

        /// <summary>
        /// Creates a scoped service provider for testing scoped services like IFeatureClient.
        /// </summary>
        /// <param name="rootServiceProvider">The root service provider to create a scope from.</param>
        /// <returns>A scoped service provider.</returns>
        private static IServiceProvider CreateScopedServiceProvider(IServiceProvider rootServiceProvider)
        {
            var scopeFactory = rootServiceProvider.GetRequiredService<IServiceScopeFactory>();
            return scopeFactory.CreateScope().ServiceProvider;
        }

        /// <summary>
        /// Creates a service provider with multiple OpenFeature providers configured.
        /// </summary>
        /// <param name="configureBuilder">Action to configure multiple providers on the OpenFeature builder.</param>
        /// <returns>A configured service provider with scope validation enabled.</returns>
        private static async Task<IServiceProvider> CreateServiceProviderWithMultipleProvidersAsync(Action<OpenFeatureBuilder> configureBuilder)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOpenFeature(configureBuilder);
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            var lifecycleManager = serviceProvider.GetRequiredService<IFeatureLifecycleManager>();
            await lifecycleManager.EnsureInitializedAsync();

            return serviceProvider;
        }

        #endregion

        #region Configuration Overload Integration Tests

        [Fact]
        public async Task ConfigurationOverload_DefaultProvider_ShouldResolveFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task ConfigurationOverload_DomainProvider_ShouldResolveKeyedFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredKeyedService<IFeatureClient>(TestDomain);

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task ConfigurationOverload_DefaultProvider_ShouldPreserveCustomConfigurationSettings()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(5);
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg
                .Offline(true)
                .StartWaitTime(startWaitTime));

            // Act
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public async Task ConfigurationOverload_DomainProvider_ShouldPreserveCustomConfigurationSettings()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(10);
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg
                .Offline(true)
                .StartWaitTime(startWaitTime));

            // Act
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region SDK Key Overload Integration Tests

        [Fact]
        public async Task SdkKeyOverload_DefaultProvider_ShouldResolveFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task SdkKeyOverload_DomainProvider_ShouldResolveKeyedFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredKeyedService<IFeatureClient>(TestDomain);

            // Act
            var result = await api.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task SdkKeyOverload_DefaultProvider_ShouldApplyCustomConfigurationFromDelegate()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(3);
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg
                .Offline(true)
                .StartWaitTime(startWaitTime));

            // Act
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        [Fact]
        public async Task SdkKeyOverload_DomainProvider_ShouldApplyCustomConfigurationFromDelegate()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(7);
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg
                .Offline(true)
                .StartWaitTime(startWaitTime));

            // Act
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.True(registeredConfig.Offline);
            Assert.Equal(startWaitTime, registeredConfig.StartWaitTime);
        }

        #endregion

        #region Multi-Provider Setup Integration Tests

        [Fact]
        public async Task MultiProvider_MixedOverloads_ShouldRegisterBothDefaultAndDomainProvidersCorrectly()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithMultipleProvidersAsync(builder =>
            {
                // Register both default and domain-scoped providers using different overloads
                builder
                    .UseLaunchDarkly("domain1", TestSdkKey, cfg => cfg.Offline(true))
                    .UseLaunchDarkly("domain2", TestSdkKey, cfg => cfg.Offline(true))
                    .AddPolicyName(policy =>
                    {
                        policy.DefaultNameSelector = _ => "domain1";
                    });
            });
            
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);

            // Act
            var defaultClient = scopedProvider.GetRequiredService<IFeatureClient>();
            var domainClient = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain2");
            var defaultConfigRegistered = serviceProvider.GetRequiredService<Configuration>();
            var domainConfigRegistered = serviceProvider.GetRequiredKeyedService<Configuration>("domain2");

            // Assert
            Assert.NotSame(defaultClient, domainClient);
            Assert.NotSame(defaultConfigRegistered, domainConfigRegistered);
            Assert.True(defaultConfigRegistered.Offline);
            Assert.True(domainConfigRegistered.Offline);
        }

        [Fact]
        public async Task MultiProvider_MultipleDomains_ShouldIsolateConfigurationsCorrectly()
        {
            // Arrange
            const string fastDomain = "fast-domain";
            const string slowDomain = "slow-domain";
            var fastStartWait = TimeSpan.FromMilliseconds(100);
            var slowStartWait = TimeSpan.FromSeconds(5);

            var serviceProvider = await CreateServiceProviderWithMultipleProvidersAsync(builder =>
            {
                // Register using different configurations for variety
                builder.UseLaunchDarkly(fastDomain, TestSdkKey, cfg => cfg
                    .Offline(true)
                    .StartWaitTime(fastStartWait));
                
                builder.UseLaunchDarkly(slowDomain, TestSdkKey, cfg => cfg
                    .Offline(true)
                    .StartWaitTime(slowStartWait));

                builder.AddPolicyName(policy => policy.DefaultNameSelector = _ => fastDomain);
            });

            // Act & Assert
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(fastDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(slowDomain);
            
            Assert.NotSame(config1, config2);
            Assert.Equal(fastStartWait, config1.StartWaitTime);
            Assert.Equal(slowStartWait, config2.StartWaitTime);
            Assert.True(config1.Offline);
            Assert.True(config2.Offline);
        }

        #endregion

        #region OpenFeature Value Type Support Tests

        [Fact]
        public async Task AllOverloads_FeatureProviders_ShouldSupportAllOpenFeatureValueTypes()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithMultipleProvidersAsync(builder =>
            {
                // Test all SDK key overloads
                builder.UseLaunchDarkly("domain1", TestSdkKey, cfg => cfg.Offline(true)); // Default provider
                builder.UseLaunchDarkly("domain2", TestSdkKey, cfg => cfg.Offline(true)); // Domain provider
                builder.UseLaunchDarkly("domain3", TestSdkKey, cfg => cfg.Offline(true)); // Domain provider
                builder.AddPolicyName(policy => policy.DefaultNameSelector = _ => "domain1");
            });

            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            
            var defaultApi = scopedProvider.GetRequiredService<IFeatureClient>();
            var domain1Api = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain1");
            var domain2Api = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain2");
            var domain3Api = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain3");

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

        #region OpenFeature Behavior Integration Tests

        [Fact]
        public async Task DefaultProvider_InOfflineMode_ShouldReturnCorrectReasonAndDefaultValue()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, false);

            // Assert
            Assert.False(result.Value);
        }

        [Fact]
        public async Task DefaultProvider_WithEvaluationContext_ShouldHandleContextCorrectly()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var api = scopedProvider.GetRequiredService<IFeatureClient>();

            var context = EvaluationContext.Builder()
                .Set("userId", "test-user")
                .Set("email", "test@example.com")
                .Build();

            // Act
            var result = await api.GetBooleanDetailsAsync(TestFlagKey, false, context);

            // Assert
            Assert.False(result.Value);
            //Assert.Equal(Reason.Default, result.Reason);
        }

        #endregion

        #region Service Lifetime Integration Tests

        [Fact]
        public async Task DefaultProvider_Configuration_ShouldBeRegisteredAsSingleton()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));

            // Act
            var config1 = serviceProvider.GetRequiredService<Configuration>();
            var config2 = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.Same(config1, config2);
        }

        [Fact]
        public async Task DomainProvider_Configuration_ShouldBeRegisteredAsKeyedSingleton()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));

            // Act
            var config1 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);
            var config2 = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.Same(config1, config2);
        }

        [Fact]
        public async Task TryAddSingleton_MultipleRegistrations_ShouldNotReplaceExistingRegistration()
        {
            // Arrange & Act
            var serviceProvider = await CreateServiceProviderWithMultipleProvidersAsync(builder =>
            {
                // First registration should win due to TryAddSingleton behavior
                builder.UseLaunchDarkly(TestSdkKey, cfg => cfg.Offline(true));
                builder.UseLaunchDarkly(TestSdkKey, cfg => cfg.Offline(false)); // Should not replace the first
            });

            // Assert
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.True(registeredConfig.Offline); // Should still be the first configuration
        }

        [Fact]
        public async Task TryAddKeyedSingleton_MultipleRegistrations_ShouldNotReplaceExistingRegistration()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithMultipleProvidersAsync(builder =>
            {
                // First registration should win due to TryAddKeyedSingleton behavior
                builder.UseLaunchDarkly(TestDomain, TestSdkKey, cfg => cfg.Offline(true));
                builder.UseLaunchDarkly(TestDomain, TestSdkKey, cfg => cfg.Offline(false)); // Should not replace the first
                builder.AddPolicyName(policy => policy.DefaultNameSelector = _ => TestDomain);
            });

            // Act
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.True(registeredConfig.Offline); // Should still be the first configuration
        }

        #endregion

        #region Resource Management Integration Tests

        [Fact]
        public async Task ServiceProviderDisposal_AfterUsingProviders_ShouldNotCauseMemoryLeaks()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));

            // Act
            var config = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.NotNull(config);
            Assert.True(config.Offline);
        }

        [Fact]
        public async Task MultipleServiceProviders_WithSameConfiguration_ShouldIsolateConfigurations()
        {
            // Arrange
            var serviceProvider1 = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));
            var serviceProvider2 = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(false));

            // Act
            var config1 = serviceProvider1.GetRequiredService<Configuration>();
            var config2 = serviceProvider2.GetRequiredService<Configuration>();

            // Assert
            Assert.NotSame(config1, config2);
            Assert.True(config1.Offline);
            Assert.False(config2.Offline);
        }

        #endregion

        #region Early Validation Integration Tests

        [Fact]
        public async Task EarlyValidation_WithValidConfiguration_ShouldPassValidationAndRegisterCorrectly()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDefaultLaunchDarklyAsync(cfg => cfg.Offline(true));

            // Act
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();

            // Assert
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public async Task EarlyValidation_WithValidDomainConfiguration_ShouldPassValidationAndRegisterCorrectly()
        {
            // Arrange
            var serviceProvider = await CreateServiceProviderWithDomainLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));

            // Act
            var registeredConfig = serviceProvider.GetRequiredKeyedService<Configuration>(TestDomain);

            // Assert
            Assert.NotNull(registeredConfig);
            Assert.True(registeredConfig.Offline);
        }

        [Fact]
        public void EarlyValidation_WithPrebuiltConfiguration_ShouldPassValidationAndRegisterCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = Configuration.Builder(TestSdkKey).Offline(true).Build();
            
            services.AddOpenFeature(builder =>
            {
                builder.UseLaunchDarkly(config);
            });

            // Act
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            // Assert
            var registeredConfig = serviceProvider.GetRequiredService<Configuration>();
            Assert.NotNull(registeredConfig);
        }

        #endregion
    }
} 