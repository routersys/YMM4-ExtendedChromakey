using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;

namespace YMM4GradientChromaKey.Effect.Video.GradientChromaKey
{
    internal class GradientChromaKeyCustomEffect(IGraphicsDevicesAndContext devices) : D2D1CustomShaderEffectBase(Create<EffectImpl>(devices))
    {
        public Vector2 ScreenSize { set => SetValue((int)EffectImpl.Properties.ScreenSize, value); }
        public float Tolerance { set => SetValue((int)EffectImpl.Properties.Tolerance, value); }
        public float LuminanceMix { set => SetValue((int)EffectImpl.Properties.LuminanceMix, value); }
        public float EdgeSoftness { set => SetValue((int)EffectImpl.Properties.EdgeSoftness, value); }
        public float ClipBlack { set => SetValue((int)EffectImpl.Properties.ClipBlack, value); }
        public float ClipWhite { set => SetValue((int)EffectImpl.Properties.ClipWhite, value); }
        public float EdgeBlur { set => SetValue((int)EffectImpl.Properties.EdgeBlur, value); }
        public float GradientStrength { set => SetValue((int)EffectImpl.Properties.GradientStrength, value); }
        public float GradientAngle { set => SetValue((int)EffectImpl.Properties.GradientAngle, value); }
        public float SpillSuppression { set => SetValue((int)EffectImpl.Properties.SpillSuppression, value); }
        public float EdgeBalance { set => SetValue((int)EffectImpl.Properties.EdgeBalance, value); }
        public float Despot { set => SetValue((int)EffectImpl.Properties.Despot, value); }
        public float Erode { set => SetValue((int)EffectImpl.Properties.Erode, value); }
        public float EdgeDesaturation { set => SetValue((int)EffectImpl.Properties.EdgeDesaturation, value); }
        public float KeyCleanup { set => SetValue((int)EffectImpl.Properties.KeyCleanup, value); }
        public Vector4 BaseColor { set => SetValue((int)EffectImpl.Properties.BaseColor, value); }
        public Vector3 EndColor { set => SetValue((int)EffectImpl.Properties.EndColor, value); }
        public Vector4 ReplaceColor { set => SetValue((int)EffectImpl.Properties.ReplaceColor, value); }
        public int ColorSpace { set => SetValue((int)EffectImpl.Properties.ColorSpace, value); }
        public int MainKeyColor { set => SetValue((int)EffectImpl.Properties.MainKeyColor, value); }
        public int IsInverted { set => SetValue((int)EffectImpl.Properties.IsInverted, value); }
        public int IsCompleteKey { set => SetValue((int)EffectImpl.Properties.IsCompleteKey, value); }
        public float HueRange { set => SetValue((int)EffectImpl.Properties.HueRange, value); }
        public float SaturationThreshold { set => SetValue((int)EffectImpl.Properties.SaturationThreshold, value); }
        public int QualityPreset { set => SetValue((int)EffectImpl.Properties.QualityPreset, value); }
        public float LuminanceRange { set => SetValue((int)EffectImpl.Properties.LuminanceRange, value); }
        public float EdgeDetection { set => SetValue((int)EffectImpl.Properties.EdgeDetection, value); }
        public float Denoise { set => SetValue((int)EffectImpl.Properties.Denoise, value); }
        public float Feathering { set => SetValue((int)EffectImpl.Properties.Feathering, value); }
        public float ReplaceIntensity { set => SetValue((int)EffectImpl.Properties.ReplaceIntensity, value); }
        public float PreserveLuminance { set => SetValue((int)EffectImpl.Properties.PreserveLuminance, value); }
        public int DebugMode { set => SetValue((int)EffectImpl.Properties.DebugMode, value); }
        public Vector4 ExceptionColor1 { set => SetValue((int)EffectImpl.Properties.ExceptionColor1, value); }
        public Vector3 ExceptionColor2 { set => SetValue((int)EffectImpl.Properties.ExceptionColor2, value); }
        public float ExceptionTolerance { set => SetValue((int)EffectImpl.Properties.ExceptionTolerance, value); }
        public float ExceptionGradientStrength { set => SetValue((int)EffectImpl.Properties.ExceptionGradientStrength, value); }
        public float ExceptionGradientAngle { set => SetValue((int)EffectImpl.Properties.ExceptionGradientAngle, value); }
        public float ResidualColorCorrection { set => SetValue((int)EffectImpl.Properties.ResidualColorCorrection, value); }
        public Vector3 TargetResidualColor { set => SetValue((int)EffectImpl.Properties.TargetResidualColor, value); }
        public Vector3 CorrectedColor { set => SetValue((int)EffectImpl.Properties.CorrectedColor, value); }
        public float CorrectionTolerance { set => SetValue((int)EffectImpl.Properties.CorrectionTolerance, value); }
        public float TransparencyQuality { set => SetValue((int)EffectImpl.Properties.TransparencyQuality, value); }
        public float AlphaBlendAdjustment { set => SetValue((int)EffectImpl.Properties.AlphaBlendAdjustment, value); }

