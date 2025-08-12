using System;
using System.Numerics;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace YMM4GradientChromaKey.Effect.Video.GradientChromaKey
{
    internal class GradientChromaKeyEffectProcessor : VideoEffectProcessorBase
    {
        private readonly GradientChromaKeyEffect _item;
        private readonly IGraphicsDevicesAndContext _devices;
        private GradientChromaKeyCustomEffect? effect;
        private ID2D1Image? _inputImage;

        public GradientChromaKeyEffectProcessor(IGraphicsDevicesAndContext devices, GradientChromaKeyEffect item) : base(devices)
        {
            _item = item;
            _devices = devices;
        }

        public override DrawDescription Update(EffectDescription effectDescription)
        {
            if (IsPassThroughEffect || effect is null)
                return effectDescription.DrawDescription;

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            Vector2 renderSize = new Vector2(1920, 1080);
            if (_inputImage is ID2D1Bitmap bitmap)
            {
                renderSize = new Vector2(bitmap.PixelSize.Width, bitmap.PixelSize.Height);
            }

            effect.ScreenSize = renderSize;

            effect.BaseColor = new Vector4(
                _item.BaseColor.R / 255.0f,
                _item.BaseColor.G / 255.0f,
                _item.BaseColor.B / 255.0f,
                _item.BaseColor.A / 255.0f
            );

            effect.EndColor = new Vector3(
                _item.EndColor.R / 255.0f,
                _item.EndColor.G / 255.0f,
                _item.EndColor.B / 255.0f
            );

            effect.ReplaceColor = new Vector4(
                _item.ReplaceColor.R / 255.0f,
                _item.ReplaceColor.G / 255.0f,
                _item.ReplaceColor.B / 255.0f,
                _item.ReplaceColor.A / 255.0f
            );

            effect.ExceptionColor1 = new Vector4(
                _item.ExceptionColor1.R / 255.0f,
                _item.ExceptionColor1.G / 255.0f,
                _item.ExceptionColor1.B / 255.0f,
                _item.ExceptionColor1.A / 255.0f
            );

            effect.ExceptionColor2 = new Vector3(
                _item.ExceptionColor2.R / 255.0f,
                _item.ExceptionColor2.G / 255.0f,
                _item.ExceptionColor2.B / 255.0f
            );

            effect.TargetResidualColor = new Vector3(
                _item.TargetResidualColor.R / 255.0f,
                _item.TargetResidualColor.G / 255.0f,
                _item.TargetResidualColor.B / 255.0f
            );

            effect.CorrectedColor = new Vector3(
                _item.CorrectedColor.R / 255.0f,
                _item.CorrectedColor.G / 255.0f,
                _item.CorrectedColor.B / 255.0f
            );

            effect.MainKeyColor = (int)_item.MainKeyColor;
            effect.Tolerance = (float)(_item.Tolerance.GetValue(frame, length, fps) / 100.0);
            effect.LuminanceMix = (float)(_item.LuminanceMix.GetValue(frame, length, fps) / 100.0);
            effect.EdgeSoftness = (float)(_item.EdgeSoftness.GetValue(frame, length, fps) / 100.0);
            effect.ClipBlack = (float)(_item.ClipBlack.GetValue(frame, length, fps) / 100.0);
            effect.ClipWhite = (float)(_item.ClipWhite.GetValue(frame, length, fps) / 100.0);
            effect.EdgeBlur = (float)_item.EdgeBlur.GetValue(frame, length, fps);
            effect.GradientStrength = (float)(_item.GradientStrength.GetValue(frame, length, fps) / 100.0);
            effect.GradientAngle = (float)(_item.GradientAngle.GetValue(frame, length, fps) * Math.PI / 180.0);
            effect.SpillSuppression = (float)(_item.SpillSuppression.GetValue(frame, length, fps) / 100.0);
            effect.EdgeBalance = (float)(_item.EdgeBalance.GetValue(frame, length, fps) / 100.0);
            effect.Despot = (float)_item.Despot.GetValue(frame, length, fps);
            effect.Erode = (float)_item.Erode.GetValue(frame, length, fps);
            effect.EdgeDesaturation = (float)(_item.EdgeDesaturation.GetValue(frame, length, fps) / 100.0);
            effect.KeyCleanup = (float)_item.KeyCleanup.GetValue(frame, length, fps);
            effect.HueRange = (float)(_item.HueRange.GetValue(frame, length, fps) / 100.0);
            effect.SaturationThreshold = (float)(_item.SaturationThreshold.GetValue(frame, length, fps) / 100.0);
            effect.LuminanceRange = (float)(_item.LuminanceRange.GetValue(frame, length, fps) / 100.0);
            effect.EdgeDetection = (float)(_item.EdgeDetection.GetValue(frame, length, fps) / 100.0);
            effect.Denoise = (float)(_item.Denoise.GetValue(frame, length, fps) / 100.0);
            effect.Feathering = (float)(_item.Feathering.GetValue(frame, length, fps) / 100.0);
            effect.ReplaceIntensity = (float)(_item.ReplaceIntensity.GetValue(frame, length, fps) / 100.0);
            effect.PreserveLuminance = (float)(_item.PreserveLuminance.GetValue(frame, length, fps) / 100.0);
            effect.ExceptionTolerance = (float)(_item.ExceptionTolerance.GetValue(frame, length, fps) / 100.0);
            effect.ExceptionGradientStrength = (float)(_item.ExceptionGradientStrength.GetValue(frame, length, fps) / 100.0);
            effect.ExceptionGradientAngle = (float)(_item.ExceptionGradientAngle.GetValue(frame, length, fps) * Math.PI / 180.0);

            effect.ResidualColorCorrection = (float)(_item.ResidualColorCorrection.GetValue(frame, length, fps) / 100.0);
            effect.CorrectionTolerance = (float)(_item.CorrectionTolerance.GetValue(frame, length, fps) / 100.0);
            effect.TransparencyQuality = (float)(_item.TransparencyQuality.GetValue(frame, length, fps) / 100.0);
            effect.AlphaBlendAdjustment = (float)(_item.AlphaBlendAdjustment.GetValue(frame, length, fps) / 100.0);
            effect.TranslucentDespill = (float)(_item.TranslucentDespill.GetValue(frame, length, fps) / 100.0);

            effect.ForegroundBrightness = (float)(_item.ForegroundBrightness.GetValue(frame, length, fps) / 100.0);
            effect.ForegroundContrast = (float)(_item.ForegroundContrast.GetValue(frame, length, fps) / 100.0);
            effect.ForegroundSaturation = (float)(_item.ForegroundSaturation.GetValue(frame, length, fps) / 100.0);

            effect.ColorSpace = (int)_item.ColorSpace;
            effect.IsInverted = _item.IsInverted ? 1 : 0;
            effect.IsCompleteKey = _item.IsCompleteKey ? 1 : 0;
            effect.DebugMode = (int)_item.DebugMode;
            effect.QualityPreset = (int)_item.QualityPreset;

            return effectDescription.DrawDescription;
        }

        protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
        {
            effect = new GradientChromaKeyCustomEffect(devices);
            if (!effect.IsEnabled)
            {
                effect.Dispose();
                effect = null;
                return null;
            }
            disposer.Collect(effect);
            var output = effect.Output;
            disposer.Collect(output);
            return output;
        }

        protected override void setInput(ID2D1Image? input)
        {
            _inputImage = input;
            effect?.SetInput(0, input, true);
        }

        protected override void ClearEffectChain()
        {
            effect?.SetInput(0, null, true);
        }
    }
}