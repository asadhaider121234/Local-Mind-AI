using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DocMind.Views;

public class SelectedToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return new SolidColorBrush(Colors.White); // active text is always white
        }
        return System.Windows.Application.Current.FindResource("TextPrimaryBrush");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
