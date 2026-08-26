using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace LaunchDarkly.OpenFeature.ServerProvider.Hosting
{
    /// <summary>
    /// Caches a single provider instance per domain so that resolving the provider more than once does not
    /// create additional LaunchDarkly clients.
    /// </summary>
    internal sealed class ProviderRegistry
    {
        internal const string DefaultDomain = "";

        private readonly IOptionsMonitor<LaunchDarklyProviderOptions> _options;
        private readonly Dictionary<string, Provider> _providers = new Dictionary<string, Provider>();
        private readonly object _lock = new object();

        public ProviderRegistry(IOptionsMonitor<LaunchDarklyProviderOptions> options)
        {
            _options = options;
        }

        public Provider Get(string domain)
        {
            var key = domain ?? DefaultDomain;
            lock (_lock)
            {
                if (_providers.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var created = new Provider(_options.Get(key).BuildConfiguration());
                _providers[key] = created;
                return created;
            }
        }
    }
}
