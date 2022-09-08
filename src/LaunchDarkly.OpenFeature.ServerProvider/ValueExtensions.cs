using System;
using System.Globalization;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    internal static class ValueExtensions
    {
        /// <summary>
        /// Extract an OpenFeature <see cref="Value"/> into an <see cref="LdValue"/>.
        ///
        /// If a value cannot be extracted, then the accessor will not be called.
        /// </summary>
        /// <param name="value">The value to extract</param>
        /// <param name="accessor">A method called if the value could be successfully extracted</param>
        private static void ExtractValue(Value value, Action<LdValue> accessor)
        {
            if (value.IsNull())
            {
                accessor(LdValue.Null);
            }
            if (value.IsBoolean())
            {
                var asBool = value.AsBoolean();
                if (asBool.HasValue)
                {
                    accessor(LdValue.Of(asBool.Value));
                }
            }
            else if (value.IsNumber())
            {
                var asDouble = value.AsDouble();
                if (asDouble.HasValue)
                {
                    accessor(LdValue.Of(asDouble.Value));
                }
            }
            else if (value.IsString())
            {
                accessor(LdValue.Of(value.AsString()));
            }
            else if (value.IsDateTime())
            {
                var asDateTime = value.AsDateTime();
                if (asDateTime.HasValue)
                {
                    accessor(LdValue.Of(asDateTime.Value.ToString("yyyy-MM-ddTHH:mm:ssZ",
                        CultureInfo.InvariantCulture)));
                }
            }
            else if (value.IsList())
            {
                var arrayBuilder = LdValue.BuildArray();

                var list = value.AsList();
                foreach (var item in list)
                {
                    ExtractValue(item, (ldValue) => arrayBuilder.Add(ldValue));
                }

                accessor(arrayBuilder.Build());
            }
            else if (value.IsStructure())
            {
                var objectBuilder = LdValue.BuildObject();
                var structure = value.AsStructure();
                foreach (var kvp in structure)
                {
                    ExtractValue(kvp.Value, (ldValue) => objectBuilder.Add(kvp.Key, ldValue));
                }

                accessor(objectBuilder.Build());
            }
        }

        public static void Extract(this Value value, Action<LdValue> accessor)
        {
            ExtractValue(value, accessor);
        }
    }
}