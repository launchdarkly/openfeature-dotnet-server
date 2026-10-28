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
            private string _statusMessage;
            private bool _startupWaitCanceled;
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

                if (type == ProviderEventTypes.ProviderError)
                {
                    payload.ErrorType = ErrorType.General;
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

            /// <summary>
            /// Handles a startup wait which was canceled instead of completing. The OpenFeature SDK reports the
            /// cancellation as the initialization outcome, so the current status is emitted as an event and later
            /// status changes are no longer suppressed as duplicates of the startup event.
            /// </summary>
            public void StartupWaitCanceled()
            {
                lock (_statusLock)
                {
                    _startupWaitCanceled = true;
                    if (_providerStatus != ProviderStatus.NotReady)
                    {
                        EmitStatusEvent(_providerStatus, _statusMessage);
                    }
                }
            }

            private void EmitStatusEvent(ProviderStatus status, string message)
            {
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

            public void SetStatus(ProviderStatus status, string message = null)
            {
                lock (_statusLock)
                {
                    if (status == _providerStatus)
                    {
                        return;
                    }

                    _providerStatus = status;
                    _statusMessage = message;
                    // The OpenFeature client will emit a ready or error event when initialization completes.
                    // The status change which completes initialization is therefore not emitted here, to avoid
                    // duplicating that event. Status changes after initialization has completed, including after it
                    // has failed, or after the startup wait was canceled, are emitted.
                    if (!_startupWaitCanceled && !_initializationCompleted())
                    {
                        return;
                    }

                    EmitStatusEvent(status, message);
                }
            }
        }
    }
}
