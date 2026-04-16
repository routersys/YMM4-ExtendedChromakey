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
        private readonly DispatcherTimer _debounceTimer;
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

            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250), IsEnabled = false };
            _debounceTimer.Tick += OnDebounceTimerTick;

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
            StopAllTimersAndAnimations();
            _viewModel.DetachEffect();
        }

        private static void OnEffectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ParameterValidationPanel panel) return;

            panel._viewModel.AttachEffect(e.NewValue as ExtendedChromaKeyEffect);

            if (e.NewValue is null)
                panel.SyncViewHidden();
            else
                panel._viewModel.Evaluate();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ValidationPanelViewModel.HasMessages)
                or nameof(ValidationPanelViewModel.DisplayLevel)
                or nameof(ValidationPanelViewModel.PrimaryMessage)
                or nameof(ValidationPanelViewModel.AdditionalCount)
                or nameof(ValidationPanelViewModel.HasAdditional))
            {
                _debounceTimer.Stop();
                _debounceTimer.Start();
            }
        }

        private void OnDebounceTimerTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            SyncViewToViewModel();
        }

        private void OnScrollDelayTimerTick(object? sender, EventArgs e)
        {
            _scrollDelayTimer.Stop();
            StartTextScrolling();
        }

        private void SyncViewToViewModel()
        {
            if (!_viewModel.HasMessages)
            {
                SyncViewHidden();
                return;
            }

            MainValidationPanel.Tag = _viewModel.DisplayLevel;
            MessageText.Text = _viewModel.PrimaryMessage;

            if (_viewModel.HasAdditional)
            {
                CountText.Text = $"+{_viewModel.AdditionalCount}";
                CountBadge.Visibility = Visibility.Visible;
                MainValidationPanel.ToolTip = new ToolTip { Content = _viewModel.TooltipText };
            }
            else
            {
                CountBadge.Visibility = Visibility.Collapsed;
                MainValidationPanel.ToolTip = new ToolTip { Content = _viewModel.PrimaryMessage };
            }

            _scrollDelayTimer.Stop();
            _scrollDelayTimer.Start();
        }

        private void SyncViewHidden()
        {
            StopAllTimersAndAnimations();
            MainValidationPanel.Tag = null;
        }

        private void StopAllTimersAndAnimations()
        {
            _debounceTimer.Stop();
            _scrollDelayTimer.Stop();
            _scrollStoryboard?.Stop();
            _scrollStoryboard = null;
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
    }
}
