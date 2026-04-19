using ExtendedChromaKey.Effect;
using ExtendedChromaKey.Services;
using ExtendedChromaKey.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using YukkuriMovieMaker.Commons;

namespace ExtendedChromaKey.Views
{
    public partial class ParameterValidationPanel : UserControl, IPropertyEditorControl
    {
        private readonly ValidationPanelViewModel _viewModel = new();
        private readonly DispatcherTimer _scrollDelayTimer;
        private Storyboard? _scrollStoryboard;

        public static new readonly DependencyProperty EffectProperty =
            DependencyProperty.Register(
                nameof(Effect),
                typeof(ExtendedChromaKeyEffect),
                typeof(ParameterValidationPanel),
                new PropertyMetadata(null, OnEffectChanged));

        public new ExtendedChromaKeyEffect? Effect
        {
            get => (ExtendedChromaKeyEffect?)GetValue(EffectProperty);
            set => SetValue(EffectProperty, value);
        }

        public event EventHandler? BeginEdit;
        public event EventHandler? EndEdit;

        public ParameterValidationPanel()
        {
            InitializeComponent();
            DataContext = _viewModel;

            _scrollDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1), IsEnabled = false };
            _scrollDelayTimer.Tick += OnScrollDelayTimerTick;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel.AttachEffect(Effect);

            try
            {
                var checker = ServiceRegistry.Instance.UpdateChecker;
                await checker.CheckAsync();
                _viewModel.SetUpdateMessage(checker.UpdateMessage);
            }
            catch
            {
            }

            _viewModel.Evaluate();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopScrollAnimation();
            _viewModel.DetachEffect();
        }

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ParameterValidationPanel panel) return;
            panel._viewModel.AttachEffect(e.NewValue as ExtendedChromaKeyEffect);
            if (e.NewValue is not null)
                panel._viewModel.Evaluate();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ValidationPanelViewModel.PrimaryMessage)) return;

            _scrollDelayTimer.Stop();
            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
            Canvas.SetLeft(MessageText, 0);

            if (_viewModel.HasMessages)
                _scrollDelayTimer.Start();
        }

        private void OnScrollDelayTimerTick(object? sender, EventArgs e)
        {
            _scrollDelayTimer.Stop();
            StartTextScrolling();
        }

        private void StartTextScrolling()
        {
            if (!_viewModel.HasMessages) return;

            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
            Canvas.SetLeft(MessageText, 0);

            MessageText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (MessageCanvas.ActualWidth <= 0 || MessageText.DesiredSize.Width <= MessageCanvas.ActualWidth)
                return;

            var scrollDistance = MessageText.DesiredSize.Width - MessageCanvas.ActualWidth + 20;
            var animation = new DoubleAnimation(0, -scrollDistance, TimeSpan.FromSeconds(Math.Max(3.0, scrollDistance / 40.0)))
            {
                BeginTime = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };

            _scrollStoryboard = new Storyboard();
            _scrollStoryboard.Children.Add(animation);
            Storyboard.SetTarget(animation, MessageText);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(Canvas.Left)"));
            _scrollStoryboard.Begin();
        }

        private void StopScrollAnimation()
        {
            _scrollDelayTimer.Stop();
            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
        }
    }
}
