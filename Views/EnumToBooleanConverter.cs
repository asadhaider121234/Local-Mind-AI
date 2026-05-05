using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DocMind.Views
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString()?.Equals(parameter.ToString(), StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                var paramString = parameter.ToString();
                if (paramString != null)
                    return Enum.Parse(targetType, paramString);
            }
            return Binding.DoNothing;
        }

    }
}
