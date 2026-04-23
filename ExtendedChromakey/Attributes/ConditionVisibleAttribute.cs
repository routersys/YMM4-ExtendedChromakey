using System.Globalization;
using System.Windows;
using System.Windows.Data;
using YukkuriMovieMaker.ItemEditor;

namespace ExtendedChromaKey.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class ConditionVisibleAttribute(string propertyName) : Attribute, ICustomVisibilityAttribute2
    {
        public string PropertyName { get; } = propertyName;

        public Binding GetBinding(object item, object propertyOwner)
        {
            return new Binding(PropertyName)
            {
                Source = item,
                Converter = new ConditionVisibleConverter()
            };
        }
    }

    internal sealed class ConditionVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
