using ExtendedChromaKey.Services;
using System.Numerics;
using Vortice.Direct2D1;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Player.Video.Effects;

namespace ExtendedChromaKey.Effect
{
    internal sealed class ExtendedChromaKeyEffectProcessor : VideoEffectProcessorBase
    {
        private readonly ExtendedChromaKeyEffect _item;
        private readonly IGraphicsDevicesAndContext _devices;
        private ExtendedChromaKeyCustomEffect? _effect;
        private ID2D1Image? _inputImage;

        public ExtendedChromaKeyEffectProcessor(IGraphicsDevicesAndContext devices, ExtendedChromaKeyEffect item)
            : base(devices)
        {
            _item = item;
            _devices = devices;
        }

        public override DrawDescription Update(EffectDescription effectDescription)
        {
            if (_effect is null)
                return effectDescription.DrawDescription;

            var frame = effectDescription.ItemPosition.Frame;
            var length = effectDescription.ItemDuration.Frame;
            var fps = effectDescription.FPS;

            Vector2 renderSize = new(1920, 1080);
            if (_inputImage is ID2D1Bitmap bitmap)
                renderSize = new Vector2(bitmap.PixelSize.Width, bitmap.PixelSize.Height);

            _effect.ScreenSize = renderSize;

            _effect.BaseColor = ColorConverter.ToVector4(_item.BaseColor);
            _effect.EndColor = ColorConverter.ToVector3(_item.EndColor);
            _effect.ExceptionColor1 = ColorConverter.ToVector4(_item.ExceptionColor1);
            _effect.ExceptionColor2 = ColorConverter.ToVector3(_item.ExceptionColor2);
            _effect.ReplaceColor = ColorConverter.ToVector4(_item.ReplaceColor);
            _effect.TargetResidualColor = ColorConverter.ToVector3(_item.TargetResidualColor);
            _effect.CorrectedColor = ColorConverter.ToVector3(_item.CorrectedColor);

            _effect.MainKeyColor = (int)_item.MainKeyColor;
            _effect.Tolerance = (float)(_item.Tolerance.GetValue(frame, length, fps) / 100.0);
            _effect.LuminanceMix = (float)(_item.LuminanceMix.GetValue(frame, length, fps) / 100.0);
            _effect.EdgeSoftness = (float)(_item.EdgeSoftness.GetValue(frame, length, fps) / 100.0);
            _effect.ClipBlack = (float)(_item.ClipBlack.GetValue(frame, length, fps) / 100.0);
            _effect.ClipWhite = (float)(_item.ClipWhite.GetValue(frame, length, fps) / 100.0);
            _effect.EdgeBlur = (float)_item.EdgeBlur.GetValue(frame, length, fps);
            _effect.GradientStrength = (float)(_item.GradientStrength.GetValue(frame, length, fps) / 100.0);
            _effect.GradientAngle = (float)_item.GradientAngle.GetValue(frame, length, fps);
            _effect.SpillSuppression = (float)(_item.SpillSuppression.GetValue(frame, length, fps) / 100.0);
            _effect.EdgeBalance = (float)(_item.EdgeBalance.GetValue(frame, length, fps) / 100.0);
            _effect.Despot = (float)_item.Despot.GetValue(frame, length, fps);
            _effect.Erode = (float)_item.Erode.GetValue(frame, length, fps);
            _effect.EdgeDesaturation = (float)(_item.EdgeDesaturation.GetValue(frame, length, fps) / 100.0);
            _effect.KeyCleanup = (float)(_item.KeyCleanup.GetValue(frame, length, fps) / 50.0);
            _effect.HueRange = (float)(_item.HueRange.GetValue(frame, length, fps) / 100.0);
            _effect.SaturationThreshold = (float)(_item.SaturationThreshold.GetValue(frame, length, fps) / 100.0);
            _effect.LuminanceRange = (float)(_item.LuminanceRange.GetValue(frame, length, fps) / 100.0);
            _effect.EdgeDetection = (float)(_item.EdgeDetection.GetValue(frame, length, fps) / 100.0);
            _effect.Denoise = (float)(_item.Denoise.GetValue(frame, length, fps) / 100.0);
            _effect.Feathering = (float)(_item.Feathering.GetValue(frame, length, fps) / 100.0);
            _effect.ReplaceIntensity = (float)(_item.ReplaceIntensity.GetValue(frame, length, fps) / 100.0);
            _effect.PreserveLuminance = (float)(_item.PreserveLuminance.GetValue(frame, length, fps) / 100.0);
            _effect.ExceptionTolerance = (float)(_item.ExceptionTolerance.GetValue(frame, length, fps) / 100.0);
            _effect.ExceptionGradientStrength = (float)(_item.ExceptionGradientStrength.GetValue(frame, length, fps) / 100.0);
            _effect.ExceptionGradientAngle = (float)_item.ExceptionGradientAngle.GetValue(frame, length, fps);
            _effect.ResidualColorCorrection = (float)(_item.ResidualColorCorrection.GetValue(frame, length, fps) / 100.0);
            _effect.CorrectionTolerance = (float)(_item.CorrectionTolerance.GetValue(frame, length, fps) / 100.0);
            _effect.TransparencyQuality = (float)(_item.TransparencyQuality.GetValue(frame, length, fps) / 100.0);
            _effect.AlphaBlendAdjustment = (float)(_item.AlphaBlendAdjustment.GetValue(frame, length, fps) / 100.0);
            _effect.TranslucentDespill = (float)(_item.TranslucentDespill.GetValue(frame, length, fps) / 100.0);
            _effect.ForegroundBrightness = (float)(_item.ForegroundBrightness.GetValue(frame, length, fps) / 100.0);
            _effect.ForegroundContrast = (float)(_item.ForegroundContrast.GetValue(frame, length, fps) / 100.0);
            _effect.ForegroundSaturation = (float)(_item.ForegroundSaturation.GetValue(frame, length, fps) / 100.0);

            _effect.ColorSpace = (int)_item.ColorSpace;
            _effect.IsInverted = _item.IsInverted ? 1 : 0;
            _effect.IsCompleteKey = _item.IsCompleteKey ? 1 : 0;
            _effect.QualityPreset = (int)_item.QualityPreset;
            _effect.DebugMode = (int)_item.DebugMode;

            return effectDescription.DrawDescription;
        }

        protected override ID2D1Image? CreateEffect(IGraphicsDevicesAndContext devices)
        {
            _effect = ServiceRegistry.Instance.EffectPool.Rent(devices);
            if (!_effect.IsEnabled)
            {
                ServiceRegistry.Instance.EffectPool.Return(devices, _effect);
                _effect = null;
                return null;
            }
            var output = _effect.Output;
            disposer.Collect(output);
            return output;
        }

        protected override void setInput(ID2D1Image? input)
        {
            _inputImage = input;
            _effect?.SetInput(0, input, true);
        }

        protected override void ClearEffectChain()
        {
            _effect?.SetInput(0, null, true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _effect is not null)
            {
                ServiceRegistry.Instance.EffectPool.Return(_devices, _effect);
                _effect = null;
            }
            base.Dispose(disposing);
        }
    }
}
