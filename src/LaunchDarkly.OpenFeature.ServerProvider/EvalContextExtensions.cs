using System;
using System.Collections.Generic;
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
                    ExtractValue(value, (ldValue) => objectBuilder.Add(kvp.Key, ldValue));
                }

                accessor(objectBuilder.Build());
            }
        }

        public static void Extract(this Value value, Action<LdValue> accessor)
        {
            ExtractValue(value, accessor);
        }
    }
    
    /// <summary>
    /// Extensions to <see cref="EvaluationContext"/> which allow for conversion to LaunchDarkly types.
    /// </summary>
    internal static class EvalContextExtensions
    {
        /// <summary>
        /// Extract a value and add it to a user builder.
        /// </summary>
        /// <param name="key">The key to add to the user if the value can be extracted</param>
        /// <param name="value">The value to extract</param>
        /// <param name="builder">The user builder to add the value to</param>
        private static void ProcessValue(string key, Value value, IUserBuilder builder)
        {
            value.Extract((ldValue) =>
            {
                switch (key)
                {
                    case "secondary":
                        if (ldValue.IsString)
                        {
                            builder.Secondary(ldValue.AsString);
                        }

                        break;
                    case "name":
                        if (ldValue.IsString)
                        {
                            builder.Name(ldValue.AsString);
                        }

                        break;
                    case "firstName":
                        if (ldValue.IsString)
                        {
                            builder.FirstName(ldValue.AsString);
                        }

                        break;
                    case "lastName":
                        if (ldValue.IsString)
                        {
                            builder.LastName(ldValue.AsString);
                        }

                        break;
                    case "email":
                        if (ldValue.IsString)
                        {
                            builder.Email(ldValue.AsString);
                        }

                        break;
                    case "avatar":
                        if (ldValue.IsString)
                        {
                            builder.Avatar(ldValue.AsString);
                        }

                        break;
                    case "ip":
                        if (ldValue.IsString)
                        {
                            builder.IPAddress(ldValue.AsString);
                        }

                        break;
                    case "country":
                        if (ldValue.IsString)
                        {
                            builder.Country(ldValue.AsString);
                        }

                        break;
                    case "anonymous":
                        if (ldValue.Type == LdValueType.Bool)
                        {
                            builder.Anonymous(ldValue.AsBool);
                        }

                        break;
                    default:
                        // Was not a built-in attribute.
                        builder.Custom(key, ldValue);
                        break;
                }
            });
            // TODO: What should happen if the type was not correct?
        }

        /// <summary>
        /// Convert an <see cref="EvaluationContext"/> into a <see cref="User"/>.
        /// </summary>
        /// <param name="evaluationContext">The evaluation context to convert</param>
        /// <returns>A converted user</returns>
        public static User ToLdUser(this EvaluationContext evaluationContext)
        {
            var userBuilder = User.Builder(evaluationContext.GetValue("targetingKey").AsString());
            foreach (var kvp in evaluationContext)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                ProcessValue(key, value, userBuilder);
            }

            return userBuilder.Build();
        }
    }
}
