using ExtendedChromaKey.Localization;
using ExtendedChromaKey.Models;
using System.Collections.Immutable;
using YukkuriMovieMaker.Commons;

namespace ExtendedChromaKey.Services
{
    internal sealed class ValidationRuleProvider
    {
        private const double ToleranceLowThreshold = 1.0;
        private const double ToleranceHighThreshold = 85.0;
        private const double LuminanceMixHighThreshold = 90.0;
        private const double ClipMidThreshold = 50.0;
        private const double EdgeSoftnessMidThreshold = 50.0;
        private const double GradientStrengthHighThreshold = 90.0;
        private const double EdgeBlurComplexThreshold = 20.0;
        private const double DespotComplexThreshold = 5.0;
        private const double DefaultGradientAngle = 90.0;
        private const int RuleCount = 21;

        private static readonly Lazy<ImmutableArray<ValidationRule>> _rules =
            new(BuildRules, LazyThreadSafetyMode.ExecutionAndPublication);

        public ImmutableArray<ValidationRule> Rules => _rules.Value;

        private static ImmutableArray<ValidationRule> BuildRules()
        {
            var builder = ImmutableArray.CreateBuilder<ValidationRule>(RuleCount);

            builder.Add(new ValidationRule(
                "Error_BaseColorTransparency",
                static e => e.MainKeyColor == KeyColorType.Custom && e.BaseColor.A == 0,
                Texts.ValidationRule_Error_BaseColorTransparency,
                ValidationLevel.Error));

            builder.Add(new ValidationRule(
                "Error_ClipBlackWhiteInverted",
                static e => GetValue(e.ClipBlack) >= GetValue(e.ClipWhite),
                Texts.ValidationRule_Error_ClipBlackWhiteInverted,
                ValidationLevel.Error));

            builder.Add(new ValidationRule(
                "Warning_ToleranceExtremelyLow",
                static e => GetValue(e.Tolerance) < ToleranceLowThreshold,
                Texts.ValidationRule_Warning_ToleranceExtremelyLow,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_ToleranceExtremelyHigh",
                static e => GetValue(e.Tolerance) > ToleranceHighThreshold,
                Texts.ValidationRule_Warning_ToleranceExtremelyHigh,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_SoftnessWithoutTolerance",
                static e => GetValue(e.EdgeSoftness) > 0 && GetValue(e.Tolerance) < ToleranceLowThreshold,
                Texts.ValidationRule_Warning_SoftnessWithoutTolerance,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_HighLuminanceMix",
                static e => GetValue(e.LuminanceMix) > LuminanceMixHighThreshold,
                Texts.ValidationRule_Warning_HighLuminanceMix,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_ClipAndSoftnessConflict",
                static e => (GetValue(e.ClipBlack) > ClipMidThreshold || GetValue(e.ClipWhite) < ClipMidThreshold)
                         && GetValue(e.EdgeSoftness) > EdgeSoftnessMidThreshold,
                Texts.ValidationRule_Warning_ClipAndSoftnessConflict,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Info_GradientStrengthZero",
                static e => GetValue(e.GradientStrength) == 0
                         && (e.EndColor != e.BaseColor || GetValue(e.GradientAngle) != DefaultGradientAngle),
                Texts.ValidationRule_Info_GradientStrengthZero,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_SpillSuppressionWithCustom",
                static e => e.MainKeyColor == KeyColorType.Custom && GetValue(e.SpillSuppression) > 0,
                Texts.ValidationRule_Info_SpillSuppressionWithCustom,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ReplaceIntensityZero",
                static e => GetValue(e.ReplaceIntensity) == 0
                         && (e.ReplaceColor.A != 0 || GetValue(e.PreserveLuminance) > 0),
                Texts.ValidationRule_Info_ReplaceIntensityZero,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ReplaceColorTransparent",
                static e => GetValue(e.ReplaceIntensity) > 0 && e.ReplaceColor.A == 0,
                Texts.ValidationRule_Info_ReplaceColorTransparent,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ColorSpaceForCustomOnly",
                static e => e.MainKeyColor != KeyColorType.Custom && e.ColorSpace != AdvancedColorSpace.Lab,
                Texts.ValidationRule_Info_ColorSpaceForCustomOnly,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_HueRangeForSpecificSpaces",
                static e => GetValue(e.HueRange) > 0
                         && e.MainKeyColor == KeyColorType.Custom
                         && e.ColorSpace != AdvancedColorSpace.HSV
                         && e.ColorSpace != AdvancedColorSpace.LCH,
                Texts.ValidationRule_Info_HueRangeForSpecificSpaces,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_SaturationThresholdForHsvOnly",
                static e => GetValue(e.SaturationThreshold) > 0
                         && e.MainKeyColor == KeyColorType.Custom
                         && e.ColorSpace != AdvancedColorSpace.HSV,
                Texts.ValidationRule_Info_SaturationThresholdForHsvOnly,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_LuminanceRangeForSpecificSpaces",
                static e => GetValue(e.LuminanceRange) > 0
                         && e.MainKeyColor == KeyColorType.Custom
                         && e.ColorSpace != AdvancedColorSpace.RGB
                         && e.ColorSpace != AdvancedColorSpace.YUV,
                Texts.ValidationRule_Info_LuminanceRangeForSpecificSpaces,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_CompleteKeyBypassesQuality",
                static e => e.IsCompleteKey
                         && (GetValue(e.SpillSuppression) > 0
                          || GetValue(e.EdgeDesaturation) > 0
                          || GetValue(e.PreserveLuminance) > 0),
                Texts.ValidationRule_Info_CompleteKeyBypassesQuality,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Warning_ExceptionColorTransparent",
                static e => e.ExceptionColor1.A == 0 && GetValue(e.ExceptionTolerance) > 0,
                Texts.ValidationRule_Warning_ExceptionColorTransparent,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Info_ExceptionGradientStrengthZero",
                static e => e.ExceptionColor1.A > 0
                         && GetValue(e.ExceptionGradientStrength) == 0
                         && (e.ExceptionColor1 != e.ExceptionColor2
                          || GetValue(e.ExceptionGradientAngle) != DefaultGradientAngle),
                Texts.ValidationRule_Info_ExceptionGradientStrengthZero,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ResidualColorCorrectionZero",
                static e => GetValue(e.ResidualColorCorrection) == 0
                         && (e.TargetResidualColor.A != 0 || e.CorrectedColor.A != 0),
                Texts.ValidationRule_Info_ResidualColorCorrectionZero,
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Warning_GradientStrengthHigh",
                static e => GetValue(e.GradientStrength) > GradientStrengthHighThreshold,
                Texts.ValidationRule_Warning_GradientStrengthHigh,
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Performance_HighQualitySettings",
                static e => e.QualityPreset == QualityPreset.HighQuality
                         && (GetValue(e.EdgeBlur) > EdgeBlurComplexThreshold
                          || GetValue(e.Despot) > DespotComplexThreshold),
                Texts.ValidationRule_Performance_HighQualitySettings,
                ValidationLevel.Performance));

            return builder.MoveToImmutable();
        }

        private static double GetValue(Animation animation) =>
            animation.Values.Count > 0 ? animation.Values[0].Value : animation.DefaultValue;
    }
}
