using Kontrol.Fan;
using Kontrol.Rgb;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using Windows.UI;

namespace Kontrol.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public static readonly BoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool invert = parameter is string s && s == "invert";
        bool boolValue = value is bool b ? b : value is not null;
        if (invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool boolValue = value is bool b ? b : value is not null;
        return boolValue ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TemperatureToColorConverter : IValueConverter
{
    public static readonly TemperatureToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not float temp)
            return new SolidColorBrush(Colors.Gray);

        return temp switch
        {
            < 50 => new SolidColorBrush(Color.FromArgb(255, 76, 175, 80)),
            < 70 => new SolidColorBrush(Color.FromArgb(255, 255, 193, 7)),
            < 85 => new SolidColorBrush(Color.FromArgb(255, 255, 87, 34)),
            _    => new SolidColorBrush(Color.FromArgb(255, 244, 67, 54))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class ColorToSolidBrushConverter : IValueConverter
{
    public static readonly ColorToSolidBrushConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
        => value is Color c ? new SolidColorBrush(c) : new SolidColorBrush(Colors.Black);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is SolidColorBrush b ? b.Color : Colors.Black;
}

public class TempCriticalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is float temp && temp >= 85;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class FanModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not FanMode mode || parameter is not string target) return Visibility.Collapsed;
        return mode.ToString() == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class FanModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not FanMode mode) return string.Empty;
        var key = mode switch
        {
            FanMode.Auto => "FanModeAuto",
            FanMode.ManualConstant => "FanModeFixed",
            FanMode.Curve => "FanModeCurve",
            FanMode.Off => "FanModeOff",
            _ => null
        };
        if (key is null) return mode.ToString();
        return Application.Current?.Resources?.TryGetValue(key, out var val) == true && val is string s
            ? s : mode.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TempSourceModeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not TempSourceMode mode) return string.Empty;
        var key = mode switch
        {
            TempSourceMode.Single => "TempModeSingle",
            TempSourceMode.Max => "TempModeMax",
            TempSourceMode.Average => "TempModeAverage",
            TempSourceMode.Min => "TempModeMin",
            _ => null
        };
        if (key is null) return mode.ToString();
        return Application.Current?.Resources?.TryGetValue(key, out var val) == true && val is string s
            ? s : mode.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class NullableFloatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float f) return f.ToString("F0");
        return string.Empty;
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is string s && float.TryParse(s, out var f)) return (float?)f;
        return null;
    }
}

public class HiddenOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is bool b && b ? 0.45 : 1.0;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class HideToggleTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var key = value is bool b && b ? "TipShowFan" : "TipHideFan";
        return Application.Current?.Resources?.TryGetValue(key, out var val) == true && val is string s
            ? s : (value is bool bv && bv ? "Show" : "Hide");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class TempDisplayConverter : IValueConverter
{
    public static readonly TempDisplayConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float f && f > 0) return $"{f:F0}°";
        return "—";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class RgbBackendLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is RgbBackend backend ? backend switch
        {
            RgbBackend.LampArray => "Native",
            RgbBackend.RgbNet => "SDK",
            _ => backend.ToString()
        } : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class FanModeTempLabelConverter : IValueConverter
{
    public static readonly FanModeTempLabelConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not FanMode mode) return "—";
        var key = mode == FanMode.Curve ? "LblTempSource" : "LblMaxTemp";
        return Application.Current?.Resources?.TryGetValue(key, out var val) == true && val is string s
            ? s : (mode == FanMode.Curve ? "Src °C" : "Max °C");
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class CurveDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not FanCurve curve) return string.Empty;
        var fmt = Application.Current?.Resources?.TryGetValue("FmtCurveItem", out var val) == true && val is string s
            ? s : "{0} pts · {1}";
        try { return string.Format(fmt, curve.Points.Count, curve.Type); }
        catch { return $"{curve.Points.Count} pts · {curve.Type}"; }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class FloatRoundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is float f ? $"{f:F0}" : "0";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => float.TryParse(value?.ToString(), out var f) ? f : 0f;
}

public class FloatPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is float f ? $"{f:F0}%" : "0%";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class FloatDegreeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is float f ? $"{f:F1}°C" : "0°C";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class IntMsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is int i ? $"{i}ms" : "0ms";

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
