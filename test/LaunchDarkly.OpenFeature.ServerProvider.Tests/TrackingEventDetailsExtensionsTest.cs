using LaunchDarkly.Sdk;
using OpenFeature.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class TrackingEventDetailsExtensionsTest
    {
        [Fact]
        public void ItCanHandleValuesAndDetails()
        {
            var trackingEventDetails = TrackingEventDetails.Builder().SetValue(99.77).Set("currency", "USD").Build();
            var (value, details) = trackingEventDetails.ToLdValue();
            Assert.Equal(LdValueType.Object, details.Type);
            Assert.Equal(LdValue.Of("USD"), details.Get("currency"));
            Assert.Equal(99.77, value);
        }

        [Fact]
        public void ItCanHandleDetailsOnly()
        {
            var trackingEventDetails = TrackingEventDetails.Builder().Set("color", "red").Build();
            var (value, details) = trackingEventDetails.ToLdValue();
            Assert.Equal(LdValueType.Object, details.Type);
            Assert.Equal(LdValue.Of("red"), details.Get("color"));
            Assert.Null(value);
        }

        [Fact]
        public void ItCanHandleValuesOnly()
        {
            var trackingEventDetails = TrackingEventDetails.Builder().SetValue(99.77).Build();
            var (value, details) = trackingEventDetails.ToLdValue();
            Assert.Equal(LdValueType.Null, details.Type);
            Assert.Equal(99.77, value);
        }

        [Fact]
        public void ItCanHandleEmptyStructures()
        {
            var trackingEventDetails = TrackingEventDetails.Empty;
            var (value, details) = trackingEventDetails.ToLdValue();
            Assert.Equal(LdValueType.Null, details.Type);
            Assert.Null(value);
        }

        [Fact]
        public void ItCanHandleNull()
        {
            TrackingEventDetails trackingEventDetails = null;
            var (value, details) = trackingEventDetails.ToLdValue();
            Assert.Equal(LdValueType.Null, details.Type);
            Assert.Null(value);
        }
    }
}
