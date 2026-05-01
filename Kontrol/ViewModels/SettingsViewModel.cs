using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;

namespace Kontrol.ViewModels;

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
    [ObservableProperty] private string _saveStatusText = string.Empty;

    public static IReadOnlyList<string> ThemeOptions { get; } = new[] { "Dark", "Light", "System" };

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
        _settings.Save();
        Services.ThemeHelper.Apply(SelectedTheme);
        SaveStatusText = $"Kaydedildi {DateTime.Now:HH:mm:ss}";
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Services.ThemeHelper.Apply(value);
    }
}
