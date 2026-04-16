using ExtendedChromaKey.Effect;
using ExtendedChromaKey.Models;
using ExtendedChromaKey.Services;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExtendedChromaKey.ViewModels
{
    internal sealed class ValidationPanelViewModel : INotifyPropertyChanged, IDisposable
    {
        private static readonly ValidationLevel[] LevelPriority =
            [ValidationLevel.Error, ValidationLevel.Warning, ValidationLevel.Performance, ValidationLevel.Info];

        private ExtendedChromaKeyEffect? _effect;
        private int _disposed;

        private string _displayLevel = string.Empty;
        private string _primaryMessage = string.Empty;
        private string _tooltipText = string.Empty;
        private int _additionalCount;
        private bool _hasMessages;
        private string? _updateMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string DisplayLevel { get => _displayLevel; private set => Set(ref _displayLevel, value); }
        public string PrimaryMessage { get => _primaryMessage; private set => Set(ref _primaryMessage, value); }
        public string TooltipText { get => _tooltipText; private set => Set(ref _tooltipText, value); }
        public int AdditionalCount { get => _additionalCount; private set => Set(ref _additionalCount, value); }
        public bool HasMessages { get => _hasMessages; private set => Set(ref _hasMessages, value); }
        public bool HasAdditional => AdditionalCount > 0;
        public string? UpdateMessage { get => _updateMessage; private set => Set(ref _updateMessage, value); }

        public void AttachEffect(ExtendedChromaKeyEffect? effect)
        {
            if (ReferenceEquals(_effect, effect))
                return;

            if (_effect is not null)
                _effect.PropertyChanged -= OnEffectPropertyChanged;

            _effect = effect;

            if (_effect is not null)
                _effect.PropertyChanged += OnEffectPropertyChanged;

            Evaluate();
        }

        public void DetachEffect()
        {
            if (_effect is not null)
            {
                _effect.PropertyChanged -= OnEffectPropertyChanged;
                _effect = null;
            }
        }

        public void SetUpdateMessage(string? message)
        {
            UpdateMessage = message;
            Evaluate();
        }

        public void Evaluate()
        {
            if (!string.IsNullOrEmpty(_updateMessage))
            {
                ApplyResult(ValidationLevel.Update.ToString(), [_updateMessage]);
                return;
            }

            if (_effect is null)
            {
                ClearResult();
                return;
            }

            var rules = ServiceRegistry.Instance.ValidationRuleProvider.Rules;

            foreach (var level in LevelPriority)
            {
                var messages = ImmutableArray.CreateBuilder<string>();
                foreach (var rule in rules)
                {
                    if (rule.Level != level) continue;
                    try
                    {
                        if (rule.Check(_effect))
                            messages.Add(rule.ErrorMessage);
                    }
                    catch
                    {
                    }
                }

                if (messages.Count > 0)
                {
                    ApplyResult(level.ToString(), messages.ToImmutable());
                    return;
                }
            }

            ClearResult();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1)
                return;
            DetachEffect();
        }

        private void ApplyResult(string level, IReadOnlyList<string> messages)
        {
            DisplayLevel = level;
            PrimaryMessage = messages[0];
            AdditionalCount = messages.Count - 1;
            TooltipText = messages.Count > 1
                ? string.Join("\n", messages.Select(static m => $"• {m}"))
                : messages[0];
            HasMessages = true;
            OnPropertyChanged(nameof(HasAdditional));
        }

        private void ClearResult()
        {
            HasMessages = false;
            DisplayLevel = string.Empty;
            PrimaryMessage = string.Empty;
            AdditionalCount = 0;
            TooltipText = string.Empty;
            OnPropertyChanged(nameof(HasAdditional));
        }

        private void OnEffectPropertyChanged(object? sender, PropertyChangedEventArgs e) => Evaluate();

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}
