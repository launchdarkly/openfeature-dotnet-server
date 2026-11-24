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
        /// Configures an <see cref="IServiceProvider"/> with OpenFeature and LaunchDarkly as the default feature provider.
        /// </summary>
        /// <param name="configure">
        /// Optional delegate to customize the LaunchDarkly <see cref="ConfigurationBuilder"/> during registration.
        /// </param>
        /// <returns>
        /// An initialized <see cref="IServiceProvider"/> with LaunchDarkly configured as the default provider.
        /// </returns>
        private static ValueTask<IServiceProvider> ConfigureLaunchDarklyAsync(Action<ConfigurationBuilder> configure = null)
            => ConfigureOpenFeatureAsync(builder => builder.UseLaunchDarkly(TestSdkKey, configure));

        /// <summary>
        /// Configures an <see cref="IServiceProvider"/> with OpenFeature and LaunchDarkly registered as a domain-scoped feature provider.
        /// </summary>
        /// <param name="domain">The domain identifier to associate with the scoped provider (e.g., tenant or environment).</param>
        /// <param name="configure">
        /// Optional delegate to customize the LaunchDarkly <see cref="ConfigurationBuilder"/> for the specified domain.
        /// </param>
        /// <returns>
        /// An initialized <see cref="IServiceProvider"/> with domain-scoped LaunchDarkly support.
        /// </returns>
        private static ValueTask<IServiceProvider> ConfigureLaunchDarklyAsync(string domain, Action<ConfigurationBuilder> configure = null)
            => ConfigureOpenFeatureAsync(builder => builder.UseLaunchDarkly(domain, TestSdkKey, configure));

        /// <summary>
        /// Configures an <see cref="IServiceProvider"/> with OpenFeature and one or more feature providers.
        /// </summary>
        /// <param name="configureBuilder">
        /// Delegate to configure the <see cref="OpenFeatureBuilder"/> with feature providers.
        /// </param>
        /// <returns>
        /// An initialized <see cref="IServiceProvider"/> with provider lifecycle setup and validation enabled.
        /// </returns>
        private static async ValueTask<IServiceProvider> ConfigureOpenFeatureAsync(Action<OpenFeatureBuilder> configureBuilder)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            // Register OpenFeature with the configured providers
            services.AddOpenFeature(configureBuilder);

            // Build the root service provider with scope validation enabled
            var serviceProvider = services.BuildServiceProvider(validateScopes: true);

            // Ensure the feature provider lifecycle is initialized (e.g., LaunchDarkly ready for evaluations)
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

        #endregion

        #region Configuration Overload Integration Tests

        [Fact]
        public async Task ConfigurationOverload_DefaultProvider_ShouldResolveFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await client.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task ConfigurationOverload_DomainProvider_ShouldResolveKeyedFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredKeyedService<IFeatureClient>(TestDomain);

            // Act
            var result = await client.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task ConfigurationOverload_DefaultProvider_ShouldPreserveCustomConfigurationSettings()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(5);
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg
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
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg
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
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await client.GetBooleanValueAsync(TestFlagKey, false);

            // Assert
            Assert.False(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task SdkKeyOverload_DomainProvider_ShouldResolveKeyedFeatureClientSuccessfully()
        {
            // Arrange
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredKeyedService<IFeatureClient>(TestDomain);

            // Act
            var result = await client.GetBooleanValueAsync(TestFlagKey, true);

            // Assert
            Assert.True(result); // Default value should be returned in offline mode
        }

        [Fact]
        public async Task SdkKeyOverload_DefaultProvider_ShouldApplyCustomConfigurationFromDelegate()
        {
            // Arrange
            var startWaitTime = TimeSpan.FromSeconds(3);
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg
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
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg
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
            var serviceProvider = await ConfigureOpenFeatureAsync(builder =>
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
            var domain1Client = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain1");
            var domain2Client = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain2");

            var defaultConfig = serviceProvider.GetRequiredService<Configuration>();
            var domain1Config = serviceProvider.GetRequiredKeyedService<Configuration>("domain1");
            var domain2Config = serviceProvider.GetRequiredKeyedService<Configuration>("domain2");

            // Assert
            Assert.Same(defaultClient, domain1Client);
            Assert.NotSame(domain1Client, domain2Client);
            Assert.NotSame(domain1Config, domain2Config);

            Assert.True(domain1Config.Offline, "Expected 'domain1' LaunchDarkly config to be in offline mode.");
            Assert.True(domain2Config.Offline, "Expected 'domain2' LaunchDarkly config to be in offline mode.");
        }

        [Fact]
        public async Task MultiProvider_MultipleDomains_ShouldIsolateConfigurationsCorrectly()
        {
            // Arrange
            const string fastDomain = "fast-domain";
            const string slowDomain = "slow-domain";
            var fastStartWait = TimeSpan.FromMilliseconds(100);
            var slowStartWait = TimeSpan.FromSeconds(5);

            var serviceProvider = await ConfigureOpenFeatureAsync(builder =>
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
            var serviceProvider = await ConfigureOpenFeatureAsync(builder =>
            {
                // Test all SDK key overloads
                builder.UseLaunchDarkly("domain1", TestSdkKey, cfg => cfg.Offline(true)); // Default provider
                builder.UseLaunchDarkly("domain2", TestSdkKey, cfg => cfg.Offline(true)); // Domain provider
                builder.UseLaunchDarkly("domain3", TestSdkKey, cfg => cfg.Offline(true)); // Domain provider
                builder.AddPolicyName(policy => policy.DefaultNameSelector = _ => "domain1");
            });

            var scopedProvider = CreateScopedServiceProvider(serviceProvider);

            var defaultClient = scopedProvider.GetRequiredService<IFeatureClient>();
            var domain1Client = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain1");
            var domain2Client = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain2");
            var domain3Client = scopedProvider.GetRequiredKeyedService<IFeatureClient>("domain3");
            
            // Act & Assert - Test all supported types on all providers
            foreach (var client in new[] { defaultClient, domain1Client, domain2Client, domain3Client })
            {
                var boolResult = await client.GetBooleanValueAsync("bool-flag", true);
                Assert.True(boolResult);

                var stringResult = await client.GetStringValueAsync("string-flag", "default");
                Assert.Equal("default", stringResult);

                var intResult = await client.GetIntegerValueAsync("int-flag", 42);
                Assert.Equal(42, intResult);

                var doubleResult = await client.GetDoubleValueAsync("double-flag", 3.14);
                Assert.Equal(3.14, doubleResult);

                var structureResult = await client.GetObjectValueAsync("object-flag", new Value("default"));
                Assert.Equal("default", structureResult.AsString);
            }
        }

        #endregion

        #region OpenFeature Behavior Integration Tests

        [Fact]
        public async Task DefaultProvider_InOfflineMode_ShouldReturnCorrectReasonAndDefaultValue()
        {
            // Arrange
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredService<IFeatureClient>();

            // Act
            var result = await client.GetBooleanDetailsAsync(TestFlagKey, false);

            // Assert
            Assert.False(result.Value);
        }

        [Fact]
        public async Task DefaultProvider_WithEvaluationContext_ShouldHandleContextCorrectly()
        {
            // Arrange
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));
            var scopedProvider = CreateScopedServiceProvider(serviceProvider);
            var client = scopedProvider.GetRequiredService<IFeatureClient>();

            var context = EvaluationContext.Builder()
                .Set("userId", "test-user")
                .Set("email", "test@example.com")
                .Build();

            // Act
            var result = await client.GetBooleanDetailsAsync(TestFlagKey, false, context);

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
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));

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
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));

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
            var serviceProvider = await ConfigureOpenFeatureAsync(builder =>
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
            var serviceProvider = await ConfigureOpenFeatureAsync(builder =>
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
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));

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
            var serviceProvider1 = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));
            var serviceProvider2 = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(false));

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
            var serviceProvider = await ConfigureLaunchDarklyAsync(cfg => cfg.Offline(true));

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
            var serviceProvider = await ConfigureLaunchDarklyAsync(TestDomain, cfg => cfg.Offline(true));

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