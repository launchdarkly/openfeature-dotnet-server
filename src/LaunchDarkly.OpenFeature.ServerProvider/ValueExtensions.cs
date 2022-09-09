using System.Globalization;
using System.Linq;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    internal static class ValueExtensions
    {
        /// <summary>
        /// Extract an OpenFeature <see cref="Value"/> into an <see cref="LdValue"/>.
        /// </summary>
        /// <param name="value">The value to extract</param>
        public static LdValue ToLdValue(this Value value)
        {
            if (value.IsNull())
            {
                return LdValue.Null;
            }
            if (value.IsBoolean())
            {
                var asBool = value.AsBoolean();
                if (asBool.HasValue)
                {
                    return LdValue.Of(asBool.Value);
                }
            }
            else if (value.IsNumber())
            {
                var asDouble = value.AsDouble();
                if (asDouble.HasValue)
                {
                    return LdValue.Of(asDouble.Value);
                }
            }
            else if (value.IsString())
            {
                return LdValue.Of(value.AsString());
            }
            else if (value.IsDateTime())
            {
                var asDateTime = value.AsDateTime();
                if (asDateTime.HasValue)
                {
                    return LdValue.Of(asDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ",
                        CultureInfo.InvariantCulture));
                }
            }
            else if (value.IsList())
            {
                var list = value.AsList();
                return LdValue.ArrayFrom(list.Select(ToLdValue));
            }
            else if (value.IsStructure())
            {
                var objectBuilder = LdValue.BuildObject();
                var structure = value.AsStructure();
                foreach (var kvp in structure)
                {
                    var val = ToLdValue(kvp.Value);
                    objectBuilder.Add(kvp.Key, val);
                }

                return objectBuilder.Build();
            }
            // Could not convert, should not happen.
            return LdValue.Null;
        }
    }
}
