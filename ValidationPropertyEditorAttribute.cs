using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YMM4GradientChromaKey.Controls;

namespace YMM4GradientChromaKey.Effect.Video.GradientChromaKey
{
    internal class ValidationPanelEditorAttribute : PropertyEditorAttribute2
    {
        public PropertyEditorSize PropertyEditorSize { get; set; }

        public override FrameworkElement Create()
        {
            return new ParameterValidationPanel();
        }

        public override void SetBindings(FrameworkElement control, ItemProperty[] itemProperties)
        {
            var panel = (ParameterValidationPanel)control;

            if (itemProperties.Length > 0 && itemProperties[0].PropertyOwner != null)
            {
                var binding = new Binding
                {
                    Source = itemProperties[0].PropertyOwner,
                    Mode = BindingMode.OneWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                panel.SetBinding(ParameterValidationPanel.EffectProperty, binding);
            }
        }

        public override void ClearBindings(FrameworkElement control)
        {
            BindingOperations.ClearBinding(control, ParameterValidationPanel.EffectProperty);
        }
    }
}