using Kontrol.Models;
using Kontrol.Models.Fan;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Kontrol.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter is string s && s == "invert";
        bool boolValue = value is bool b && b;
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TemperatureToColorConverter : IValueConverter
{
    public static readonly TemperatureToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not float temp)
            return new SolidColorBrush(Colors.Gray);

        return temp switch
        {
            < 50 => new SolidColorBrush(Color.FromRgb(76, 175, 80)),   // Green
            < 70 => new SolidColorBrush(Color.FromRgb(255, 193, 7)),   // Amber
            < 85 => new SolidColorBrush(Color.FromRgb(255, 87, 34)),   // Deep Orange
            _    => new SolidColorBrush(Color.FromRgb(244, 67, 54))    // Red
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ColorToSolidBrushConverter : IValueConverter
{
    public static readonly ColorToSolidBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Color c ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Black);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is SolidColorBrush b ? b.Color : Colors.Black;
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TempCriticalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is float temp && temp >= 85;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FanModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not FanMode mode || parameter is not string target) return Visibility.Collapsed;
        return mode.ToString() == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FanModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is FanMode mode ? mode switch
        {
            FanMode.Auto => "Automatic (BIOS)",
            FanMode.ManualConstant => "Fixed Speed",
            FanMode.Curve => "Curve (Temperature-Based)",
            FanMode.Off => "Off",
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
            TempSourceMode.Single => "Single Sensor",
            TempSourceMode.Max => "Maximum",
            TempSourceMode.Average => "Average",
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

// Shows a card for a fan based on (IsHidden, ShowHiddenFans) pair.
// Visible when: not hidden, OR hidden but ShowHiddenFans=true.
public class HiddenFanVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool isHidden = values[0] is bool b && b;
        bool showHidden = values[1] is bool s && s;
        return (!isHidden || showHidden) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HiddenOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? 0.45 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HideToggleTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? "Show" : "Hide";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class RgbBackendLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is RgbBackend backend ? backend switch
        {
            RgbBackend.LampArray => "Native",
            RgbBackend.RgbNet => "SDK",
            _ => backend.ToString()
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
