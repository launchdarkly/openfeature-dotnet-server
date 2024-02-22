using System.Threading.Channels;
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

                if (!_eventChannel.Writer.TryWrite(payload))
                {
                    _logger.Warn("Provider was unable to write to the event channel for a change in provider status.");
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
