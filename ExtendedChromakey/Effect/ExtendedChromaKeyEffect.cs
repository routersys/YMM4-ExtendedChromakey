using ExtendedChromaKey.Attributes;
using ExtendedChromaKey.Localization;
using ExtendedChromaKey.Models;
using ExtendedChromaKey.Services;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace ExtendedChromaKey.Effect
{
    [VideoEffect(nameof(Texts.ExtendedChromaKey_Label), ["拡張"], ["extended chroma key", "拡張クロマキー", "chromakey"], IsAviUtlSupported = false, ResourceType = typeof(Texts))]
    public sealed class ExtendedChromaKeyEffect : VideoEffectBase
    {
        public override string Label => Texts.ExtendedChromaKey_Label;

        [Display(GroupName = nameof(Texts.GroupName_BasicSettings), Name = nameof(Texts.Empty), Order = 0, ResourceType = typeof(Texts))]
        [ValidationPanelEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public bool ValidationPlaceholder { get; set; }

        [Display(GroupName = nameof(Texts.GroupName_BasicSettings), Name = nameof(Texts.BaseColor_Name), Description = nameof(Texts.BaseColor_Desc), Order = 1, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color BaseColor
        {
            get => baseColor;
            set
            {
                if (Set(ref baseColor, value))
                    EndColor = value;
            }
        }
        private Color baseColor = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_BasicSettings), Name = nameof(Texts.EndColor_Name), Description = nameof(Texts.EndColor_Desc), Order = 2, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color EndColor { get => endColor; set => Set(ref endColor, value); }
        private Color endColor = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_KeyingMode), Name = nameof(Texts.MainKeyColor_Name), Description = nameof(Texts.MainKeyColor_Desc), Order = 3, ResourceType = typeof(Texts))]
        [EnumComboBox]
        public KeyColorType MainKeyColor { get => mainKeyColor; set => Set(ref mainKeyColor, value); }
        private KeyColorType mainKeyColor = KeyColorType.Custom;

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.Tolerance_Name), Description = nameof(Texts.Tolerance_Desc), Order = 4, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Tolerance { get; } = new(20, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.LuminanceMix_Name), Description = nameof(Texts.LuminanceMix_Desc), Order = 5, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation LuminanceMix { get; } = new(50, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.EdgeSoftness_Name), Description = nameof(Texts.EdgeSoftness_Desc), Order = 6, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeSoftness { get; } = new(10, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.ClipBlack_Name), Description = nameof(Texts.ClipBlack_Desc), Order = 7, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ClipBlack { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.ClipWhite_Name), Description = nameof(Texts.ClipWhite_Desc), Order = 8, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ClipWhite { get; } = new(100, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingAdjustment), Name = nameof(Texts.EdgeBlur_Name), Description = nameof(Texts.EdgeBlur_Desc), Order = 9, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", 0, 50)]
        public Animation EdgeBlur { get; } = new(0, 0, 50);

        [Display(GroupName = nameof(Texts.GroupName_Gradient), Name = nameof(Texts.GradientStrength_Name), Description = nameof(Texts.GradientStrength_Desc), Order = 10, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation GradientStrength { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_Gradient), Name = nameof(Texts.GradientAngle_Name), Description = nameof(Texts.GradientAngle_Desc), Order = 11, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation GradientAngle { get; } = new(90, -360, 360);

        [Display(GroupName = nameof(Texts.GroupName_ColorReplacement), Name = nameof(Texts.ReplaceColor_Name), Description = nameof(Texts.ReplaceColor_Desc), Order = 12, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color ReplaceColor { get => replaceColor; set => Set(ref replaceColor, value); }
        private Color replaceColor = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_ColorReplacement), Name = nameof(Texts.ReplaceIntensity_Name), Description = nameof(Texts.ReplaceIntensity_Desc), Order = 13, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ReplaceIntensity { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorReplacement), Name = nameof(Texts.PreserveLuminance_Name), Description = nameof(Texts.PreserveLuminance_Desc), Order = 14, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation PreserveLuminance { get; } = new(75, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingMode), Name = nameof(Texts.ColorSpace_Name), Description = nameof(Texts.ColorSpace_Desc), Order = 15, ResourceType = typeof(Texts))]
        [EnumComboBox]
        public AdvancedColorSpace ColorSpace { get => colorSpace; set => Set(ref colorSpace, value); }
        private AdvancedColorSpace colorSpace = AdvancedColorSpace.Lab;

        [Display(GroupName = nameof(Texts.GroupName_KeyingMode), Name = nameof(Texts.HueRange_Name), Description = nameof(Texts.HueRange_Desc), Order = 16, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 50)]
        public Animation HueRange { get; } = new(10, 0, 50);

        [Display(GroupName = nameof(Texts.GroupName_KeyingMode), Name = nameof(Texts.SaturationThreshold_Name), Description = nameof(Texts.SaturationThreshold_Desc), Order = 17, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation SaturationThreshold { get; } = new(10, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_KeyingMode), Name = nameof(Texts.LuminanceRange_Name), Description = nameof(Texts.LuminanceRange_Desc), Order = 18, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation LuminanceRange { get; } = new(40, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.SpillSuppression_Name), Description = nameof(Texts.SpillSuppression_Desc), Order = 19, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation SpillSuppression { get; } = new(15, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.EdgeBalance_Name), Description = nameof(Texts.EdgeBalance_Desc), Order = 20, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", -100, 100)]
        public Animation EdgeBalance { get; } = new(0, -100, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.Despot_Name), Description = nameof(Texts.Despot_Desc), Order = 21, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", 0, 10)]
        public Animation Despot { get; } = new(0, 0, 10);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.Erode_Name), Description = nameof(Texts.Erode_Desc), Order = 22, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "px", -10, 10)]
        public Animation Erode { get; } = new(0, -10, 10);

        [Display(GroupName = nameof(Texts.GroupName_ExceptionSettings), Name = nameof(Texts.ExceptionColor1_Name), Description = nameof(Texts.ExceptionColor1_Desc), Order = 23, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color ExceptionColor1
        {
            get => exceptionColor1;
            set
            {
                if (Set(ref exceptionColor1, value))
                    ExceptionColor2 = value;
            }
        }
        private Color exceptionColor1 = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_ExceptionSettings), Name = nameof(Texts.ExceptionColor2_Name), Description = nameof(Texts.ExceptionColor2_Desc), Order = 24, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color ExceptionColor2 { get => exceptionColor2; set => Set(ref exceptionColor2, value); }
        private Color exceptionColor2 = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_ExceptionSettings), Name = nameof(Texts.ExceptionTolerance_Name), Description = nameof(Texts.ExceptionTolerance_Desc), Order = 25, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ExceptionTolerance { get; } = new(10, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ExceptionSettings), Name = nameof(Texts.ExceptionGradientStrength_Name), Description = nameof(Texts.ExceptionGradientStrength_Desc), Order = 26, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ExceptionGradientStrength { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ExceptionSettings), Name = nameof(Texts.ExceptionGradientAngle_Name), Description = nameof(Texts.ExceptionGradientAngle_Desc), Order = 27, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation ExceptionGradientAngle { get; } = new(90, -360, 360);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.EdgeDesaturation_Name), Description = nameof(Texts.EdgeDesaturation_Desc), Order = 28, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeDesaturation { get; } = new(20, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.KeyCleanup_Name), Description = nameof(Texts.KeyCleanup_Desc), Order = 29, ResourceType = typeof(Texts))]
        [AnimationSlider("F2", "px", -50, 50)]
        public Animation KeyCleanup { get; } = new(0, -50, 50);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.EdgeDetection_Name), Description = nameof(Texts.EdgeDetection_Desc), Order = 30, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeDetection { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.Denoise_Name), Description = nameof(Texts.Denoise_Desc), Order = 31, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Denoise { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.Feathering_Name), Description = nameof(Texts.Feathering_Desc), Order = 32, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Feathering { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.TranslucentDespill_Name), Description = nameof(Texts.TranslucentDespill_Desc), Order = 33, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation TranslucentDespill { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.TransparencyQuality_Name), Description = nameof(Texts.TransparencyQuality_Desc), Order = 34, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation TransparencyQuality { get; } = new(50, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_QualityOptimization), Name = nameof(Texts.AlphaBlendAdjustment_Name), Description = nameof(Texts.AlphaBlendAdjustment_Desc), Order = 35, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation AlphaBlendAdjustment { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.TargetResidualColor_Name), Description = nameof(Texts.TargetResidualColor_Desc), Order = 36, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color TargetResidualColor { get => targetResidualColor; set => Set(ref targetResidualColor, value); }
        private Color targetResidualColor = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.CorrectedColor_Name), Description = nameof(Texts.CorrectedColor_Desc), Order = 37, ResourceType = typeof(Texts))]
        [ColorPicker]
        public Color CorrectedColor { get => correctedColor; set => Set(ref correctedColor, value); }
        private Color correctedColor = Colors.Transparent;

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.ResidualColorCorrection_Name), Description = nameof(Texts.ResidualColorCorrection_Desc), Order = 38, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ResidualColorCorrection { get; } = new(0, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.CorrectionTolerance_Name), Description = nameof(Texts.CorrectionTolerance_Desc), Order = 39, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation CorrectionTolerance { get; } = new(20, 0, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.ForegroundBrightness_Name), Description = nameof(Texts.ForegroundBrightness_Desc), Order = 40, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", -100, 100)]
        public Animation ForegroundBrightness { get; } = new(0, -100, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.ForegroundContrast_Name), Description = nameof(Texts.ForegroundContrast_Desc), Order = 41, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", -100, 100)]
        public Animation ForegroundContrast { get; } = new(0, -100, 100);

        [Display(GroupName = nameof(Texts.GroupName_ColorCorrection), Name = nameof(Texts.ForegroundSaturation_Name), Description = nameof(Texts.ForegroundSaturation_Desc), Order = 42, ResourceType = typeof(Texts))]
        [AnimationSlider("F1", "%", -100, 100)]
        public Animation ForegroundSaturation { get; } = new(0, -100, 100);

        [Display(GroupName = nameof(Texts.GroupName_Other), Name = nameof(Texts.IsCompleteKey_Name), Description = nameof(Texts.IsCompleteKey_Desc), Order = 43, ResourceType = typeof(Texts))]
        [ToggleSlider]
        public bool IsCompleteKey { get => isCompleteKey; set => Set(ref isCompleteKey, value); }
        private bool isCompleteKey;

        [Display(GroupName = nameof(Texts.GroupName_Other), Name = nameof(Texts.IsInverted_Name), Description = nameof(Texts.IsInverted_Desc), Order = 44, ResourceType = typeof(Texts))]
        [ToggleSlider]
        public bool IsInverted { get => isInverted; set => Set(ref isInverted, value); }
        private bool isInverted;

        [Display(GroupName = nameof(Texts.GroupName_Other), Name = nameof(Texts.QualityPreset_Name), Description = nameof(Texts.QualityPreset_Desc), Order = 45, ResourceType = typeof(Texts))]
        [EnumComboBox]
        public QualityPreset QualityPreset { get => qualityPreset; set => Set(ref qualityPreset, value); }
        private QualityPreset qualityPreset = QualityPreset.Balanced;

        [Display(GroupName = nameof(Texts.GroupName_Debug), Name = nameof(Texts.DebugMode_Name), Description = nameof(Texts.DebugMode_Desc), Order = 46, ResourceType = typeof(Texts))]
        [EnumComboBox]
        public DebugViewMode DebugMode { get => debugMode; set => Set(ref debugMode, value); }
        private DebugViewMode debugMode = DebugViewMode.Result;

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription) => [];

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices) =>
            ServiceRegistry.Instance.CreateProcessor(devices, this);

        protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [
            Tolerance, LuminanceMix, EdgeSoftness, ClipBlack, ClipWhite, EdgeBlur,
            GradientStrength, GradientAngle,
            HueRange, SaturationThreshold, LuminanceRange,
            SpillSuppression, EdgeBalance, Despot, Erode, EdgeDesaturation, KeyCleanup,
            EdgeDetection, Denoise, Feathering, TranslucentDespill,
            ReplaceIntensity, PreserveLuminance,
            ExceptionTolerance, ExceptionGradientStrength, ExceptionGradientAngle,
            ResidualColorCorrection, CorrectionTolerance,
            TransparencyQuality, AlphaBlendAdjustment,
            ForegroundBrightness, ForegroundContrast, ForegroundSaturation,
        ];
    }
}
