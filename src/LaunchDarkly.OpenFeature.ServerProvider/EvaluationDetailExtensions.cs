using System;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Constant;
using OpenFeature.SDK.Model;

namespace LaunchDarkly.OpenFeature.ServerProvider
{
    /// <summary>
    /// Class containing extension methods used in the conversion of <see cref="EvaluationDetail{T}"/> into
    /// <see cref="ResolutionDetails{T}"/>.
    /// </summary>
    internal static class EvaluationDetailExtensions
    {
        public static ResolutionDetails<T> ToResolutionDetails<T>(this EvaluationDetail<T> detail, string flagKey)
        {
            if (detail.Reason.Kind != EvaluationReasonKind.Error)
            {
                return new ResolutionDetails<T>(flagKey, detail.Value, ErrorType.None,
                    reason: detail.Reason.Kind.ToString(), variant: detail.VariationIndex?.ToString());
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
                case EvaluationErrorKind.Exception:
                case null:
                    // All general errors.
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return new ResolutionDetails<T>(flagKey, detail.Value, errorType,
                reason: detail.Reason.Kind.ToString(), variant: detail.VariationIndex?.ToString());
        }

        /// <summary>
        /// Convert an <see cref="EvaluationDetail{LdValue}"/> into an <see cref="EvaluationDetail{Structure}"/>.
        /// 
        /// When handling an evaluation with a <see cref="Structure"/> type we need to convert the evaluation detail
        /// into a detail containing a <see cref="Structure"/>. Doing so allows for the full evaluation detail to be
        /// converted into a <see cref="ResolutionDetails{T}"/>.
        /// </summary>
        /// <param name="detail">The detail to converted</param>
        /// <param name="defaultValue">
        /// Used in place of the default value provided during evaluation.
        /// This avoids converting the value to a <see cref="LdValue"/> and then converting it back to a <see cref="Structure"/>.
        /// </param>
        /// <returns>The converted detail</returns>
        public static EvaluationDetail<Structure> ToStructDetail(this EvaluationDetail<LdValue> detail,
            Structure defaultValue)
        {
            return detail.IsDefaultValue
                ? new EvaluationDetail<Structure>(defaultValue, detail.VariationIndex, detail.Reason)
                : new EvaluationDetail<Structure>(detail.Value.ToStructure(), detail.VariationIndex, detail.Reason);
        }
    }
}