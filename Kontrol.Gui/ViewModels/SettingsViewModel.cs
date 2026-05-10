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
    [ObservableProperty] private string _selectedTheme = string.Empty;
    [ObservableProperty] private LanguageOption _selectedLanguage = null!;
    [ObservableProperty] private string _saveStatusText = string.Empty;

    public string ThresholdText => $"{TempAlertThresholdC:F0}°C";

    partial void OnTempAlertThresholdCChanged(float value) => OnPropertyChanged(nameof(ThresholdText));

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

    private readonly MainViewModel _mainVm;

    public bool IsAdvancedMode
    {
        get => _mainVm.IsAdvancedMode;
        set => _mainVm.IsAdvancedMode = value;
    }

    public SettingsViewModel(AppSettings settings, MainViewModel mainVm)
    {
        _settings = settings;
        _mainVm = mainVm;
        _pollingIntervalMs = settings.PollingIntervalMs;
        _startMinimized = settings.StartMinimized;
        _minimizeToTray = settings.MinimizeToTray;
        _launchOnWindowsStartup = settings.LaunchOnWindowsStartup;
        _tempAlertsEnabled = settings.TempAlertsEnabled;
        _tempAlertThresholdC = settings.TempAlertThresholdC;
        _selectedTheme = settings.Theme;
        _selectedLanguage = LanguageOptions.FirstOrDefault(l => l.Code == settings.Language)
                            ?? LanguageOptions[0];

        mainVm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsAdvancedMode))
                OnPropertyChanged(nameof(IsAdvancedMode));
        };
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
            Loc.Load(code);
        }
        catch { }
    }

    partial void OnSelectedThemeChanged(string value)
    {
        Services.ThemeHelper.Apply(value);
    }
}
