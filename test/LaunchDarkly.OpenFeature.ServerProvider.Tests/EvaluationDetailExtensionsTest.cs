using System.Collections.Generic;
using LaunchDarkly.Sdk;
using OpenFeature.SDK.Constant;
using OpenFeature.SDK.Model;
using Xunit;

namespace LaunchDarkly.OpenFeature.ServerProvider.Tests
{
    public class EvaluationDetailExtensionsTest
    {
        [Fact]
        public void ItCanHandleSuccessfulEvaluations()
        {
            var evaluationDetail = new EvaluationDetail<bool>(true, 10, EvaluationReason.FallthroughReason);
            var resolutionDetail = evaluationDetail.ToResolutionDetails("test-flag");
            Assert.True(resolutionDetail.Value);
            Assert.Equal("10", resolutionDetail.Variant);
            Assert.Equal("FALLTHROUGH", resolutionDetail.Reason);
            Assert.Equal(ErrorType.None, resolutionDetail.ErrorType);
        }

        [Fact]
        public void ItCanHandleEachOfTheNonErrorEvaluationReasons()
        {
            Assert.Equal("OFF",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.OffReason)
                    .ToResolutionDetails("test-flag").Reason);

            Assert.Equal("FALLTHROUGH",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.FallthroughReason)
                    .ToResolutionDetails("test-flag").Reason);

            Assert.Equal("TARGET_MATCH",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.TargetMatchReason)
                    .ToResolutionDetails("test-flag").Reason);

            Assert.Equal("RULE_MATCH",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.RuleMatchReason(0, "8"))
                    .ToResolutionDetails("test-flag").Reason);

            Assert.Equal("PREREQUISITE_FAILED",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.PrerequisiteFailedReason("other-flag"))
                    .ToResolutionDetails("test-flag").Reason);
        }
        
        [Fact]
        public void ItCanHandleErrorTypes()
        {
            Assert.Equal("ERROR",
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.Exception))
                    .ToResolutionDetails("test-flag").Reason);
            
            Assert.Equal(ErrorType.General,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.Exception))
                    .ToResolutionDetails("test-flag").ErrorType);
            
            Assert.Equal(ErrorType.General,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.UserNotSpecified))
                    .ToResolutionDetails("test-flag").ErrorType);
            
            Assert.Equal(ErrorType.FlagNotFound,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.FlagNotFound))
                    .ToResolutionDetails("test-flag").ErrorType);
            
            Assert.Equal(ErrorType.ParseError,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.MalformedFlag))
                    .ToResolutionDetails("test-flag").ErrorType);
            
            Assert.Equal(ErrorType.TypeMismatch,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.WrongType))
                    .ToResolutionDetails("test-flag").ErrorType);
            
            Assert.Equal(ErrorType.ProviderNotReady,
                new EvaluationDetail<bool>(true, 10, EvaluationReason.ErrorReason(EvaluationErrorKind.ClientNotReady))
                    .ToResolutionDetails("test-flag").ErrorType);
        }

        [Fact]
        public void ItCanHandleDefaultValueDetail()
        {
            var detail = new EvaluationDetail<LdValue>(LdValue.ObjectFrom(new Dictionary<string, LdValue> { }), null,
                EvaluationReason.ErrorReason(EvaluationErrorKind.ClientNotReady));

            // We want to bypass the default conversion to we don't have to convert the Value
            // back and forth. So, for errors, it should use the provided default.
            var resolution = detail.ToValueDetail(new Value("TheDefault"));
            Assert.Equal("TheDefault", resolution.Value.AsString());
        }
        
        [Fact]
        public void ItHandleNonDefaultValueDetail()
        {
            var detail = new EvaluationDetail<LdValue>(LdValue.Of(true), 0,
                EvaluationReason.ErrorReason(EvaluationErrorKind.ClientNotReady));
            
            var resolution = detail.ToValueDetail(new Value(false));
            Assert.True(resolution.Value.AsBoolean());
        }
    }
}