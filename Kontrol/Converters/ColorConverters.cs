using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Kontrol.Converters;

public class TemperatureToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not float temp)
            return new SolidColorBrush(Colors.Gray);

        return temp switch
        {
            < 50 => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            < 70 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),
            < 85 => new SolidColorBrush(Color.FromRgb(255, 87, 34)),
            _    => new SolidColorBrush(Color.FromRgb(244, 67, 54))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ColorToSolidBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Color c ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Black);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is SolidColorBrush b ? b.Color : Colors.Black;
}
