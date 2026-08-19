using System;
using System.Threading;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using Xunit;
using Xunit.Abstractions;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using Moq;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;
using Timer = System.Timers.Timer;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ClientIntegrationTests
    {
        private ITestOutputHelper _outHelper;

        public ClientIntegrationTests(ITestOutputHelper outHelper)
        {
            _outHelper = outHelper;
        }

        [Fact(Timeout = 5000)]
        public async Task ItHandlesValidInitializationWhenClientIsImmediatelyReady()
        {
            var provider = new Provider(Configuration.Builder("").Offline(true).Build());
            var readyCount = 0;
            Api.Instance.AddHandler(ProviderEventTypes.ProviderReady,
                details => { Interlocked.Increment(ref readyCount); });
            await Api.Instance.SetProviderAsync(provider);
            // Sleep for a moment and ensure there is only 1 ready event received.
            Thread.Sleep(100);
            Assert.Equal(1, readyCount);
        }

#if NET6_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public async Task ItHandlesValidInitializationWhenClientIsReadyAfterADelay()
        {
            var mockClient = new Mock<ILdClient>();
            mockClient.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));

            var mockDataSourceStatus = new Mock<IDataSourceStatusProvider>();
            mockDataSourceStatus.Setup(l => l.Status).Returns(new DataSourceStatus
            {
                State = DataSourceState.Initializing
            });
            mockClient.Setup(l => l.DataSourceStatusProvider).Returns(mockDataSourceStatus.Object);

            var mockFlagTracker = new Mock<IFlagTracker>();
            mockClient.Setup(l => l.FlagTracker).Returns(mockFlagTracker.Object);

            var provider = new Provider(mockClient.Object);

            // Setup a timer to indicate that the client has initialized after some amount of time.
            var completionTimer = new Timer(100);
            completionTimer.AutoReset = false;
            completionTimer.Elapsed += (sender, args) =>
            {
                mockDataSourceStatus.Raise(e => e.StatusChanged += null,
                    mockDataSourceStatus.Object,
                    new DataSourceStatus { State = DataSourceState.Valid });

                mockDataSourceStatus.Setup(l => l.Status).Returns(new DataSourceStatus
                {
                    State = DataSourceState.Valid
                });
            };
            completionTimer.Start();

            var readyCount = 0;
            Api.Instance.AddHandler(ProviderEventTypes.ProviderReady,
                details => { Interlocked.Increment(ref readyCount); });
            await Api.Instance.SetProviderAsync(provider);
            // Sleep for a moment and ensure there is only 1 ready event received.
            Thread.Sleep(100);
            Assert.Equal(1, readyCount);
        }

        [Fact(Timeout = 5000)]
        public async Task ItHandlesFailedInitialization()
        {
            var mockClient = new Mock<ILdClient>();
            mockClient.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));

            var mockDataSourceStatus = new Mock<IDataSourceStatusProvider>();
            mockDataSourceStatus.Setup(l => l.Status).Returns(new DataSourceStatus
            {
                State = DataSourceState.Initializing
            });
            mockClient.Setup(l => l.DataSourceStatusProvider).Returns(mockDataSourceStatus.Object);

            var mockFlagTracker = new Mock<IFlagTracker>();
            mockClient.Setup(l => l.FlagTracker).Returns(mockFlagTracker.Object);

            var provider = new Provider(mockClient.Object);

            // Setup a timer to indicate that the client has failed initialization after some amount of time.
            var completionTimer = new Timer(100);
            completionTimer.AutoReset = false;
            completionTimer.Elapsed += (sender, args) =>
            {
                mockDataSourceStatus.Raise(e => e.StatusChanged += null,
                    mockDataSourceStatus.Object,
                    new DataSourceStatus { State = DataSourceState.Off });
            };
            completionTimer.Start();

            var errorCount = 0;
            Api.Instance.AddHandler(ProviderEventTypes.ProviderError,
                details => { Interlocked.Increment(ref errorCount); });
            await Api.Instance.SetProviderAsync(provider);

            // Sleep for a moment and ensure there is only 1 error event received.
            Thread.Sleep(100);
            Assert.Equal(1, errorCount);
        }

        [Fact(Timeout = 5000)]
        public async Task ItCanEvaluateFlagsAfterTheDataSourceHasBeenShutdown()
        {
            var mockClient = new Mock<ILdClient>();
            mockClient.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mockClient.Setup(l => l.Initialized).Returns(true);
            mockClient.Setup(l => l.BoolVariationDetail("the-flag", It.IsAny<Sdk.Context>(), false))
                .Returns(new Sdk.EvaluationDetail<bool>(true, 10, Sdk.EvaluationReason.FallthroughReason));

            var mockDataSourceStatus = new Mock<IDataSourceStatusProvider>();
            mockDataSourceStatus.Setup(l => l.Status).Returns(new DataSourceStatus
            {
                State = DataSourceState.Valid
            });
            mockClient.Setup(l => l.DataSourceStatusProvider).Returns(mockDataSourceStatus.Object);

            var mockFlagTracker = new Mock<IFlagTracker>();
            mockClient.Setup(l => l.FlagTracker).Returns(mockFlagTracker.Object);

            var provider = new Provider(mockClient.Object);
            await Api.Instance.SetProviderAsync(provider);

            mockDataSourceStatus.Raise(e => e.StatusChanged += null,
                mockDataSourceStatus.Object,
                new DataSourceStatus { State = DataSourceState.Off });
            Thread.Sleep(100);

            var client = Api.Instance.GetClient();
            Assert.True(await client.GetBooleanValueAsync("the-flag", false,
                EvaluationContext.Builder().Set("targetingKey", "the-key").Build()));
        }

        [Fact(Timeout = 5000)]
        public async Task ItBecomesReadyAfterInitializationTimesOut()
        {
            var mockClient = new Mock<ILdClient>();
            mockClient.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));

            var mockDataSourceStatus = new Mock<IDataSourceStatusProvider>();
            mockDataSourceStatus.Setup(l => l.Status).Returns(new DataSourceStatus
            {
                State = DataSourceState.Initializing
            });
            mockClient.Setup(l => l.DataSourceStatusProvider).Returns(mockDataSourceStatus.Object);

            var mockFlagTracker = new Mock<IFlagTracker>();
            mockClient.Setup(l => l.FlagTracker).Returns(mockFlagTracker.Object);

            var provider = new Provider(mockClient.Object, TimeSpan.FromMilliseconds(50));

            await Api.Instance.SetProviderAsync(provider);

            // The handler is added after the failed initialization, otherwise it would be immediately invoked for
            // the state of any previously registered provider.
            var readyCount = 0;
            Api.Instance.AddHandler(ProviderEventTypes.ProviderReady,
                details => { Interlocked.Increment(ref readyCount); });

            mockDataSourceStatus.Raise(e => e.StatusChanged += null,
                mockDataSourceStatus.Object,
                new DataSourceStatus { State = DataSourceState.Valid });

            // The initialization timeout does not stop the client from connecting, so a later connection makes the
            // provider ready.
            Thread.Sleep(100);
            Assert.Equal(1, readyCount);
        }
#endif
    }
}
