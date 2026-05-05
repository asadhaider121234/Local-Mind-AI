using System;
using System.Globalization;
using System.Windows.Data;

namespace DocMind.ViewModels
{
    /// <summary>
    /// Converts bool → inverted bool. Used as x:Static singleton in XAML bindings.
    /// e.g. IsEnabled="{Binding IsRecommendedKeep, Converter={x:Static vm:InverseBoolConverter.Instance}}"
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public static readonly InverseBoolConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }
}
