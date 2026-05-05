using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DocMind.Views
{
    public class ScoreToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                if (score >= 0.85) return new SolidColorBrush(Color.FromRgb(34, 197, 94));  // Green
                if (score >= 0.70) return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // Orange/Yellow
                return new SolidColorBrush(Color.FromRgb(239, 68, 68));                    // Red
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
