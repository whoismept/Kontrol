namespace Kontrol.Services;

public static class ThemeHelper
{
    public static void Apply(string theme)
    {
        var resolved = theme;
        if (resolved == "System")
            resolved = IsSystemDarkTheme() ? "Dark" : "Light";

        var appTheme = resolved == "Light"
            ? Wpf.Ui.Appearance.ApplicationTheme.Light
            : Wpf.Ui.Appearance.ApplicationTheme.Dark;

        Wpf.Ui.Appearance.ApplicationThemeManager.Apply(appTheme);
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
