using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;
using Kontrol.Services;
using System.Globalization;

namespace Kontrol.ViewModels;

public record LanguageOption(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    [ObservableProperty] private int _pollingIntervalMs;
    [ObservableProperty] private bool _startMinimized;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _launchOnWindowsStartup;
    [ObservableProperty] private bool _tempAlertsEnabled;
    [ObservableProperty] private float _tempAlertThresholdC;
    [ObservableProperty] private string _selectedTheme;
    [ObservableProperty] private bool _openRgbEnabled;
    [ObservableProperty] private string _openRgbHost;
    [ObservableProperty] private int _openRgbPort;
    [ObservableProperty] private string _openRgbClientName;
    [ObservableProperty] private LanguageOption _selectedLanguage;
    [ObservableProperty] private string _saveStatusText = string.Empty;

    public static IReadOnlyList<string> ThemeOptions { get; } = new[] { "Dark", "Light", "System" };

    public static IReadOnlyList<LanguageOption> LanguageOptions { get; } = new[]
    {
        new LanguageOption("en", "English"),
        new LanguageOption("tr", "Türkçe"),
        new LanguageOption("de", "Deutsch"),
        new LanguageOption("fr", "Français"),
        new LanguageOption("es", "Español"),
        new LanguageOption("pt", "Português"),
        new LanguageOption("it", "Italiano"),
        new LanguageOption("ru", "Русский"),
        new LanguageOption("zh-CN", "中文 (简体)"),
        new LanguageOption("ja", "日本語"),
        new LanguageOption("ko", "한국어"),
        new LanguageOption("ar", "العربية"),
        new LanguageOption("nl", "Nederlands"),
        new LanguageOption("pl", "Polski"),
        new LanguageOption("sv", "Svenska"),
    };

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        _pollingIntervalMs = settings.PollingIntervalMs;
        _startMinimized = settings.StartMinimized;
        _minimizeToTray = settings.MinimizeToTray;
        _launchOnWindowsStartup = settings.LaunchOnWindowsStartup;
        _tempAlertsEnabled = settings.TempAlertsEnabled;
        _tempAlertThresholdC = settings.TempAlertThresholdC;
        _selectedTheme = settings.Theme;
        _openRgbEnabled = settings.OpenRgbEnabled;
        _openRgbHost = settings.OpenRgbHost;
        _openRgbPort = settings.OpenRgbPort;
        _openRgbClientName = settings.OpenRgbClientName;
        _selectedLanguage = LanguageOptions.FirstOrDefault(l => l.Code == settings.Language)
                            ?? LanguageOptions[0];
    }

    [RelayCommand]
    private void Save()
    {
        _settings.PollingIntervalMs = PollingIntervalMs;
        _settings.StartMinimized = StartMinimized;
        _settings.MinimizeToTray = MinimizeToTray;
        _settings.LaunchOnWindowsStartup = LaunchOnWindowsStartup;
        _settings.TempAlertsEnabled = TempAlertsEnabled;
        _settings.TempAlertThresholdC = TempAlertThresholdC;
        _settings.Theme = SelectedTheme;
        _settings.OpenRgbEnabled = OpenRgbEnabled;
        _settings.OpenRgbHost = OpenRgbHost;
        _settings.OpenRgbPort = OpenRgbPort;
        _settings.OpenRgbClientName = OpenRgbClientName;
        _settings.Language = SelectedLanguage.Code;
        _settings.Save();
        Services.ThemeHelper.Apply(SelectedTheme);
        ApplyLanguage(SelectedLanguage.Code);
        SaveStatusText = Loc.Format("StatSaved", DateTime.Now.ToString("HH:mm:ss"));
    }

    private static void ApplyLanguage(string code)
    {
        try
        {
            var culture = new CultureInfo(code);
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
        catch { }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Services.ThemeHelper.Apply(value);
    }
}
