using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LaunchDarkly.Logging;
using LaunchDarkly.Sdk;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Class which converts <see cref="EvaluationContext"/> objects into <see cref="Context"/> objects.
    /// </summary>
    internal class EvalContextConverter
    {
        private readonly Logger _log;

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
        private void Extract(string key, LdValue value, Func<string, ContextBuilder> setter)
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
        private void Extract(string key, LdValue value, Func<bool, ContextBuilder> setter)
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
        /// Extract a value and add it to a context builder.
        /// </summary>
        /// <param name="key">The key to add to the context if the value can be extracted</param>
        /// <param name="value">The value to extract</param>
        /// <param name="builder">The context builder to add the value to</param>
        private void ProcessValue(string key, Value value, ContextBuilder builder)
        {
            var ldValue = value.ToLdValue();

            switch (key)
            {
                // The targeting key/key will be handled outside this value conversion.
                case "targetingKey":
                case "key":
                    break;
                case "name":
                    Extract(key, ldValue, builder.Name);
                    break;
                case "anonymous":
                    Extract(key, ldValue, builder.Anonymous);
                    break;
                case "privateAttributes":
                    builder.Private(ldValue.AsList(LdValue.Convert.String).ToArray());
                    break;
                default:
                    // Was not a built-in attribute.
                    builder.Set(key, ldValue);
                    break;
            }
        }

        /// <summary>
        /// Convert an <see cref="EvaluationContext"/> into a <see cref="Context"/>.
        /// </summary>
        /// <param name="evaluationContext">The evaluation context to convert</param>
        /// <returns>A converted context</returns>
        public Context ToLdContext(EvaluationContext evaluationContext)
        {
            // Use the kind to determine the evaluation context shape.
            // If there is no kind at all, then we make a single context of "user" kind.
            evaluationContext.TryGetValue("kind", out var kind);

            var kindString = "user";
            // A multi-context.
            if (kind != null && kind.AsString == "multi")
            {
                return BuildMultiLdContext(evaluationContext);
            }
            // Single context with specified kind.
            else if (kind != null && kind.IsString)
            {
                kindString = kind.AsString;
            }
            // The kind was not a string.
            else if (kind != null && !kind.IsString)
            {
                _log.Warn("The EvaluationContext contained an invalid kind and it will be discarded.");
            }
            // Else, there is no kind, so we are going to assume a user.

            return BuildSingleLdContext(evaluationContext.AsDictionary(), kindString);
        }

        private Context BuildMultiLdContext(EvaluationContext evaluationContext)
        {
            var multiBuilder = Context.MultiBuilder();
            foreach (var pair in evaluationContext.AsDictionary())
            {
                // Don't need to inspect the "kind" key.
                if (pair.Key == "kind") continue;

                var kind = pair.Key;
                var attributes = pair.Value;

                if (!attributes.IsStructure)
                {
                    _log.Warn("Top level attributes in a multi-kind context should be Structure types.");
                    continue;
                }

                multiBuilder.Add(BuildSingleLdContext(attributes.AsStructure.AsDictionary(), kind));
            }


            return multiBuilder.Build();
        }

        private Context BuildSingleLdContext(IImmutableDictionary<string, Value> evaluationContext, string kindString)
        {
            // targetingKey is in the specification, so it takes precedence.
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

            var contextBuilder = Context.Builder(ContextKind.Of(kindString), finalKey);
            foreach (var kvp in evaluationContext)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                ProcessValue(key, value, contextBuilder);
            }

            return contextBuilder.Build();
        }
    }
}
