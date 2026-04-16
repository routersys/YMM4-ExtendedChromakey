using ExtendedChromaKey.Services;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace ExtendedChromaKey.Effect
{
    public sealed class ExtendedChromaKeyCustomEffect : D2D1CustomShaderEffectBase
    {
        public Vector2 ScreenSize { set => SetValue((int)EffectImpl.PropertyIndex.ScreenSize, value); }
        public float Tolerance { set => SetValue((int)EffectImpl.PropertyIndex.Tolerance, value); }
        public float LuminanceMix { set => SetValue((int)EffectImpl.PropertyIndex.LuminanceMix, value); }
        public float EdgeSoftness { set => SetValue((int)EffectImpl.PropertyIndex.EdgeSoftness, value); }
        public float ClipBlack { set => SetValue((int)EffectImpl.PropertyIndex.ClipBlack, value); }
        public float ClipWhite { set => SetValue((int)EffectImpl.PropertyIndex.ClipWhite, value); }
        public float EdgeBlur { set => SetValue((int)EffectImpl.PropertyIndex.EdgeBlur, value); }
        public float GradientStrength { set => SetValue((int)EffectImpl.PropertyIndex.GradientStrength, value); }
        public float GradientAngle { set => SetValue((int)EffectImpl.PropertyIndex.GradientAngle, value); }
        public float SpillSuppression { set => SetValue((int)EffectImpl.PropertyIndex.SpillSuppression, value); }
        public float EdgeBalance { set => SetValue((int)EffectImpl.PropertyIndex.EdgeBalance, value); }
        public float Despot { set => SetValue((int)EffectImpl.PropertyIndex.Despot, value); }
        public float Erode { set => SetValue((int)EffectImpl.PropertyIndex.Erode, value); }
        public float EdgeDesaturation { set => SetValue((int)EffectImpl.PropertyIndex.EdgeDesaturation, value); }
        public float KeyCleanup { set => SetValue((int)EffectImpl.PropertyIndex.KeyCleanup, value); }
        public Vector4 BaseColor { set => SetValue((int)EffectImpl.PropertyIndex.BaseColor, value); }
        public Vector3 EndColor { set => SetValue((int)EffectImpl.PropertyIndex.EndColor, value); }
        public Vector4 ReplaceColor { set => SetValue((int)EffectImpl.PropertyIndex.ReplaceColor, value); }
        public int ColorSpace { set => SetValue((int)EffectImpl.PropertyIndex.ColorSpace, value); }
        public int MainKeyColor { set => SetValue((int)EffectImpl.PropertyIndex.MainKeyColor, value); }
        public int IsInverted { set => SetValue((int)EffectImpl.PropertyIndex.IsInverted, value); }
        public int IsCompleteKey { set => SetValue((int)EffectImpl.PropertyIndex.IsCompleteKey, value); }
        public float HueRange { set => SetValue((int)EffectImpl.PropertyIndex.HueRange, value); }
        public float SaturationThreshold { set => SetValue((int)EffectImpl.PropertyIndex.SaturationThreshold, value); }
        public int QualityPreset { set => SetValue((int)EffectImpl.PropertyIndex.QualityPreset, value); }
        public float LuminanceRange { set => SetValue((int)EffectImpl.PropertyIndex.LuminanceRange, value); }
        public float EdgeDetection { set => SetValue((int)EffectImpl.PropertyIndex.EdgeDetection, value); }
        public float Denoise { set => SetValue((int)EffectImpl.PropertyIndex.Denoise, value); }
        public float Feathering { set => SetValue((int)EffectImpl.PropertyIndex.Feathering, value); }
        public float ReplaceIntensity { set => SetValue((int)EffectImpl.PropertyIndex.ReplaceIntensity, value); }
        public float PreserveLuminance { set => SetValue((int)EffectImpl.PropertyIndex.PreserveLuminance, value); }
        public int DebugMode { set => SetValue((int)EffectImpl.PropertyIndex.DebugMode, value); }
        public Vector4 ExceptionColor1 { set => SetValue((int)EffectImpl.PropertyIndex.ExceptionColor1, value); }
        public Vector3 ExceptionColor2 { set => SetValue((int)EffectImpl.PropertyIndex.ExceptionColor2, value); }
        public float ExceptionTolerance { set => SetValue((int)EffectImpl.PropertyIndex.ExceptionTolerance, value); }
        public float ExceptionGradientStrength { set => SetValue((int)EffectImpl.PropertyIndex.ExceptionGradientStrength, value); }
        public float ExceptionGradientAngle { set => SetValue((int)EffectImpl.PropertyIndex.ExceptionGradientAngle, value); }
        public float ResidualColorCorrection { set => SetValue((int)EffectImpl.PropertyIndex.ResidualColorCorrection, value); }
        public Vector3 TargetResidualColor { set => SetValue((int)EffectImpl.PropertyIndex.TargetResidualColor, value); }
        public Vector3 CorrectedColor { set => SetValue((int)EffectImpl.PropertyIndex.CorrectedColor, value); }
        public float CorrectionTolerance { set => SetValue((int)EffectImpl.PropertyIndex.CorrectionTolerance, value); }
        public float TransparencyQuality { set => SetValue((int)EffectImpl.PropertyIndex.TransparencyQuality, value); }
        public float AlphaBlendAdjustment { set => SetValue((int)EffectImpl.PropertyIndex.AlphaBlendAdjustment, value); }
        public float ForegroundBrightness { set => SetValue((int)EffectImpl.PropertyIndex.ForegroundBrightness, value); }
        public float ForegroundContrast { set => SetValue((int)EffectImpl.PropertyIndex.ForegroundContrast, value); }
        public float ForegroundSaturation { set => SetValue((int)EffectImpl.PropertyIndex.ForegroundSaturation, value); }
        public float TranslucentDespill { set => SetValue((int)EffectImpl.PropertyIndex.TranslucentDespill, value); }

        internal void ClearInput() => SetInput(0, null, true);

        public ExtendedChromaKeyCustomEffect(IGraphicsDevicesAndContext devices)
            : base(Create<EffectImpl>(devices)) { }

        [CustomEffect(1)]
        private sealed class EffectImpl : D2D1CustomShaderEffectImplBase<EffectImpl>
        {
            private ConstantBuffer _cb;

            public EffectImpl() : base(ShaderResourceLoader.GetExtendedChromaKeyPS()) { }

            protected override void UpdateConstants() => drawInformation?.SetPixelShaderConstantBuffer(_cb);

            [CustomEffectProperty(PropertyType.Vector2, (int)PropertyIndex.ScreenSize)]
            public Vector2 ScreenSize { get => _cb.ScreenSize; set { _cb.ScreenSize = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Tolerance)]
            public float Tolerance { get => _cb.Tolerance; set { _cb.Tolerance = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.LuminanceMix)]
            public float LuminanceMix { get => _cb.LuminanceMix; set { _cb.LuminanceMix = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeSoftness)]
            public float EdgeSoftness { get => _cb.EdgeSoftness; set { _cb.EdgeSoftness = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ClipBlack)]
            public float ClipBlack { get => _cb.ClipBlack; set { _cb.ClipBlack = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ClipWhite)]
            public float ClipWhite { get => _cb.ClipWhite; set { _cb.ClipWhite = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeBlur)]
            public float EdgeBlur { get => _cb.EdgeBlur; set { _cb.EdgeBlur = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.GradientStrength)]
            public float GradientStrength { get => _cb.GradientStrength; set { _cb.GradientStrength = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.GradientAngle)]
            public float GradientAngle { get => _cb.GradientAngle; set { _cb.GradientAngle = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.SpillSuppression)]
            public float SpillSuppression { get => _cb.SpillSuppression; set { _cb.SpillSuppression = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeBalance)]
            public float EdgeBalance { get => _cb.EdgeBalance; set { _cb.EdgeBalance = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Despot)]
            public float Despot { get => _cb.Despot; set { _cb.Despot = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Erode)]
            public float Erode { get => _cb.Erode; set { _cb.Erode = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeDesaturation)]
            public float EdgeDesaturation { get => _cb.EdgeDesaturation; set { _cb.EdgeDesaturation = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.KeyCleanup)]
            public float KeyCleanup { get => _cb.KeyCleanup; set { _cb.KeyCleanup = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector4, (int)PropertyIndex.BaseColor)]
            public Vector4 BaseColor { get => _cb.BaseColor; set { _cb.BaseColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector3, (int)PropertyIndex.EndColor)]
            public Vector3 EndColor { get => _cb.EndColor; set { _cb.EndColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector4, (int)PropertyIndex.ReplaceColor)]
            public Vector4 ReplaceColor { get => _cb.ReplaceColor; set { _cb.ReplaceColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.ColorSpace)]
            public int ColorSpace { get => _cb.ColorSpace; set { _cb.ColorSpace = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.MainKeyColor)]
            public int MainKeyColor { get => _cb.MainKeyColor; set { _cb.MainKeyColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.IsInverted)]
            public int IsInverted { get => _cb.IsInverted; set { _cb.IsInverted = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.IsCompleteKey)]
            public int IsCompleteKey { get => _cb.IsCompleteKey; set { _cb.IsCompleteKey = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.HueRange)]
            public float HueRange { get => _cb.HueRange; set { _cb.HueRange = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.SaturationThreshold)]
            public float SaturationThreshold { get => _cb.SaturationThreshold; set { _cb.SaturationThreshold = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.QualityPreset)]
            public int QualityPreset { get => _cb.QualityPreset; set { _cb.QualityPreset = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.LuminanceRange)]
            public float LuminanceRange { get => _cb.LuminanceRange; set { _cb.LuminanceRange = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.EdgeDetection)]
            public float EdgeDetection { get => _cb.EdgeDetection; set { _cb.EdgeDetection = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Denoise)]
            public float Denoise { get => _cb.Denoise; set { _cb.Denoise = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.Feathering)]
            public float Feathering { get => _cb.Feathering; set { _cb.Feathering = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ReplaceIntensity)]
            public float ReplaceIntensity { get => _cb.ReplaceIntensity; set { _cb.ReplaceIntensity = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.PreserveLuminance)]
            public float PreserveLuminance { get => _cb.PreserveLuminance; set { _cb.PreserveLuminance = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Int32, (int)PropertyIndex.DebugMode)]
            public int DebugMode { get => _cb.DebugMode; set { _cb.DebugMode = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector4, (int)PropertyIndex.ExceptionColor1)]
            public Vector4 ExceptionColor1 { get => _cb.ExceptionColor1; set { _cb.ExceptionColor1 = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector3, (int)PropertyIndex.ExceptionColor2)]
            public Vector3 ExceptionColor2 { get => _cb.ExceptionColor2; set { _cb.ExceptionColor2 = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ExceptionTolerance)]
            public float ExceptionTolerance { get => _cb.ExceptionTolerance; set { _cb.ExceptionTolerance = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ExceptionGradientStrength)]
            public float ExceptionGradientStrength { get => _cb.ExceptionGradientStrength; set { _cb.ExceptionGradientStrength = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ExceptionGradientAngle)]
            public float ExceptionGradientAngle { get => _cb.ExceptionGradientAngle; set { _cb.ExceptionGradientAngle = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ResidualColorCorrection)]
            public float ResidualColorCorrection { get => _cb.ResidualColorCorrection; set { _cb.ResidualColorCorrection = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector3, (int)PropertyIndex.TargetResidualColor)]
            public Vector3 TargetResidualColor { get => _cb.TargetResidualColor; set { _cb.TargetResidualColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Vector3, (int)PropertyIndex.CorrectedColor)]
            public Vector3 CorrectedColor { get => _cb.CorrectedColor; set { _cb.CorrectedColor = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.CorrectionTolerance)]
            public float CorrectionTolerance { get => _cb.CorrectionTolerance; set { _cb.CorrectionTolerance = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.TransparencyQuality)]
            public float TransparencyQuality { get => _cb.TransparencyQuality; set { _cb.TransparencyQuality = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.AlphaBlendAdjustment)]
            public float AlphaBlendAdjustment { get => _cb.AlphaBlendAdjustment; set { _cb.AlphaBlendAdjustment = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ForegroundBrightness)]
            public float ForegroundBrightness { get => _cb.ForegroundBrightness; set { _cb.ForegroundBrightness = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ForegroundContrast)]
            public float ForegroundContrast { get => _cb.ForegroundContrast; set { _cb.ForegroundContrast = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.ForegroundSaturation)]
            public float ForegroundSaturation { get => _cb.ForegroundSaturation; set { _cb.ForegroundSaturation = value; UpdateConstants(); } }

            [CustomEffectProperty(PropertyType.Float, (int)PropertyIndex.TranslucentDespill)]
            public float TranslucentDespill { get => _cb.TranslucentDespill; set { _cb.TranslucentDespill = value; UpdateConstants(); } }

            [StructLayout(LayoutKind.Sequential)]
            private struct ConstantBuffer
            {
                public Vector2 ScreenSize;
                public float Tolerance;
                public float LuminanceMix;

                public float EdgeSoftness;
                public float ClipBlack;
                public float ClipWhite;
                public float EdgeBlur;

                public float GradientStrength;
                public float GradientAngle;
                public float SpillSuppression;
                public float EdgeBalance;

                public float Despot;
                public float Erode;
                public float EdgeDesaturation;
                public float KeyCleanup;

                public Vector4 BaseColor;

                public Vector3 EndColor;
                private float _padding0;

                public Vector4 ReplaceColor;

                public int ColorSpace;
                public int MainKeyColor;
                public int IsInverted;
                public int IsCompleteKey;

                public float HueRange;
                public float SaturationThreshold;
                public int QualityPreset;
                public float LuminanceRange;

                public float EdgeDetection;
                public float Denoise;
                public float Feathering;
                public float ReplaceIntensity;

                public float PreserveLuminance;
                public int DebugMode;
                private float _padding1;
                private float _padding2;

                public Vector4 ExceptionColor1;

                public Vector3 ExceptionColor2;
                public float ExceptionTolerance;

                public float ExceptionGradientStrength;
                public float ExceptionGradientAngle;
                private float _padding3;
                private float _padding4;

                public float ResidualColorCorrection;
                public float CorrectionTolerance;
                public float TransparencyQuality;
                public float AlphaBlendAdjustment;

                public Vector3 TargetResidualColor;
                private float _padding5;

                public Vector3 CorrectedColor;
                public float TranslucentDespill;

                public float ForegroundBrightness;
                public float ForegroundContrast;
                public float ForegroundSaturation;
                private float _padding7;
            }

            internal enum PropertyIndex
            {
                ScreenSize,
                Tolerance,
                LuminanceMix,
                EdgeSoftness,
                ClipBlack,
                ClipWhite,
                EdgeBlur,
                GradientStrength,
                GradientAngle,
                SpillSuppression,
                EdgeBalance,
                Despot,
                Erode,
                EdgeDesaturation,
                KeyCleanup,
                BaseColor,
                EndColor,
                ReplaceColor,
                ColorSpace,
                MainKeyColor,
                IsInverted,
                IsCompleteKey,
                HueRange,
                SaturationThreshold,
                QualityPreset,
                LuminanceRange,
                EdgeDetection,
                Denoise,
                Feathering,
                ReplaceIntensity,
                PreserveLuminance,
                DebugMode,
                ExceptionColor1,
                ExceptionColor2,
                ExceptionTolerance,
                ExceptionGradientStrength,
                ExceptionGradientAngle,
                ResidualColorCorrection,
                TargetResidualColor,
                CorrectedColor,
                CorrectionTolerance,
                TransparencyQuality,
                AlphaBlendAdjustment,
                ForegroundBrightness,
                ForegroundContrast,
                ForegroundSaturation,
                TranslucentDespill,
            }
        }
    }
}