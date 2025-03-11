using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using Moq;
using OpenFeature.Model;
using Xunit;
using LaunchDarkly.Sdk.Server.Integrations;
using Xunit.Abstractions;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class ProviderTests
    {
        private readonly EvalContextConverter _converter =
            new EvalContextConverter(Components.NoLogging.Build(null).LogAdapter.Logger("test"));

        private ITestOutputHelper _outHelper;

        public ProviderTests(ITestOutputHelper outHelper)
        {
            _outHelper = outHelper;
        }

        [Fact(Timeout = 5000)]
        public void ItCanProvideMetaData()
        {
            var provider = new Provider(Configuration.Builder("").Offline(true).Build());

            Assert.Equal("LaunchDarkly.OpenFeature.ServerProvider", provider.GetMetadata().Name);
        }

        [Fact(Timeout = 5000)]
        public async Task ItHandlesValidInitializationWhenClientIsImmediatelyReady()
        {
            var provider = new Provider(Configuration.Builder("").Offline(true).Build());

            await provider.InitializeAsync(EvaluationContext.Builder().Set("key", "test").Build());
        }

        [Fact(Timeout = 5000)]
        public async Task ItHandlesMultipleCallsToInitialize()
        {
            var provider = new Provider(Configuration.Builder("").Offline(true).Build());

            await provider.InitializeAsync(EvaluationContext.Builder().Set("key", "test").Build());
            await provider.InitializeAsync(EvaluationContext.Builder().Set("key", "test").Build());
        }

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
                    new DataSourceStatus {State = DataSourceState.Valid});
            };
            completionTimer.Start();

            await provider.InitializeAsync(EvaluationContext.Empty);
        }

        [Fact(Timeout = 5000)]
        public async Task ItCanBeShutdown()
        {
            var provider = new Provider(Configuration.Builder("").Offline(true).Build());

            await provider.InitializeAsync(EvaluationContext.Builder().Set("key", "test").Build());

            await provider.ShutdownAsync();
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

            // Setup a timer to indicate that the client has initialized after some amount of time.
            var completionTimer = new Timer(100);
            completionTimer.AutoReset = false;
            completionTimer.Elapsed += (sender, args) =>
            {
                mockDataSourceStatus.Raise(e => e.StatusChanged += null,
                    mockDataSourceStatus.Object,
                    new DataSourceStatus {State = DataSourceState.Off});
            };
            completionTimer.Start();

            var exception =
                await Record.ExceptionAsync(async () => await provider.InitializeAsync(EvaluationContext.Empty));
            Assert.NotNull(exception);
            Assert.Equal("the provider has encountered a permanent error or been shutdown", exception.Message);
        }

        [Fact(Timeout = 5000)]
        public void ItCanBeConstructedWithLoggingConfiguration()
        {
            var logCapture = new LogCapture();
            var provider = new Provider(Configuration.Builder("").Offline(true)
                .Logging(Components.Logging().Adapter(logCapture)).Build());

            // This context is malformed and will cause a log.
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Set("key", "the-key")
                .Build();

            provider.ResolveBooleanValueAsync("the-flag", false, evaluationContext);
            Assert.True(logCapture.HasMessageWithText(LogLevel.Warn,
                "The EvaluationContext contained both a 'targetingKey' and a 'key' attribute. The 'key'" +
                " attribute will be discarded."));

            var exception = Record.Exception(() => logCapture.GetMessages()
                .Find(message => message.LoggerName == "LaunchDarkly.Sdk.OpenFeature.ServerProvider"));
            Assert.Null(exception);
        }

        [Fact(Timeout = 5000)]
        public void ItCanDoABooleanEvaluation()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.BoolVariationDetail("flag-key",
                    _converter.ToLdContext(evaluationContext), false))
                .Returns(new EvaluationDetail<bool>(true, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveBooleanValueAsync("flag-key", false, evaluationContext).Result;
            Assert.True(res.Value);
        }

        [Fact(Timeout = 5000)]
        public void ItCanDoAStringEvaluation()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.StringVariationDetail("flag-key",
                    _converter.ToLdContext(evaluationContext), "default"))
                .Returns(new EvaluationDetail<string>("notDefault", 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveStringValueAsync("flag-key", "default", evaluationContext).Result;
            Assert.Equal("notDefault", res.Value);
        }

        [Fact(Timeout = 5000)]
        public void ItCanDoAnIntegerEvaluation()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.IntVariationDetail("flag-key",
                    _converter.ToLdContext(evaluationContext), 0))
                .Returns(new EvaluationDetail<int>(1, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveIntegerValueAsync("flag-key", 0, evaluationContext).Result;
            Assert.Equal(1, res.Value);
        }

        [Fact(Timeout = 5000)]
        public void ItCanDoADoubleEvaluation()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.DoubleVariationDetail("flag-key",
                    _converter.ToLdContext(evaluationContext), 0))
                .Returns(new EvaluationDetail<double>(1.7, 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveDoubleValueAsync("flag-key", 0, evaluationContext).Result;
            Assert.Equal(1.7, res.Value);
        }

        [Fact(Timeout = 5000)]
        public void ItCanDoAValueEvaluation()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.JsonVariationDetail("flag-key",
                    It.IsAny<Context>(), It.IsAny<LdValue>()))
                .Returns(new EvaluationDetail<LdValue>(LdValue.Of("true"), 10, EvaluationReason.FallthroughReason));
            var provider = new Provider(mock.Object);

            var res = provider.ResolveStructureValueAsync("flag-key", new Value("false"), evaluationContext).Result;
            Assert.Equal("true", res.Value.AsString);
        }

        [Fact(Timeout = 5000)]
        public async Task ItEmitsConfigurationChangedEvents()
        {
            var testData = TestData.DataSource();
            var config = Configuration.Builder("")
                .DataSource(testData)
                .Events(Components.NoEvents)
                .Build();

            var provider = new Provider(config);
            await provider.InitializeAsync(EvaluationContext.Empty);

            testData.Update(testData.Flag("test-flag-a").BooleanFlag().On(true));
            testData.Update(testData.Flag("test-flag-b").BooleanFlag().On(true));

            // The ordering of the subsequent events is not going to be deterministic.
            var eventA = await provider.GetEventChannel().Reader.ReadAsync();
            var eventPayloadA = eventA as ProviderEventPayload;
            _outHelper.WriteLine($"Payload A change {eventPayloadA?.FlagsChanged[0]}");
            Assert.True("test-flag-a" == eventPayloadA?.FlagsChanged[0] || "test-flag-b" == eventPayloadA?.FlagsChanged[0]);

            Assert.Single(eventPayloadA?.FlagsChanged ?? new List<string>());

            var eventB = await provider.GetEventChannel().Reader.ReadAsync();
            var eventPayloadB = eventB as ProviderEventPayload;
            _outHelper.WriteLine($"Payload B change {eventPayloadB?.FlagsChanged[0]}");
            Assert.True("test-flag-a" == eventPayloadB?.FlagsChanged[0] || "test-flag-b" == eventPayloadB?.FlagsChanged[0]);
            Assert.Single(eventPayloadB?.FlagsChanged ?? new List<string>());
            Assert.NotEqual(eventPayloadA?.FlagsChanged[0], eventPayloadB?.FlagsChanged[0]);
        }

        [Fact(Timeout = 5000)]
        public void ItTracksCustomEvents()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.Track("event-key-123abc", _converter.ToLdContext(evaluationContext))).Verifiable();
            var provider = new Provider(mock.Object);

            provider.Track("event-key-123abc", evaluationContext);

            mock.Verify();
        }

        [Fact(Timeout = 5000)]
        public void ItTracksCustomEventsWithValue()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.Track("event-key-123abc", _converter.ToLdContext(evaluationContext), LdValue.Null, 99.77)).Verifiable();
            var provider = new Provider(mock.Object);

            provider.Track("event-key-123abc", evaluationContext, TrackingEventDetails.Builder().SetValue(99.77).Build());

            mock.Verify();
        }

        [Fact(Timeout = 5000)]
        public void ItTracksCustomEventsWithDetails()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.Track("event-key-123abc", _converter.ToLdContext(evaluationContext), LdValue.BuildObject().Set("color", "red").Build())).Verifiable();
            var provider = new Provider(mock.Object);

            provider.Track("event-key-123abc", evaluationContext, TrackingEventDetails.Builder().Set("color", "red").Build());

            mock.Verify();
        }

        [Fact(Timeout = 5000)]
        public void ItTracksCustomEventsWithDetailsAndValue()
        {
            var evaluationContext = EvaluationContext.Builder()
                .Set("targetingKey", "the-key")
                .Build();
            var mock = new Mock<ILdClient>();
            mock.Setup(l => l.GetLogger())
                .Returns(Components.NoLogging.Build(null).LogAdapter.Logger(null));
            mock.Setup(l => l.Track("event-key-123abc", _converter.ToLdContext(evaluationContext), LdValue.BuildObject().Set("currency", "USD").Build(), 99.77)).Verifiable();
            var provider = new Provider(mock.Object);

            provider.Track("event-key-123abc", evaluationContext, TrackingEventDetails.Builder().SetValue(99.77).Set("currency", "USD").Build());

            mock.Verify();
        }
    }
}
