using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;
using YMM4GradientChromaKey.Effect.Video.GradientChromaKey;

namespace YMM4GradientChromaKey.Controls
{
    public partial class ParameterValidationPanel : UserControl, INotifyPropertyChanged, IPropertyEditorControl
    {
        private GradientChromaKeyEffect? _effect;
        private readonly DispatcherTimer _validationTimer;
        private readonly DispatcherTimer _scrollDelayTimer;
        private readonly List<ValidationRule> _validationRules;
        private Storyboard? _scrollStoryboard;

        private static string? _updateMessage;
        private static bool _updateCheckCompleted = false;
        private static readonly HttpClient _httpClient = new();

        public static readonly DependencyProperty EffectProperty =
            DependencyProperty.Register(nameof(Effect), typeof(GradientChromaKeyEffect), typeof(ParameterValidationPanel),
                new PropertyMetadata(null, OnEffectChanged));

        public GradientChromaKeyEffect? Effect
        {
            get => (GradientChromaKeyEffect?)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        private bool _isPanelVisible;
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set => Set(ref _isPanelVisible, value);
        }

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public ParameterValidationPanel()
        {
            InitializeComponent();
            _validationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250), IsEnabled = false };
            _validationTimer.Tick += ValidationTimer_Tick;
            _scrollDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1), IsEnabled = false };
            _scrollDelayTimer.Tick += ScrollDelayTimer_Tick;
            _validationRules = InitializeValidationRules();
            Loaded += ParameterValidationPanel_Loaded;
            Unloaded += ParameterValidationPanel_Unloaded;

            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("YMM4-ExtendedChromakey", GetCurrentVersion()));
            }
        }

        private async void ParameterValidationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_effect != null)
            {
                _effect.PropertyChanged += Effect_PropertyChanged;
            }
            await CheckForUpdatesAsync();
            ValidateParameters();
        }

        private void ParameterValidationPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAllTimersAndAnimations();
            if (_effect != null)
            {
                _effect.PropertyChanged -= Effect_PropertyChanged;
            }
        }

        private void StopAllTimersAndAnimations()
        {
            _validationTimer.Stop();
            _scrollDelayTimer.Stop();
            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
        }

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ParameterValidationPanel panel) return;

            if (e.OldValue is GradientChromaKeyEffect oldEffect)
            {
                oldEffect.PropertyChanged -= panel.Effect_PropertyChanged;
            }

            panel._effect = e.NewValue as GradientChromaKeyEffect;

            if (panel._effect != null)
            {
                if (panel.IsLoaded)
                {
                    panel._effect.PropertyChanged += panel.Effect_PropertyChanged;
                }
                panel.ValidateParameters();
            }
            else
            {
                panel.HideValidationPanel();
            }
        }

        private static string GetCurrentVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.1";
        }

        private async Task CheckForUpdatesAsync()
        {
            if (_updateCheckCompleted) return;

            try
            {
                var response = await _httpClient.GetAsync("https://api.github.com/repos/routersys/YMM4-ExtendedChromakey/releases/latest");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("tag_name", out var tagNameElement))
                {
                    string latestVersionTag = tagNameElement.GetString() ?? "";
                    string latestVersionStr = latestVersionTag.StartsWith("v") ? latestVersionTag.Substring(1) : latestVersionTag;

                    if (Version.TryParse(latestVersionStr, out var latestVersion) &&
                        Version.TryParse(GetCurrentVersion(), out var currentVersion) &&
                        latestVersion > currentVersion)
                    {
                        _updateMessage = $"新しいバージョン v{latestVersionStr} が利用可能です。（現在: v{currentVersion}）";
                    }
                }
            }
            catch
            {
                // APIへのアクセス失敗時は何もしない
            }
            finally
            {
                _updateCheckCompleted = true;
            }
        }

        private void Effect_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            TriggerValidation();
        }

        private void TriggerValidation()
        {
            _validationTimer.Stop();
            _validationTimer.Start();
        }

        private void ValidationTimer_Tick(object? sender, EventArgs e)
        {
            _validationTimer.Stop();
            ValidateParameters();
        }

        private void ScrollDelayTimer_Tick(object? sender, EventArgs e)
        {
            _scrollDelayTimer.Stop();
            StartTextScrolling();
        }

        private List<ValidationRule> InitializeValidationRules()
        {
            return new List<ValidationRule>
            {
                new ValidationRule("Error_BaseColorTransparency", effect => effect.MainKeyColor == KeyColorType.Custom && effect.BaseColor.A == 0, "カスタムモードでは、基本色を不透明に設定してください。完全に透明な色はキーイングできません。", ValidationLevel.Error),
                new ValidationRule("Error_ClipBlackWhiteInverted", effect => GetAnimationValue(effect.ClipBlack) >= GetAnimationValue(effect.ClipWhite), "クリップ(黒)の値がクリップ(白)以上になっています。マスクが正常に生成されません。", ValidationLevel.Error),
                new ValidationRule("Warning_ToleranceExtremelyLow", effect => GetAnimationValue(effect.Tolerance) < 1, "許容値が極端に低いため、意図した範囲が透過されない可能性があります。", ValidationLevel.Warning),
                new ValidationRule("Warning_ToleranceExtremelyHigh", effect => GetAnimationValue(effect.Tolerance) > 85, "許容値が極端に高いため、前景の意図しない部分まで透過される可能性があります。", ValidationLevel.Warning),
                new ValidationRule("Warning_SoftnessWithoutTolerance", effect => GetAnimationValue(effect.EdgeSoftness) > 0 && GetAnimationValue(effect.Tolerance) < 1, "エッジの柔らかさを機能させるには、許容値を少し上げる必要があります。", ValidationLevel.Warning),
                new ValidationRule("Warning_HighLuminanceMix", effect => GetAnimationValue(effect.LuminanceMix) > 90, "輝度ミックスが高すぎると、色の情報が無視され、明るさだけでキーイングされます。", ValidationLevel.Warning),
                new ValidationRule("Warning_ClipAndSoftnessConflict", effect => (GetAnimationValue(effect.ClipBlack) > 50 || GetAnimationValue(effect.ClipWhite) < 50) && GetAnimationValue(effect.EdgeSoftness) > 50, "クリップ設定とエッジの柔らかさの両方が高く設定されています。意図しない硬い輪郭になる可能性があります。", ValidationLevel.Warning),
                new ValidationRule("Info_GradientStrengthZero", effect => GetAnimationValue(effect.GradientStrength) == 0 && (effect.EndColor != effect.BaseColor || GetAnimationValue(effect.GradientAngle) != 90), "グラデーション強度が0%のため、「終端色」と「グラデーション角度」は効果がありません。", ValidationLevel.Info),
                new ValidationRule("Info_SpillSuppressionWithCustom", effect => effect.MainKeyColor == KeyColorType.Custom && GetAnimationValue(effect.SpillSuppression) > 0, "スピル除去は、主要キーイング色が「緑」「青」「赤」の時に最も効果的です。", ValidationLevel.Info),
                new ValidationRule("Info_ReplaceIntensityZero", effect => GetAnimationValue(effect.ReplaceIntensity) == 0 && (effect.ReplaceColor != System.Windows.Media.Colors.Transparent || GetAnimationValue(effect.PreserveLuminance) > 0), "置換の強度が0%のため、「置換色」と「輝度保持」は効果がありません。", ValidationLevel.Info),
                new ValidationRule("Info_ReplaceColorTransparent", effect => GetAnimationValue(effect.ReplaceIntensity) > 0 && effect.ReplaceColor.A == 0, "置換色が透明です。これは除去した領域の不透明度を下げる効果になります。", ValidationLevel.Info),
                new ValidationRule("Info_ColorSpaceForCustomOnly", effect => effect.MainKeyColor != KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.Lab, "色空間の変更は、主要キーイング色が「カスタム」の時にのみ適用されます。", ValidationLevel.Info),
                new ValidationRule("Info_HueRangeForSpecificSpaces", effect => GetAnimationValue(effect.HueRange) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.HSV && effect.ColorSpace != AdvancedColorSpace.LCH, "色相範囲は、色空間が「HSV」または「LCH」の場合にのみ有効です。", ValidationLevel.Info),
                new ValidationRule("Info_SaturationThresholdForHsvOnly", effect => GetAnimationValue(effect.SaturationThreshold) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.HSV, "彩度閾値は、色空間が「HSV」の場合にのみ有効です。", ValidationLevel.Info),
                new ValidationRule("Info_LuminanceRangeForSpecificSpaces", effect => GetAnimationValue(effect.LuminanceRange) > 0 && effect.MainKeyColor == KeyColorType.Custom && effect.ColorSpace != AdvancedColorSpace.RGB && effect.ColorSpace != AdvancedColorSpace.YUV, "明度範囲は、色空間が「RGB」または「YUV」の場合にのみ有効です。", ValidationLevel.Info),
                new ValidationRule("Info_CompleteKeyBypassesQuality", effect => effect.IsCompleteKey && (GetAnimationValue(effect.SpillSuppression) > 0 || GetAnimationValue(effect.EdgeDesaturation) > 0 || GetAnimationValue(effect.PreserveLuminance) > 0), "完全クロマキーモードでは、スピル除去・エッジ彩度除去・輝度保持は無効になります。", ValidationLevel.Info),
                new ValidationRule("Warning_ExceptionColorTransparent", effect => effect.ExceptionColor1.A == 0 && GetAnimationValue(effect.ExceptionTolerance) > 0, "例外色が透明に設定されているため、「例外設定」は機能しません。", ValidationLevel.Warning),
                new ValidationRule("Info_ExceptionGradientStrengthZero", effect => effect.ExceptionColor1.A > 0 && GetAnimationValue(effect.ExceptionGradientStrength) == 0 && (effect.ExceptionColor1 != effect.ExceptionColor2 || GetAnimationValue(effect.ExceptionGradientAngle) != 90), "例外グラデーション強度が0%のため、「例外色2 (終端)」と「グラデーション角度」は効果がありません。", ValidationLevel.Info),
                new ValidationRule("Info_ResidualColorCorrectionZero", effect => GetAnimationValue(effect.ResidualColorCorrection) == 0 && (effect.TargetResidualColor != System.Windows.Media.Colors.Transparent || effect.CorrectedColor != System.Windows.Media.Colors.Transparent), "残存色補正の強度が0%のため、「補正対象色」と「補正後の色」は効果がありません。", ValidationLevel.Info),
                new ValidationRule("Warning_GradientStrengthHigh", effect => GetAnimationValue(effect.GradientStrength) > 90, "グラデーション強度が高すぎると、意図しない範囲まで透過される可能性があります。煙などの透明度が高い部分で問題が発生する場合は、「透明度品質」を上げてください。", ValidationLevel.Warning),
                new ValidationRule("Performance_HighQualitySettings", effect => effect.QualityPreset == QualityPreset.HighQuality && (GetAnimationValue(effect.EdgeBlur) > 20 || GetAnimationValue(effect.Despot) > 5), "高品質モードで重い処理が組み合わさっています。パフォーマンスに影響する可能性があります。", ValidationLevel.Performance)
            };
        }

        private static double GetAnimationValue(Animation animation) => animation.Values.Any() ? animation.Values[0].Value : animation.DefaultValue;

        private void ValidateParameters()
        {
            if (!string.IsNullOrEmpty(_updateMessage))
            {
                ShowValidationMessage(ValidationLevel.Update.ToString(), new List<string> { _updateMessage });
                return;
            }

            if (_effect == null || !IsLoaded)
            {
                HideValidationPanel();
                return;
            }

            var levelPriority = new[] { ValidationLevel.Error, ValidationLevel.Warning, ValidationLevel.Performance, ValidationLevel.Info };

            foreach (var level in levelPriority)
            {
                var messages = _validationRules
                    .Where(rule => rule.Level == level && rule.Check(_effect))
                    .Select(rule => rule.ErrorMessage)
                    .ToList();

                if (messages.Any())
                {
                    ShowValidationMessage(level.ToString(), messages);
                    return;
                }
            }

            HideValidationPanel();
        }

        private void ShowValidationMessage(string level, List<string> messages)
        {
            IsPanelVisible = true;
            MainValidationPanel.Tag = level;
            MessageText.Text = messages.First();
            if (messages.Count > 1)
            {
                CountText.Text = $"+{messages.Count - 1}";
                CountBadge.Visibility = Visibility.Visible;
                MainValidationPanel.ToolTip = new ToolTip { Content = string.Join("\n", messages.Select(m => $"• {m}")) };
            }
            else
            {
                CountBadge.Visibility = Visibility.Collapsed;
                MainValidationPanel.ToolTip = new ToolTip { Content = messages.First() };
            }
            _scrollDelayTimer.Stop();
            _scrollDelayTimer.Start();
        }

        private void HideValidationPanel()
        {
            IsPanelVisible = false;
            StopAllTimersAndAnimations();
        }

        private void StartTextScrolling()
        {
            if (!IsPanelVisible) return;

            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
            Canvas.SetLeft(MessageText, 0);

            MessageText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (MessageCanvas.ActualWidth > 0 && MessageText.DesiredSize.Width > MessageCanvas.ActualWidth)
            {
                var scrollDistance = MessageText.DesiredSize.Width - MessageCanvas.ActualWidth + 20;
                var animation = new DoubleAnimation(0, -scrollDistance, TimeSpan.FromSeconds(Math.Max(3.0, scrollDistance / 40.0)))
                {
                    BeginTime = TimeSpan.FromSeconds(1),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                _scrollStoryboard = new Storyboard();
                _scrollStoryboard.Children.Add(animation);
                Storyboard.SetTarget(animation, MessageText);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
                _scrollStoryboard.Begin();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public class ValidationRule
    {
        public string Name { get; }
        public Func<GradientChromaKeyEffect, bool> Check { get; }
        public string ErrorMessage { get; }
        public ValidationLevel Level { get; }
        public ValidationRule(string name, Func<GradientChromaKeyEffect, bool> check, string errorMessage, ValidationLevel level)
        {
            Name = name; Check = check; ErrorMessage = errorMessage; Level = level;
        }
    }

    public enum ValidationLevel { Update, Error, Warning, Performance, Info }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value is true) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (value is Visibility.Visible);
    }
}