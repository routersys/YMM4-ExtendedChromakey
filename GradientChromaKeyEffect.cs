using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Media;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Effects;

namespace YMM4GradientChromaKey.Effect.Video.GradientChromaKey
{
    [VideoEffect("拡張クロマキー", ["拡張"], ["extended chroma key", "拡張クロマキー", "chromakey"], IsAviUtlSupported = false)]
    public class GradientChromaKeyEffect : VideoEffectBase
    {
        public override string Label => "拡張クロマキー";

        [Display(GroupName = "基本設定", Name = "", Description = "現在の設定に問題がある場合はここに表示されます。", Order = 0)]
        [ValidationPanelEditor(PropertyEditorSize = PropertyEditorSize.FullWidth)]
        public bool ValidationPlaceholder { get; set; }

        [Display(GroupName = "基本設定", Name = "基本色", Description = "透過する基本色を指定します。", Order = 1)]
        [ColorPicker]
        public Color BaseColor
        {
            get => baseColor;
            set
            {
                if (Set(ref baseColor, value))
                {
                    EndColor = value;
                }
            }
        }
        private Color baseColor = Colors.Transparent;

        [Display(GroupName = "基本設定", Name = "終端色", Description = "グラデーションの終端色を指定します。基本色とこの色の間がクロマキー対象になります。", Order = 2)]
        [ColorPicker]
        public Color EndColor { get => endColor; set => Set(ref endColor, value); }
        private Color endColor = Colors.Transparent;

        [Display(GroupName = "キーイングモード", Name = "主要キーイング色", Description = "キーイングの基準となる主要な色を選択します。背景色に合わせて選択してください。", Order = 3)]
        [EnumComboBox]
        public KeyColorType MainKeyColor { get => mainKeyColor; set => Set(ref mainKeyColor, value); }
        private KeyColorType mainKeyColor = KeyColorType.Custom;

