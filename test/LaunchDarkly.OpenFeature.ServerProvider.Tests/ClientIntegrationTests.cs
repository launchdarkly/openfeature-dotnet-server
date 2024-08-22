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

        [Fact]
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

        [Fact]
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

        [Fact]
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
    }
}
