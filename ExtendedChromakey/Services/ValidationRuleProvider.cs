using ExtendedChromaKey.Models;
using System.Collections.Immutable;
using YukkuriMovieMaker.Commons;

namespace ExtendedChromaKey.Services
{
    internal sealed class ValidationRuleProvider
    {
        private static readonly Lazy<ImmutableArray<ValidationRule>> _rules =
            new(BuildRules, LazyThreadSafetyMode.ExecutionAndPublication);

        public ImmutableArray<ValidationRule> Rules => _rules.Value;

        private static ImmutableArray<ValidationRule> BuildRules()
        {
            var builder = ImmutableArray.CreateBuilder<ValidationRule>(21);

            builder.Add(new ValidationRule(
                "Error_BaseColorTransparency",
                static effect => effect.MainKeyColor == KeyColorType.Custom && effect.BaseColor.A == 0,
                "カスタムモードでは、基本色を不透明に設定してください。完全に透明な色はキーイングできません。",
                ValidationLevel.Error));

            builder.Add(new ValidationRule(
                "Error_ClipBlackWhiteInverted",
                static effect => GetAnimationValue(effect.ClipBlack) >= GetAnimationValue(effect.ClipWhite),
                "クリップ(黒)の値がクリップ(白)以上になっています。マスクが正常に生成されません。",
                ValidationLevel.Error));

            builder.Add(new ValidationRule(
                "Warning_ToleranceExtremelyLow",
                static effect => GetAnimationValue(effect.Tolerance) < 1,
                "許容値が極端に低いため、意図した範囲が透過されない可能性があります。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_ToleranceExtremelyHigh",
                static effect => GetAnimationValue(effect.Tolerance) > 85,
                "許容値が極端に高いため、前景の意図しない部分まで透過される可能性があります。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_SoftnessWithoutTolerance",
                static effect => GetAnimationValue(effect.EdgeSoftness) > 0 && GetAnimationValue(effect.Tolerance) < 1,
                "エッジの柔らかさを機能させるには、許容値を少し上げる必要があります。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_HighLuminanceMix",
                static effect => GetAnimationValue(effect.LuminanceMix) > 90,
                "輝度ミックスが高すぎると、色の情報が無視され、明るさだけでキーイングされます。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Warning_ClipAndSoftnessConflict",
                static effect => (GetAnimationValue(effect.ClipBlack) > 50 || GetAnimationValue(effect.ClipWhite) < 50) && GetAnimationValue(effect.EdgeSoftness) > 50,
                "クリップ設定とエッジの柔らかさの両方が高く設定されています。意図しない硬い輪郭になる可能性があります。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Info_GradientStrengthZero",
                static effect => GetAnimationValue(effect.GradientStrength) == 0 && (effect.EndColor != effect.BaseColor || GetAnimationValue(effect.GradientAngle) != 90),
                "グラデーション強度が0%のため、「終端色」と「グラデーション角度」は効果がありません。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_SpillSuppressionWithCustom",
                static effect => effect.MainKeyColor == KeyColorType.Custom && GetAnimationValue(effect.SpillSuppression) > 0,
                "スピル除去は、主要キーイング色が「緑」「青」「赤」の時に最も効果的です。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ReplaceIntensityZero",
                static effect => GetAnimationValue(effect.ReplaceIntensity) == 0 && (effect.ReplaceColor != System.Windows.Media.Colors.Transparent || GetAnimationValue(effect.PreserveLuminance) > 0),
                "置換の強度が0%のため、「置換色」と「輝度保持」は効果がありません。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ReplaceColorTransparent",
                static effect => GetAnimationValue(effect.ReplaceIntensity) > 0 && effect.ReplaceColor.A == 0,
                "置換色が透明です。これは除去した領域の不透明度を下げる効果になります。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ColorSpaceForCustomOnly",
                static effect => effect.MainKeyColor != KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.Lab,
                "色空間の変更は、主要キーイング色が「カスタム」の時にのみ適用されます。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_HueRangeForSpecificSpaces",
                static effect => GetAnimationValue(effect.HueRange) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.HSV && effect.ColorSpace != AdvancedColorSpace.LCH,
                "色相範囲は、色空間が「HSV」または「LCH」の場合にのみ有効です。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_SaturationThresholdForHsvOnly",
                static effect => GetAnimationValue(effect.SaturationThreshold) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.HSV,
                "彩度閾値は、色空間が「HSV」の場合にのみ有効です。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_LuminanceRangeForSpecificSpaces",
                static effect => GetAnimationValue(effect.LuminanceRange) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.RGB && effect.ColorSpace != AdvancedColorSpace.YUV,
                "明度範囲は、色空間が「RGB」または「YUV」の場合にのみ有効です。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_CompleteKeyBypassesQuality",
                static effect => effect.IsCompleteKey && (GetAnimationValue(effect.SpillSuppression) > 0 || GetAnimationValue(effect.EdgeDesaturation) > 0 || GetAnimationValue(effect.PreserveLuminance) > 0),
                "完全クロマキーモードでは、スピル除去・エッジ彩度除去・輝度保持は無効になります。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Warning_ExceptionColorTransparent",
                static effect => effect.ExceptionColor1.A == 0 && GetAnimationValue(effect.ExceptionTolerance) > 0,
                "例外色が透明に設定されているため、「例外設定」は機能しません。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Info_ExceptionGradientStrengthZero",
                static effect => effect.ExceptionColor1.A > 0 && GetAnimationValue(effect.ExceptionGradientStrength) == 0 && (effect.ExceptionColor1 != effect.ExceptionColor2 || GetAnimationValue(effect.ExceptionGradientAngle) != 90),
                "例外グラデーション強度が0%のため、「例外色2 (終端)」と「グラデーション角度」は効果がありません。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Info_ResidualColorCorrectionZero",
                static effect => GetAnimationValue(effect.ResidualColorCorrection) == 0 && (effect.TargetResidualColor != System.Windows.Media.Colors.Transparent || effect.CorrectedColor != System.Windows.Media.Colors.Transparent),
                "残存色補正の強度が0%のため、「補正対象色」と「補正後の色」は効果がありません。",
                ValidationLevel.Info));

            builder.Add(new ValidationRule(
                "Warning_GradientStrengthHigh",
                static effect => GetAnimationValue(effect.GradientStrength) > 90,
                "グラデーション強度が高すぎると、意図しない範囲まで透過される可能性があります。煙などの透明度が高い部分で問題が発生する場合は、「透明度品質」を上げてください。",
                ValidationLevel.Warning));

            builder.Add(new ValidationRule(
                "Performance_HighQualitySettings",
                static effect => effect.QualityPreset == QualityPreset.HighQuality && (GetAnimationValue(effect.EdgeBlur) > 20 || GetAnimationValue(effect.Despot) > 5),
                "高品質モードで重い処理が組み合わさっています。パフォーマンスに影響する可能性があります。",
                ValidationLevel.Performance));

            return builder.MoveToImmutable();
        }

        private static double GetAnimationValue(Animation animation) =>
            animation.Values.Count > 0 ? animation.Values[0].Value : animation.DefaultValue;
    }
}