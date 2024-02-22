﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Internal.Concurrent;
using LaunchDarkly.Sdk.Server;
using LaunchDarkly.Sdk.Server.Interfaces;
using OpenFeature;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// An OpenFeature <see cref="FeatureProvider"/> which enables the use of the LaunchDarkly Server-Side SDK for .NET
    /// with OpenFeature.
    /// </summary>
    /// <example>
    ///     var provider = new Provider(Configuration.Builder("my-sdk-key").Build());
    ///
    ///     OpenFeature.Api.Instance.SetProvider(provider);
    ///
    ///     var client = OpenFeature.Api.Instance.GetClient();
    /// </example>
    public sealed partial class Provider : FeatureProvider
    {
        private const string NameSpace = "OpenFeature.ServerProvider";
        private readonly Metadata _metadata = new Metadata($"LaunchDarkly.{NameSpace}");
        private readonly ILdClient _client;
        private readonly EvalContextConverter _contextConverter;
        private readonly StatusProvider _statusProvider;

        private readonly AtomicBoolean _initializeCalled = new AtomicBoolean(false);

        // There is no support for void task completion, so we use bool as a dummy result type.
        private readonly TaskCompletionSource<bool> _initCompletion = new TaskCompletionSource<bool>();
        private readonly Logger _logger;

        private const string ProviderShutdownMessage =
            "the provider has encountered a permanent error or been shutdown";

        internal Provider(ILdClient client)
        {
            _client = client;
            _logger = _client.GetLogger().SubLogger(NameSpace);
            _statusProvider = new StatusProvider(EventChannel, _metadata.Name, _logger);
            _contextConverter = new EvalContextConverter(_logger);
        }

        /// <summary>
        ///  Construct a new instance of the provider with the given configuration.
        /// </summary>
        /// <param name="config">A client configuration object</param>
        public Provider(Configuration config) : this(new LdClient(config))
        {
        }

        /// <summary>
        ///  Construct a new instance of the provider with the given SDK key.
        /// </summary>
        /// <param name="sdkKey">The SDK key</param>
        public Provider(string sdkKey) : this(new LdClient(sdkKey))
        {
        }

        /// <summary>
        /// <para>
        /// Get the underlying client instance.
        /// </para>
        /// <para>
        /// If the initialization/shutdown features of OpenFeature are being used, then the returned client instance
        /// may be shutdown if the provider has been removed from the OpenFeature API instance.
        /// </para>
        /// <para>
        /// This client instance can be used to take advantage of features which are not part of OpenFeature.
        /// For instance using migration flags.
        /// </para>
        /// </summary>
        /// <returns>The LaunchDarkly client instance</returns>
        public ILdClient GetClient()
        {
            return _client;
        }

        #region FeatureProvider Implementation

        /// <inheritdoc />
        public override Metadata GetMetadata() => _metadata;

        /// <inheritdoc />
        public override Task<ResolutionDetails<bool>> ResolveBooleanValue(string flagKey, bool defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .BoolVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<string>> ResolveStringValue(string flagKey, string defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .StringVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<int>> ResolveIntegerValue(string flagKey, int defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .IntVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<double>> ResolveDoubleValue(string flagKey, double defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .DoubleVariationDetail(flagKey, _contextConverter.ToLdContext(context), defaultValue)
            .ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override Task<ResolutionDetails<Value>> ResolveStructureValue(string flagKey, Value defaultValue,
            EvaluationContext context = null) => Task.FromResult(_client
            .JsonVariationDetail(flagKey, _contextConverter.ToLdContext(context), LdValue.Null)
            .ToValueDetail(defaultValue).ToResolutionDetails(flagKey));

        /// <inheritdoc />
        public override ProviderStatus GetStatus()
        {
            return _statusProvider.Status;
        }

        /// <inheritdoc />
        public override Task Initialize(EvaluationContext context)
        {
            if (_initializeCalled.GetAndSet(true))
            {
                return _initCompletion.Task;
            }

            _client.FlagTracker.FlagChanged += FlagChangeHandler;

            _client.DataSourceStatusProvider.StatusChanged += StatusChangeHandler;

            // We start listening for status changes and then we check the current status change. If we do not check
            // then we could have missed a status change. If we check before registering a listener, then we could
            // miss a change between checking and listening. Doing it this way we can get duplicates, but we filter
            // when the status does not actually change, so we won't emit duplicate events.
            if (_client.Initialized)
            {
                _statusProvider.SetStatus(ProviderStatus.Ready);
                _initCompletion.TrySetResult(true);
            }

            if (_client.DataSourceStatusProvider.Status.State == DataSourceState.Off)
            {
                _statusProvider.SetStatus(ProviderStatus.Error, ProviderShutdownMessage);
                _initCompletion.TrySetException(new LaunchDarklyProviderInitException(ProviderShutdownMessage));
            }

            return _initCompletion.Task;
        }

        /// <inheritdoc />
        public override Task Shutdown()
        {
            _client.DataSourceStatusProvider.StatusChanged -= StatusChangeHandler;
            _client.FlagTracker.FlagChanged -= FlagChangeHandler;
            (_client as IDisposable)?.Dispose();
            _statusProvider.SetStatus(ProviderStatus.NotReady);
            return Task.CompletedTask;
        }

        #endregion

        private void FlagChangeHandler(object sender, FlagChangeEvent changeEvent)
        {
            Task.Run(() => SafeWriteChangeEvent(changeEvent)).ConfigureAwait(false);
        }

        private async Task SafeWriteChangeEvent(FlagChangeEvent changeEvent)
        {
            try
            {
                await EventChannel.Writer.WriteAsync(new ProviderEventPayload
                {
                    ProviderName = _metadata.Name,
                    Type = ProviderEventTypes.ProviderConfigurationChanged,
                    FlagsChanged = new List<string>{changeEvent.Key},
                }).ConfigureAwait(false);
            }
            catch(Exception e)
            {
                _logger.Warn($"Encountered an error sending configuration changed events: {e.Message}");
            }
        }

        private void StatusChangeHandler(object sender, DataSourceStatus status)
        {
            switch (status.State)
            {
                case DataSourceState.Initializing:
                    break;
                case DataSourceState.Valid:
                    _statusProvider.SetStatus(ProviderStatus.Ready);
                    _initCompletion.TrySetResult(true);
                    break;
                case DataSourceState.Interrupted:
                    _statusProvider.SetStatus(ProviderStatus.Error,
                        status.LastError?.Message ?? "encountered an unknown error");
                    break;
                case DataSourceState.Off:
                default:
                    _statusProvider.SetStatus(ProviderStatus.Error, ProviderShutdownMessage);
                    _initCompletion.TrySetException(new LaunchDarklyProviderInitException(ProviderShutdownMessage));
                    break;
            }
        }
    }
}
