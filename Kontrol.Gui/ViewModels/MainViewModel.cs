using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Fan;
using Kontrol.Models;
using Kontrol.Rgb;

namespace Kontrol.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly FanProfileService _fanProfileService;

    public FanViewModel Fan { get; }
    public RgbViewModel Rgb { get; }
    public SettingsViewModel Settings { get; }
    public FanControlViewModel FanConfig { get; }

    /// <summary>Built-in preset profiles exposed for Dashboard quick-switch.</summary>
    public IReadOnlyList<FanProfile> FanPresets => FanProfileService.Presets;

    [ObservableProperty] private bool _isAdvancedMode;
    [ObservableProperty] private bool _hasGlobalAlert;
    [ObservableProperty] private string _globalAlertTitle = "";
    [ObservableProperty] private string _globalAlertMessage = "";

    public MainViewModel(AppSettings settings)
    {
        _settings          = settings;
        _isAdvancedMode    = settings.AdvancedMode;
        _fanProfileService = new FanProfileService();

        var hardwareService  = App.HardwareServiceInstance ?? new HardwareService();
        var fanControlService = App.FanControlServiceInstance ?? new FanControlService();
        var fanController    = App.FanControllerInstance;
        var rgbService       = new RgbService();
        var rgbProfileService = new RgbProfileService();

        Fan      = new FanViewModel(hardwareService, fanControlService, fanController!, settings.PollingIntervalMs);
        Rgb      = new RgbViewModel(rgbService, rgbProfileService, settings);
        Settings = new SettingsViewModel(settings, this);
        FanConfig = new FanControlViewModel(fanController!, hardwareService, _fanProfileService, settings);

        // Keep MainViewModel notified of active profile changes from FanControlViewModel
        FanConfig.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FanControlViewModel.ActiveProfile))
                OnPropertyChanged(nameof(ActiveFanProfile));
        };
    }

    /// <summary>Currently active fan profile (preset or custom). Null if in manual state.</summary>
    public FanProfile? ActiveFanProfile => FanConfig.ActiveProfile;

    /// <summary>Applies a fan profile from the Dashboard (quick-switch).</summary>
    [RelayCommand]
    public void ApplyFanPreset(FanProfile? profile)
    {
        if (profile is null) return;
        FanConfig.ApplyFanProfileCommand.Execute(profile);
    }

    partial void OnIsAdvancedModeChanged(bool value)
    {
        _settings.AdvancedMode = value;
        _settings.Save();
    }

    [RelayCommand]
    public void EnableAdvancedMode() => IsAdvancedMode = true;

    public void ShowGlobalAlert(string title, string message)
    {
        var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        if (dispatcher is not null)
        {
            dispatcher.TryEnqueue(() =>
            {
                GlobalAlertTitle = title;
                GlobalAlertMessage = message;
                HasGlobalAlert = true;
            });
        }
    }
}
