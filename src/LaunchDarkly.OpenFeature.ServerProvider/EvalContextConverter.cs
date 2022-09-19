using System;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Class which converts <see cref="EvaluationContext"/> objects into <see cref="User"/> objects.
    /// </summary>
    internal class EvalContextConverter
    {
        private readonly Logger _log;

        private delegate IUserBuilder UserBuilderStringSetter(string val);
        private delegate IUserBuilder UserBuilderBoolSetter(bool val);

        /// <summary>
        /// Construct a new instance of the converter.
        /// </summary>
        /// <param name="log">Used for logging any issues encountered during conversion</param>
        public EvalContextConverter(Logger log)
        {
            _log = log;
        }

        /// <summary>
        /// Generate a message indicating that an attribute was not of the correct type.
        /// </summary>
        /// <param name="attribute">The name of the attribute.</param>
        /// <param name="type">The type of the attribute.</param>
        /// <returns>A message indicating the attribute was of the incorrect type.</returns>
        private static string InvalidTypeMessage(string attribute, string type) => $"The attribute '{attribute}' " +
            $"must be of type {type}";

        /// <summary>
        /// Extract a string value and log an error if the value was not a string.
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value to extract</param>
        /// <param name="setter">
        /// A method to call with the extracted value.
        /// This will only be called if the type was correct.
        /// </param>
        private void Extract(string key, LdValue value, UserBuilderStringSetter setter)
        {
            if (value.IsNull)
            {
                // Ignore null values.
                return;
            }

            if (value.IsString)
            {
                setter(value.AsString);
                return;
            }

            _log.Error(InvalidTypeMessage(key, "string"));
        }

        /// <summary>
        /// Extract a bool value and log an error if the value was not a boolean.
        /// </summary>
        /// <param name="key">The key of the value</param>
        /// <param name="value">The value to extract</param>
        /// <param name="setter">
        /// A method to call with the extracted value.
        /// This will only be called if the type was correct.
        /// </param>
        private void Extract(string key, LdValue value, UserBuilderBoolSetter setter)
        {
            if (value.IsNull)
            {
                // Ignore null values.
                return;
            }

            if (value.Type == LdValueType.Bool)
            {
                setter(value.AsBool);
                return;
            }

            _log.Error(InvalidTypeMessage(key, "bool"));
        }

        /// <summary>
        /// Extract a value and add it to a user builder.
        /// </summary>
        /// <param name="key">The key to add to the user if the value can be extracted</param>
        /// <param name="value">The value to extract</param>
        /// <param name="builder">The user builder to add the value to</param>
        private void ProcessValue(string key, Value value, IUserBuilder builder)
        {
            var ldValue = value.ToLdValue();

            switch (key)
            {
                // The targeting key/key will be handled outside this value conversion.
                case "targetingKey":
                case "key":
                    break;
                case "secondary":
                    Extract(key, ldValue, builder.Secondary);
                    break;
                case "name":
                    Extract(key, ldValue, builder.Name);
                    break;
                case "firstName":
                    Extract(key, ldValue, builder.FirstName);
                    break;
                case "lastName":
                    Extract(key, ldValue, builder.LastName);
                    break;
                case "email":
                    Extract(key, ldValue, builder.Email);
                    break;
                case "avatar":
                    Extract(key, ldValue, builder.Avatar);
                    break;
                case "ip":
                    Extract(key, ldValue, builder.IPAddress);
                    break;
                case "country":
                    Extract(key, ldValue, builder.Country);
                    break;
                case "anonymous":
                    Extract(key, ldValue, builder.Anonymous);
                    break;
                default:
                    // Was not a built-in attribute.
                    builder.Custom(key, ldValue);
                    break;
            }
        }

        /// <summary>
        /// Convert an <see cref="EvaluationContext"/> into a <see cref="User"/>.
        /// </summary>
        /// <param name="evaluationContext">The evaluation context to convert</param>
        /// <returns>A converted user</returns>
        public User ToLdUser(EvaluationContext evaluationContext)
        {
            // targetingKey is the specification, so it takes precedence.
            evaluationContext.TryGetValue("key", out var keyAttr);
            evaluationContext.TryGetValue("targetingKey", out var targetingKey);
            var finalKey = (targetingKey ?? keyAttr)?.AsString;

            if (keyAttr != null && targetingKey != null)
            {
                _log.Warn("The EvaluationContext contained both a 'targetingKey' and a 'key' attribute. The 'key'" +
                          "attribute will be discarded.");
            }

            if (finalKey == null)
            {
                _log.Error("The EvaluationContext must contain either a 'targetingKey' or a 'key' and the type" +
                           "must be a string.");
            }

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