        [CustomEffect(1)]
        private class EffectImpl() : D2D1CustomShaderEffectImplBase<EffectImpl>(LoadShaderFromEmbeddedResource("GradientChromaKeyShader.cso"))
        {
            private ConstantBuffer constants;

            protected override void UpdateConstants() => drawInformation?.SetPixelShaderConstantBuffer(constants);

            private static byte[] LoadShaderFromEmbeddedResource(string resourceName)
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fullResourceName = $"YMM4GradientChromaKey.Shaders.{resourceName}";
                using var stream = assembly.GetManifestResourceStream(fullResourceName) ?? throw new FileNotFoundException($"Shader resource '{fullResourceName}' not found.");
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }

            [StructLayout(LayoutKind.Sequential)]
            struct ConstantBuffer
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
                private float _padding5;
                private float _padding6;
                private float _padding7;

                public Vector3 TargetResidualColor;
                public float CorrectionTolerance;

                public Vector3 CorrectedColor;
                public float TransparencyQuality;

                public float AlphaBlendAdjustment;
                private float _padding8;
                private float _padding9;
                private float _padding10;
            }

            public enum Properties
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
            }

            [CustomEffectProperty(PropertyType.Vector2, (int)Properties.ScreenSize)] public Vector2 ScreenSize { get => constants.ScreenSize; set { constants.ScreenSize = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Tolerance)] public float Tolerance { get => constants.Tolerance; set { constants.Tolerance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.LuminanceMix)] public float LuminanceMix { get => constants.LuminanceMix; set { constants.LuminanceMix = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.EdgeSoftness)] public float EdgeSoftness { get => constants.EdgeSoftness; set { constants.EdgeSoftness = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ClipBlack)] public float ClipBlack { get => constants.ClipBlack; set { constants.ClipBlack = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ClipWhite)] public float ClipWhite { get => constants.ClipWhite; set { constants.ClipWhite = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.EdgeBlur)] public float EdgeBlur { get => constants.EdgeBlur; set { constants.EdgeBlur = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.GradientStrength)] public float GradientStrength { get => constants.GradientStrength; set { constants.GradientStrength = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.GradientAngle)] public float GradientAngle { get => constants.GradientAngle; set { constants.GradientAngle = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.SpillSuppression)] public float SpillSuppression { get => constants.SpillSuppression; set { constants.SpillSuppression = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.EdgeBalance)] public float EdgeBalance { get => constants.EdgeBalance; set { constants.EdgeBalance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Despot)] public float Despot { get => constants.Despot; set { constants.Despot = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Erode)] public float Erode { get => constants.Erode; set { constants.Erode = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.EdgeDesaturation)] public float EdgeDesaturation { get => constants.EdgeDesaturation; set { constants.EdgeDesaturation = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.KeyCleanup)] public float KeyCleanup { get => constants.KeyCleanup; set { constants.KeyCleanup = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.BaseColor)] public Vector4 BaseColor { get => constants.BaseColor; set { constants.BaseColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector3, (int)Properties.EndColor)] public Vector3 EndColor { get => constants.EndColor; set { constants.EndColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.ReplaceColor)] public Vector4 ReplaceColor { get => constants.ReplaceColor; set { constants.ReplaceColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.ColorSpace)] public int ColorSpace { get => constants.ColorSpace; set { constants.ColorSpace = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.MainKeyColor)] public int MainKeyColor { get => constants.MainKeyColor; set { constants.MainKeyColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.IsInverted)] public int IsInverted { get => constants.IsInverted; set { constants.IsInverted = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.IsCompleteKey)] public int IsCompleteKey { get => constants.IsCompleteKey; set { constants.IsCompleteKey = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.HueRange)] public float HueRange { get => constants.HueRange; set { constants.HueRange = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.SaturationThreshold)] public float SaturationThreshold { get => constants.SaturationThreshold; set { constants.SaturationThreshold = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.QualityPreset)] public int QualityPreset { get => constants.QualityPreset; set { constants.QualityPreset = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.LuminanceRange)] public float LuminanceRange { get => constants.LuminanceRange; set { constants.LuminanceRange = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.EdgeDetection)] public float EdgeDetection { get => constants.EdgeDetection; set { constants.EdgeDetection = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Denoise)] public float Denoise { get => constants.Denoise; set { constants.Denoise = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.Feathering)] public float Feathering { get => constants.Feathering; set { constants.Feathering = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ReplaceIntensity)] public float ReplaceIntensity { get => constants.ReplaceIntensity; set { constants.ReplaceIntensity = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.PreserveLuminance)] public float PreserveLuminance { get => constants.PreserveLuminance; set { constants.PreserveLuminance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Int32, (int)Properties.DebugMode)] public int DebugMode { get => constants.DebugMode; set { constants.DebugMode = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector4, (int)Properties.ExceptionColor1)] public Vector4 ExceptionColor1 { get => constants.ExceptionColor1; set { constants.ExceptionColor1 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector3, (int)Properties.ExceptionColor2)] public Vector3 ExceptionColor2 { get => constants.ExceptionColor2; set { constants.ExceptionColor2 = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ExceptionTolerance)] public float ExceptionTolerance { get => constants.ExceptionTolerance; set { constants.ExceptionTolerance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ExceptionGradientStrength)] public float ExceptionGradientStrength { get => constants.ExceptionGradientStrength; set { constants.ExceptionGradientStrength = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ExceptionGradientAngle)] public float ExceptionGradientAngle { get => constants.ExceptionGradientAngle; set { constants.ExceptionGradientAngle = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.ResidualColorCorrection)] public float ResidualColorCorrection { get => constants.ResidualColorCorrection; set { constants.ResidualColorCorrection = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector3, (int)Properties.TargetResidualColor)] public Vector3 TargetResidualColor { get => constants.TargetResidualColor; set { constants.TargetResidualColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Vector3, (int)Properties.CorrectedColor)] public Vector3 CorrectedColor { get => constants.CorrectedColor; set { constants.CorrectedColor = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.CorrectionTolerance)] public float CorrectionTolerance { get => constants.CorrectionTolerance; set { constants.CorrectionTolerance = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.TransparencyQuality)] public float TransparencyQuality { get => constants.TransparencyQuality; set { constants.TransparencyQuality = value; UpdateConstants(); } }
            [CustomEffectProperty(PropertyType.Float, (int)Properties.AlphaBlendAdjustment)] public float AlphaBlendAdjustment { get => constants.AlphaBlendAdjustment; set { constants.AlphaBlendAdjustment = value; UpdateConstants(); } }
        }
    }
}