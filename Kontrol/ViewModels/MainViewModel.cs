using CommunityToolkit.Mvvm.ComponentModel;
using Kontrol.Models;
using Kontrol.Services;

namespace Kontrol.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public FanViewModel Fan { get; }
    public RgbViewModel Rgb { get; }
    public SettingsViewModel Settings { get; }
    public FanControlViewModel FanConfig { get; }

    public MainViewModel(AppSettings settings)
    {
        var hardwareService = App.HardwareServiceInstance ?? new HardwareService();
        var fanControlService = App.FanControlServiceInstance ?? new FanControlService();
        var fanController = App.FanControllerInstance;
        var rgbService = new RgbService();
        var rgbProfileService = new RgbProfileService();

        Fan = new FanViewModel(hardwareService, fanControlService, settings.PollingIntervalMs);
        Rgb = new RgbViewModel(rgbService, rgbProfileService, settings);
        Settings = new SettingsViewModel(settings);
        FanConfig = new FanControlViewModel(fanController!, hardwareService);
    }
}
