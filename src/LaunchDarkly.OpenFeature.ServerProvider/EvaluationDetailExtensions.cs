using System;
using LaunchDarkly.Sdk;
using OpenFeature.Constant;
using OpenFeature.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Class containing extension methods used in the conversion of <see cref="EvaluationDetail{T}"/> into
    /// <see cref="ResolutionDetails{T}"/>.
    /// </summary>
    internal static class EvaluationDetailExtensions
    {
        /// <summary>
        /// Convert an <see cref="EvaluationReasonKind"/> into a string identifier.
        /// This string identifier is the same as we would use in a JSON representation.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>The value as a string</returns>
        /// <exception cref="ArgumentException">Thrown if the kind is unsupported.</exception>
        private static string ToIdentifier(this EvaluationReasonKind value)
        {
            switch (value)
            {
                case EvaluationReasonKind.Off:
                    return "OFF";
                case EvaluationReasonKind.Fallthrough:
                    return "FALLTHROUGH";
                case EvaluationReasonKind.TargetMatch:
                    return "TARGET_MATCH";
                case EvaluationReasonKind.RuleMatch:
                    return "RULE_MATCH";
                case EvaluationReasonKind.PrerequisiteFailed:
                    return "PREREQUISITE_FAILED";
                case EvaluationReasonKind.Error:
                    return "ERROR";
                default:
                    throw new ArgumentException();
            }
        }
        
        /// <summary>
        /// Convert an <see cref="EvaluationDetail{T}"/> to a <see cref="ResolutionDetails{T}"/>.
        /// </summary>
        /// <param name="detail">The detail to convert</param>
        /// <param name="flagKey">The flag key that was evaluated</param>
        /// <typeparam name="T">The type of the flag evaluation</typeparam>
        /// <returns>The <see cref="EvaluationDetail{T}"/></returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the kind is not valid</exception>
        public static ResolutionDetails<T> ToResolutionDetails<T>(this EvaluationDetail<T> detail, string flagKey)
        {
            if (detail.Reason.Kind != EvaluationReasonKind.Error)
            {
                return new ResolutionDetails<T>(flagKey, detail.Value, ErrorType.None,
                    reason: detail.Reason.Kind.ToIdentifier(), variant: detail.VariationIndex?.ToString());
            }

            var errorType = ErrorType.General;
            switch (detail.Reason.ErrorKind)
            {
                case EvaluationErrorKind.ClientNotReady:
                    errorType = ErrorType.ProviderNotReady;
                    break;
                case EvaluationErrorKind.WrongType:
                    errorType = ErrorType.TypeMismatch;
                    break;
                case EvaluationErrorKind.MalformedFlag:
                    errorType = ErrorType.ParseError;
                    break;
                case EvaluationErrorKind.FlagNotFound:
                    errorType = ErrorType.FlagNotFound;
                    break;
                case EvaluationErrorKind.UserNotSpecified:
                    errorType = ErrorType.TargetingKeyMissing;
                    break;
                case EvaluationErrorKind.Exception:
                case null:
                    // All general errors.
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ResolutionDetails<T>(flagKey, detail.Value, errorType,
                reason: detail.Reason.Kind.ToIdentifier(), variant: detail.VariationIndex?.ToString());
        }

        /// <summary>
        /// Convert an <see cref="EvaluationDetail{LdValue}"/> into an <see cref="EvaluationDetail{Value}"/>.
        ///
        /// When handling an evaluation with a <see cref="Value"/> type we need to convert the evaluation detail
        /// into a detail containing a <see cref="Value"/>. Doing so allows for the full evaluation detail to be
        /// converted into a <see cref="ResolutionDetails{T}"/>.
        /// </summary>
        /// <param name="detail">The detail to converted</param>
        /// <param name="defaultValue">
        /// Used in place of the default value provided during evaluation.
        /// This avoids converting the value to a <see cref="LdValue"/> and then converting it back to a <see cref="Value"/>.
        /// </param>
        /// <returns>The converted detail</returns>
        public static EvaluationDetail<Value> ToValueDetail(this EvaluationDetail<LdValue> detail,
            Value defaultValue)
        {
            return detail.IsDefaultValue
                ? new EvaluationDetail<Value>(defaultValue, detail.VariationIndex, detail.Reason)
                : new EvaluationDetail<Value>(detail.Value.ToValue(), detail.VariationIndex, detail.Reason);
        }
    }
}
