using System.Threading.Channels;
using System.Threading.Tasks;
using LaunchDarkly.Logging;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    public sealed partial class Provider
    {
        private class StatusProvider
        {
            private ProviderStatus _providerStatus = ProviderStatus.NotReady;
            private object _statusLock = new object();
            private Channel<object> _eventChannel;
            private string _providerName;
            private Logger _logger;

            public StatusProvider(Channel<object> eventChannel, string providerName, Logger logger)
            {
                _eventChannel = eventChannel;
                _providerName = providerName;
                _logger = logger;
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
                catch
                {
                    _logger.Warn("Failed to send provider status event");
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
                        default:
                            EmitProviderEvent(ProviderEventTypes.ProviderError, message);
                            break;
                    }
                }
            }

            public ProviderStatus Status
            {
                get
                {
                    lock (_statusLock)
                    {
                        return _providerStatus;
                    }
                }
            }
        }
    }
}
