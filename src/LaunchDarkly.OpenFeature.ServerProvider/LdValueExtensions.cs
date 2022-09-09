using System;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;

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
        private static Value ExtractValue(LdValue value)
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
                    return new Value(value.List.Select(ExtractValue).ToList());
                case LdValueType.Object:
                    var ofStructure = new Structure();
                    foreach (var kvp in value.Dictionary)
                    {
                        ofStructure.Add(kvp.Key, ExtractValue(kvp.Value));
                    }
                    return new Value(ofStructure);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Convert an <see cref="LdValue"/> into a <see cref="Structure"/>.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>A converted structure, or an empty structure if it could not be converted.</returns>
        public static Structure ToStructure(this LdValue value)
        {
            var ofStructure = new Structure();
            foreach (var kvp in value.Dictionary)
            {
                var val = ExtractValue(kvp.Value);
                ofStructure.Add(kvp.Key, val);
            }

            return ofStructure;
        }
    }
}
