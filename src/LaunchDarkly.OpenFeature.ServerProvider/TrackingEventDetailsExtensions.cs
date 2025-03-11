using LaunchDarkly.Sdk;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    internal static class TrackingEventDetailsExtensions
    {
        /// <summary>
        /// Extract an OpenFeature <see cref="TrackingEventDetails"/> into an <see cref="LdValue"/>.
        /// </summary>
        /// <param name="trackingEventDetails">The value to extract</param>
        public static (double?, LdValue) ToLdValue(this TrackingEventDetails trackingEventDetails)
        {
            if (trackingEventDetails == null)
            {
                return (null, LdValue.Null);
            }

            var value = trackingEventDetails.Value;

            LdValue details;
            if (trackingEventDetails.Count == 0)
            {
                details = LdValue.Null;
            }
            else
            {
                var builder = LdValue.BuildObject();
                foreach (var keyvalue in trackingEventDetails)
                {
                    builder.Add(keyvalue.Key, keyvalue.Value.ToLdValue());
                }
                details = builder.Build();
            }

            return (value, details);
        }
    }
}
