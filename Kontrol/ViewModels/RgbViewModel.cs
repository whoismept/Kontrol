using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;
using Kontrol.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Kontrol.ViewModels;

public partial class RgbViewModel : ObservableObject, IDisposable
{
    private readonly RgbService _rgbService;
    private readonly RgbProfileService _profileService;
    private readonly AppSettings _settings;
    private bool _disposed;

    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private RgbDevice? _selectedDevice;
    [ObservableProperty] private Color _selectedColor = Colors.Red;
    [ObservableProperty] private string? _selectedProfile;
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private bool _hasDevices;
    [ObservableProperty] private string _initLogText = string.Empty;

    public string HexColorText
    {
        get => $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        set
        {
            try
            {
                var hex = value?.TrimStart('#') ?? "";
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex[..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);
                    SelectedColor = Color.FromRgb(r, g, b);
                    OnPropertyChanged();
                }
            }
            catch { }
        }
    }

    partial void OnSelectedColorChanged(Color value) => OnPropertyChanged(nameof(HexColorText));

    public ObservableCollection<RgbDevice> Devices { get; } = new();
    public ObservableCollection<string> Profiles { get; } = new();

    public RgbViewModel(RgbService rgbService, RgbProfileService profileService, AppSettings settings)
    {
        _rgbService = rgbService;
        _profileService = profileService;
        _settings = settings;

        Initialize();
    }

    private void Initialize()
    {
        StatusText = "Scanning RGB devices...";

        try
        {
            _rgbService.Initialize(_settings);
        }
        catch (Exception ex)
        {
            StatusText = $"Startup error: {ex.Message}";
        }

        BuildInitLogText();
        LoadDevices();
        ReloadProfiles();
    }

    private void BuildInitLogText()
    {
        var ok = _rgbService.InitLog.Where(l => l.StartsWith("[OK]")).ToList();
        var skip = _rgbService.InitLog.Where(l => l.StartsWith("[SKIP]")).ToList();

        var okNames = ok.Select(l => l.Replace("[OK] ", "")).ToList();
        var detail = okNames.Count > 0
            ? $"Active: {string.Join(", ", okNames)}"
            : "No provider is active";

        InitLogText = $"{detail} | {skip.Count} provider skipped";
    }

    [RelayCommand]
    private void LoadDevices()
    {
        Devices.Clear();
        try
        {
            foreach (var d in _rgbService.GetDevices())
                Devices.Add(d);
        }
        catch (Exception ex)
        {
            StatusText = $"Device scan error: {ex.Message}";
        }

        HasDevices = Devices.Count > 0;

        if (HasDevices)
        {
            SelectedDevice ??= Devices[0];
            int lampCount = Devices.Count(d => d.Backend == RgbBackend.LampArray);
            int sdkCount = Devices.Count(d => d.Backend == RgbBackend.RgbNet);
            StatusText = $"{Devices.Count} cihaz ({sdkCount} SDK · {lampCount} Native)";
        }
        else
        {
            StatusText = "No device found";
        }
    }

    private void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileService.ListProfiles())
            Profiles.Add(p);
    }

    [RelayCommand]
    private void ApplyColorToDevice()
    {
        if (SelectedDevice is null) return;
        _rgbService.SetDeviceColor(SelectedDevice, SelectedColor);
        StatusText = $"{SelectedDevice.Name} — color applied";
    }

    [RelayCommand]
    private void ApplyColorToAll()
    {
        if (Devices.Count == 0) return;
        _rgbService.SetAllDevicesColor(Devices, SelectedColor);
        StatusText = $"{Devices.Count} devices color applied";
    }

    [RelayCommand]
    private void LoadProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfile)) return;
        var profile = _profileService.Load(SelectedProfile);
        if (profile is null)
        {
            StatusText = "Profile could not be loaded";
            return;
        }

        foreach (var entry in profile.Devices)
        {
            var device = Devices.FirstOrDefault(d => d.Name == entry.DeviceName);
            if (device is not null)
                _rgbService.SetDeviceColor(device, entry.GetColor());
        }

        StatusText = $"Profile loaded: {profile.Name}";
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var name = NewProfileName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var profile = new RgbProfile
        {
            Name = name,
            Devices = Devices.Select(d => RgbProfileEntry.FromColor(d.Name, d.CurrentColor)).ToList()
        };

        _profileService.Save(profile);
        ReloadProfiles();
        NewProfileName = string.Empty;
        SelectedProfile = name;
        StatusText = $"Profile saved: {name}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rgbService.Dispose();
    }
}
