using Kontrol.Models.Fan;
using System.Globalization;
using System.Windows.Data;

namespace Kontrol.Converters;

public class TempCriticalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is float temp && temp >= 85;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FanModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is FanMode mode ? mode switch
        {
            FanMode.Auto => "Otomatik (BIOS)",
            FanMode.ManualConstant => "Sabit Hız",
            FanMode.Curve => "Eğri (Sıcaklığa Göre)",
            FanMode.Off => "Kapalı",
            _ => mode.ToString()
        } : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TempSourceModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TempSourceMode mode ? mode switch
        {
            TempSourceMode.Single => "Tek Sensör",
            TempSourceMode.Max => "Maksimum",
            TempSourceMode.Average => "Ortalama",
            TempSourceMode.Min => "Minimum",
            _ => mode.ToString()
        } : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullableFloatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f) return f.ToString("F0");
        return string.Empty;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && float.TryParse(s, out var f)) return (float?)f;
        return null;
    }
}
