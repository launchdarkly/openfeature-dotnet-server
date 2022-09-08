using System;
using System.Collections.Generic;
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
        ///
        /// If a value cannot be extracted, then the accessor will not be called.
        /// </summary>
        /// <param name="value">The value to extract.</param>
        /// <param name="accessor">A method called if the value could be successfully extracted.</param>
        private static void ExtractValue(LdValue value, Action<Value> accessor)
        {
            switch (value.Type)
            {
                case LdValueType.Null:
                    accessor(new Value());
                    break;
                case LdValueType.Bool:
                    accessor(new Value(value.AsBool));
                    break;
                case LdValueType.Number:
                    accessor(new Value(value.AsDouble));
                    break;
                case LdValueType.String:
                    accessor(new Value(value.AsString));
                    break;
                case LdValueType.Array:
                    var ofList = new List<Value>();
                    foreach (var ldValue in value.List)
                    {
                        ExtractValue(ldValue, (ofValue) => { ofList.Add(ofValue); });
                    }

                    accessor(new Value(ofList));
                    break;
                case LdValueType.Object:
                    var ofStructure = new Structure();
                    foreach (var kvp in value.Dictionary)
                    {
                        ExtractValue(kvp.Value, (ofValue) => { ofStructure.Add(kvp.Key, ofValue); });
                    }

                    accessor(new Value(ofStructure));
                    break;
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
                ExtractValue(kvp.Value, (ofValue) => { ofStructure.Add(kvp.Key, ofValue); });
            }

            return ofStructure;
        }
    }
}