        [Display(GroupName = "キーイング調整", Name = "許容値", Description = "色の許容範囲。0%に設定すると選択した色のみが対象になります。", Order = 4)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Tolerance { get; } = new(20, 0, 100);

        [Display(GroupName = "キーイング調整", Name = "輝度ミックス", Description = "輝度をキーイング判定にどれだけ使用するかを調整します。値を上げると、明るい部分（発光など）が背景から分離されやすくなります。", Order = 5)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation LuminanceMix { get; } = new(50, 0, 100);

        [Display(GroupName = "キーイング調整", Name = "エッジの柔らかさ", Description = "透過境界の柔らかさを調整します。", Order = 6)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeSoftness { get; } = new(10, 0, 100);

        [Display(GroupName = "キーイング調整", Name = "クリップ(黒)", Description = "マスクの黒レベルを調整します。背景のノイズ除去に有効です。", Order = 7)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ClipBlack { get; } = new(0, 0, 100);

        [Display(GroupName = "キーイング調整", Name = "クリップ(白)", Description = "マスクの白レベルを調整します。前景（特に発光部分）をくっきりと残すのに有効です。", Order = 8)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ClipWhite { get; } = new(100, 0, 100);

        [Display(GroupName = "キーイング調整", Name = "エッジぼかし", Description = "マスクの境界をぼかして滑らかにします。粗いエッジの調整に有効です。", Order = 9)]
        [AnimationSlider("F1", "px", 0, 50)]
        public Animation EdgeBlur { get; } = new(0, 0, 50);

        [Display(GroupName = "グラデーション", Name = "グラデーション強度", Description = "グラデーション効果の強さを調整します。0%で基本色のみ、100%で基本色から終端色へのグラデーションになります。", Order = 10)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation GradientStrength { get; } = new(0, 0, 100);

        [Display(GroupName = "グラデーション", Name = "グラデーション角度", Description = "グラデーションの方向を度数で指定します。", Order = 11)]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation GradientAngle { get; } = new(90, -360, 360);

        [Display(GroupName = "色置換", Name = "置換色", Description = "除去した領域を置き換える色を指定します。", Order = 12)]
        [ColorPicker]
        public Color ReplaceColor { get => replaceColor; set => Set(ref replaceColor, value); }
        private Color replaceColor = Colors.Transparent;

        [Display(GroupName = "色置換", Name = "置換の強度", Description = "除去領域を置換色で塗りつぶす強度。", Order = 13)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ReplaceIntensity { get; } = new(0, 0, 100);

        [Display(GroupName = "色置換", Name = "輝度保持", Description = "色置換時に元の輝度をどれだけ保持するか。", Order = 14)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation PreserveLuminance { get; } = new(75, 0, 100);

        [Display(GroupName = "キーイングモード", Name = "色空間(カスタム用)", Description = "「主要キーイング色」でカスタムを選択した場合の色距離計算に使用する色空間を選択します。", Order = 15)]
        [EnumComboBox]
        public AdvancedColorSpace ColorSpace { get => colorSpace; set => Set(ref colorSpace, value); }
        private AdvancedColorSpace colorSpace = AdvancedColorSpace.Lab;

        [Display(GroupName = "キーイングモード", Name = "色相範囲", Description = "HSV/LCH色空間使用時の色相許容範囲。", Order = 16)]
        [AnimationSlider("F1", "%", 0, 50)]
        public Animation HueRange { get; } = new(10, 0, 50);

        [Display(GroupName = "キーイングモード", Name = "彩度閾値", Description = "この値以下の彩度の色は、彩度を無視して明度で比較されます。", Order = 17)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation SaturationThreshold { get; } = new(10, 0, 100);

        [Display(GroupName = "キーイングモード", Name = "明度範囲", Description = "RGB/YUV色空間使用時の明度差の許容範囲。", Order = 18)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation LuminanceRange { get; } = new(40, 0, 100);

        [Display(GroupName = "品質最適化", Name = "スピル除去", Description = "背景色の被写体への全体的な映り込み（色かぶり）を除去します。", Order = 19)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation SpillSuppression { get; } = new(15, 0, 100);

        [Display(GroupName = "品質最適化", Name = "エッジバランス", Description = "キーイングの境界を内外に調整します。プラスで内側、マイナスで外側に移動します。", Order = 20)]
        [AnimationSlider("F1", "%", -100, 100)]
        public Animation EdgeBalance { get; } = new(0, -100, 100);

        [Display(GroupName = "品質最適化", Name = "デスポット", Description = "マスク内の小さなノイズ（穴やゴミ）を除去します。", Order = 21)]
        [AnimationSlider("F1", "px", 0, 10)]
        public Animation Despot { get; } = new(0, 0, 10);

        [Display(GroupName = "品質最適化", Name = "エッジの浸食", Description = "マスクの境界を拡大・縮小します。輪郭に残る細いフリンジの除去に有効です。", Order = 22)]
        [AnimationSlider("F1", "px", -10, 10)]
        public Animation Erode { get; } = new(0, -10, 10);

        [Display(GroupName = "例外設定", Name = "例外色1 (基本)", Description = "クロマキーから除外する基本色を指定します。この色が透明の場合、この機能は無効になります。", Order = 23)]
        [ColorPicker]
        public Color ExceptionColor1
        {
            get => exceptionColor1;
            set
            {
                if (Set(ref exceptionColor1, value))
                {
                    ExceptionColor2 = value;
                }
            }
        }
        private Color exceptionColor1 = Colors.Transparent;

        [Display(GroupName = "例外設定", Name = "例外色2 (終端)", Description = "グラデーションの終端色を指定します。この範囲の色はクロマキーされにくくなります。", Order = 24)]
        [ColorPicker]
        public Color ExceptionColor2 { get => exceptionColor2; set => Set(ref exceptionColor2, value); }
        private Color exceptionColor2 = Colors.Transparent;

        [Display(GroupName = "例外設定", Name = "許容値", Description = "例外色の許容範囲。値を大きくすると、より広い範囲の色が保護されます。", Order = 25)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ExceptionTolerance { get; } = new(10, 0, 100);

        [Display(GroupName = "例外設定", Name = "グラデーション強度", Description = "例外色のグラデーション効果の強さ。", Order = 26)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ExceptionGradientStrength { get; } = new(0, 0, 100);

        [Display(GroupName = "例外設定", Name = "グラデーション角度", Description = "例外色のグラデーションの方向。", Order = 27)]
        [AnimationSlider("F1", "°", -360, 360)]
        public Animation ExceptionGradientAngle { get; } = new(90, -360, 360);

        [Display(GroupName = "品質最適化", Name = "エッジの色のじみ除去", Description = "輪郭部分の色の映り込みを低減させ、より自然な合成を実現します。", Order = 28)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeDesaturation { get; } = new(20, 0, 100);

        [Display(GroupName = "品質最適化", Name = "キー・クリーンアップ", Description = "キーイングしたマスクの境界を調整します。マイナスで縮小、プラスで拡大します。", Order = 29)]
        [AnimationSlider("F2", "px", -50, 50)]
        public Animation KeyCleanup { get; } = new(0, -50, 50);

        [Display(GroupName = "品質最適化", Name = "エッジ検出", Description = "輪郭のディテールを保持する強度。値を上げるとエッジがシャープになりますが、ノイズが増えることがあります。", Order = 30)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation EdgeDetection { get; } = new(0, 0, 100);

        [Display(GroupName = "品質最適化", Name = "ノイズ除去", Description = "マスクから細かいノイズを除去します。", Order = 31)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Denoise { get; } = new(0, 0, 100);

        [Display(GroupName = "品質最適化", Name = "フェザリング", Description = "マスクのエッジをぼかして、背景となじませます。", Order = 32)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation Feathering { get; } = new(0, 0, 100);

        [Display(GroupName = "色補正", Name = "残存色補正", Description = "クロマキーで残った背景色を他の色に変換します。", Order = 33)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation ResidualColorCorrection { get; } = new(0, 0, 100);

        [Display(GroupName = "色補正", Name = "補正対象色", Description = "補正したい残存色を指定します。", Order = 34)]
        [ColorPicker]
        public Color TargetResidualColor { get => targetResidualColor; set => Set(ref targetResidualColor, value); }
        private Color targetResidualColor = Colors.Transparent;

        [Display(GroupName = "色補正", Name = "補正後の色", Description = "残存色をこの色に変換します。", Order = 35)]
        [ColorPicker]
        public Color CorrectedColor { get => correctedColor; set => Set(ref correctedColor, value); }
        private Color correctedColor = Colors.Transparent;

        [Display(GroupName = "色補正", Name = "補正許容値", Description = "色補正の許容範囲。値を大きくすると、より広い範囲の色が補正されます。", Order = 36)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation CorrectionTolerance { get; } = new(20, 0, 100);

        [Display(GroupName = "高度な設定", Name = "透明度品質", Description = "透明度の高い部分（煙など）の処理品質を向上させます。", Order = 37)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation TransparencyQuality { get; } = new(50, 0, 100);

        [Display(GroupName = "高度な設定", Name = "アルファブレンド調整", Description = "前景と背景の混合処理を調整します。", Order = 38)]
        [AnimationSlider("F1", "%", 0, 100)]
        public Animation AlphaBlendAdjustment { get; } = new(0, 0, 100);

        [Display(GroupName = "その他", Name = "完全クロマキー", Description = "オンにすると、輝度を保持せず完全に透過させます。スピル除去なども無効になります。", Order = 39)]
        [ToggleSlider]
        public bool IsCompleteKey { get => isCompleteKey; set => Set(ref isCompleteKey, value); }
        private bool isCompleteKey = false;

        [Display(GroupName = "その他", Name = "反転", Description = "透過範囲を反転します。", Order = 40)]
        [ToggleSlider]
        public bool IsInverted { get => isInverted; set => Set(ref isInverted, value); }
        private bool isInverted = false;

        [Display(GroupName = "その他", Name = "品質プリセット", Description = "処理の品質を選択します。高品質ほど処理が重くなります。", Order = 41)]
        [EnumComboBox]
        public QualityPreset QualityPreset { get => qualityPreset; set => Set(ref qualityPreset, value); }
        private QualityPreset qualityPreset = QualityPreset.Balanced;

        [Display(GroupName = "デバッグ", Name = "表示モード", Description = "デバッグ用に、エフェクトの各処理段階の画像を表示します。", Order = 42)]
        [EnumComboBox]
        public DebugViewMode DebugMode { get => debugMode; set => Set(ref debugMode, value); }
        private DebugViewMode debugMode = DebugViewMode.Result;

        public override IEnumerable<string> CreateExoVideoFilters(int keyFrameIndex, ExoOutputDescription exoOutputDescription)
        {
            return [];
        }

        public override IVideoEffectProcessor CreateVideoEffect(IGraphicsDevicesAndContext devices)
        {
            return new GradientChromaKeyEffectProcessor(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() =>
        [
            Tolerance, LuminanceMix, EdgeSoftness, ClipBlack, ClipWhite, EdgeBlur,
            GradientStrength, GradientAngle,
            HueRange, SaturationThreshold, LuminanceRange,
            SpillSuppression, EdgeBalance, Despot, Erode, EdgeDesaturation, KeyCleanup,
            EdgeDetection, Denoise, Feathering,
            ReplaceIntensity, PreserveLuminance,
            ExceptionTolerance, ExceptionGradientStrength, ExceptionGradientAngle,
            ResidualColorCorrection, CorrectionTolerance,
            TransparencyQuality, AlphaBlendAdjustment
        ];
    }

    public enum KeyColorType
    {
        [Display(Name = "カスタム")]
        Custom,
        [Display(Name = "緑")]
        Green,
        [Display(Name = "青")]
        Blue,
        [Display(Name = "赤")]
        Red
    }

    public enum AdvancedColorSpace
    {
        [Display(Name = "RGB（標準）")]
        RGB,
        [Display(Name = "HSV（色相重視）")]
        HSV,
        [Display(Name = "CIE Lab（知覚的）")]
        Lab,
        [Display(Name = "YUV（放送品質）")]
        YUV,
        [Display(Name = "XYZ（CIE標準）")]
        XYZ,
        [Display(Name = "LCH（知覚円筒）")]
        LCH
    }

    public enum QualityPreset
    {
        [Display(Name = "高速")]
        Fast,
        [Display(Name = "バランス")]
        Balanced,
        [Display(Name = "高品質")]
        HighQuality
    }

    public enum DebugViewMode
    {
        [Display(Name = "最終結果")]
        Result,
        [Display(Name = "マスク表示")]
        Matte,
        [Display(Name = "色距離")]
        ColorDistance,
        [Display(Name = "スピル除去後")]
        SpillSuppressed,
        [Display(Name = "色補正後")]
        ColorCorrected,
    }
}