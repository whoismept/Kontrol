using Microsoft.UI.Xaml;

namespace Kontrol.Services;

public static class ThemeHelper
{
    public static void Apply(string theme)
    {
        var resolved = theme;
        if (resolved == "System")
            resolved = IsSystemDarkTheme() ? "Dark" : "Light";

        var element = App.Window?.Content as FrameworkElement;
        if (element is not null)
            element.RequestedTheme = resolved == "Light"
                ? ElementTheme.Light
                : ElementTheme.Dark;
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            return val is int i && i == 0;
        }
        catch { return true; }
    }
}
