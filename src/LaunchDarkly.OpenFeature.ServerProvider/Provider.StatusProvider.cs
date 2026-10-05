using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    public sealed partial class Provider
    {
        private sealed class StatusProvider
        {
            private ProviderStatus _providerStatus = ProviderStatus.NotReady;
            private readonly object _statusLock = new object();
            private readonly Channel<object> _eventChannel;
            private readonly string _providerName;
            private readonly Logger _logger;
            private readonly Func<bool> _initializationCompleted;

            public StatusProvider(Channel<object> eventChannel, string providerName, Logger logger,
                Func<bool> initializationCompleted)
            {
                _eventChannel = eventChannel;
                _providerName = providerName;
                _logger = logger;
                _initializationCompleted = initializationCompleted;
            }

            private void EmitProviderEvent(ProviderEventTypes type, string message)
            {
                var payload = new ProviderEventPayload
                {
                    Type = type,
                    ProviderName = _providerName
                };
                if (message != null)
                {
                    payload.Message = message;
                }

                // Trigger the task do run, but don't wait for it. We wrap the exceptions inside SafeWrite,
                // so we aren't going to have unexpected exceptions here.
                Task.Run(() => SafeWrite(payload)).ConfigureAwait(false);
            }

            private async Task SafeWrite(ProviderEventPayload payload)
            {
                try
                {
                    await _eventChannel.Writer.WriteAsync(payload).ConfigureAwait(false);
                }
                catch(System.Exception e)
                {
                    _logger.Warn($"Failed to send provider status event: {e.Message}");
                }
            }

            public void SetStatus(ProviderStatus status, string message = null)
            {
                lock (_statusLock)
                {
                    if (status == _providerStatus)
                    {
                        return;
                    }

                    _providerStatus = status;
                    // The OpenFeature client will emit a ready or error event when initialization completes.
                    // The status change which completes initialization is therefore not emitted here, to avoid
                    // duplicating that event. Status changes after initialization has completed, including after it
                    // has failed, are emitted.
                    if (!_initializationCompleted())
                    {
                        return;
                    }
                    switch (status)
                    {
                        case ProviderStatus.NotReady:
                            break;
                        case ProviderStatus.Ready:
                            EmitProviderEvent(ProviderEventTypes.ProviderReady, message);
                            break;
                        case ProviderStatus.Stale:
                            EmitProviderEvent(ProviderEventTypes.ProviderStale, message);
                            break;
                        case ProviderStatus.Error:
                        case ProviderStatus.Fatal:
                        default:
                            EmitProviderEvent(ProviderEventTypes.ProviderError, message);
                            break;
                    }
                }
            }
        }
    }
}
