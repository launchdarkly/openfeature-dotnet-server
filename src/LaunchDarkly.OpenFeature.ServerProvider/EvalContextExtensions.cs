using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
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
            var ldValue = value.ToLdValue();
            switch (key)
            {
                // The targeting key/key will be handled outside this value conversion.
                case "targetingKey":
                case "key":
                    break;
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

            // TODO: What should happen if the type was not correct?
        }

        /// <summary>
        /// Convert an <see cref="EvaluationContext"/> into a <see cref="User"/>.
        /// </summary>
        /// <param name="evaluationContext">The evaluation context to convert</param>
        /// <returns>A converted user</returns>
        public static User ToLdUser(this EvaluationContext evaluationContext)
        {
            // targetingKey is the specification, so it takes precedence.
            var keyAttr = evaluationContext.ContainsKey("key") ? evaluationContext.GetValue("key") : null;
            var targetingKey = evaluationContext.ContainsKey("targetingKey") ? evaluationContext.GetValue("targetingKey") : null;
            var finalKey = (targetingKey ?? keyAttr)?.AsString;

            var userBuilder = User.Builder(finalKey);
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
