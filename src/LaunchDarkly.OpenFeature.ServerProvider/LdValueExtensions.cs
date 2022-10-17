using System;
using System.Linq;
using LaunchDarkly.Sdk;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Extensions to <see cref="LdValue"/> which allow for conversions into OpenFeature types.
    /// </summary>
    internal static class LdValueExtensions
    {
        /// <summary>
        /// Extract an <see cref="LdValue"/> into an <see cref="Value"/>.
        /// </summary>
        /// <param name="value">The value to extract.</param>
        public static Value ToValue(this LdValue value)
        {
            switch (value.Type)
            {
                case LdValueType.Null:
                    return new Value();
                case LdValueType.Bool:
                    return new Value(value.AsBool);
                case LdValueType.Number:
                   return new Value(value.AsDouble);
                case LdValueType.String:
                    return new Value(value.AsString);
                case LdValueType.Array:
                    return new Value(value.List.Select(ToValue).ToList());
                case LdValueType.Object:
                    var structureBuilder = Structure.Builder();
                    foreach (var kvp in value.Dictionary)
                    {
                        structureBuilder.Set(kvp.Key, ToValue(kvp.Value));
                    }
                    return new Value(structureBuilder.Build());
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
