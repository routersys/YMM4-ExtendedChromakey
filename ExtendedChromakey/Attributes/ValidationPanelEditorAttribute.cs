using ExtendedChromaKey.Views;
using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.Commons;

namespace ExtendedChromaKey.Attributes
{
    internal sealed class ValidationPanelEditorAttribute : PropertyEditorAttribute2
    {
        public override FrameworkElement Create()
        {
            return new ParameterValidationPanel();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            if (control is ParameterValidationPanel panel && itemProperties.Length > 0 && itemProperties[0].PropertyOwner is not null)
            {
                var binding = new Binding
                {
                    Source = itemProperties[0].PropertyOwner,
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                };
                panel.SetBinding(ParameterValidationPanel.EffectProperty, binding);
            }
        }

        public override void ClearBindings(FrameworkElement control)
        {
            if (control is ParameterValidationPanel panel)
                BindingOperations.ClearBinding(panel, ParameterValidationPanel.EffectProperty);
        }
    }
}
