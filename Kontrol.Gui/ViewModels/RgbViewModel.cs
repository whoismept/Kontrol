using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;
using Kontrol.Rgb;
using Kontrol.Services;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.UI;

namespace Kontrol.ViewModels;

public partial class RgbViewModel : ObservableObject, IDisposable
{
    private readonly RgbService _rgbService;
    private readonly RgbProfileService _profileService;
    private readonly AppSettings _settings;
    private bool _disposed;

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private RgbDevice? _selectedDevice;
    [ObservableProperty] private RgbZone? _selectedZone;
    [ObservableProperty] private Color _selectedColor = Color.FromArgb(255, 255, 0, 0);
    [ObservableProperty] private string? _selectedProfile;
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private bool _hasDevices;
    [ObservableProperty] private bool _hasZones;
    [ObservableProperty] private string _initLogText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;

    /// <summary>LED count input for the selected device (used when device has no zones).</summary>
    [ObservableProperty] private int _deviceLedCountInput = 1;

    /// <summary>LED count input for the selected zone (used when device has zones).</summary>
    [ObservableProperty] private int _zoneLedCountInput = 1;

    public int SelectedColorR
    {
        get => SelectedColor.R;
        set => SelectedColor = Color.FromArgb(255, (byte)Math.Clamp(value, 0, 255), SelectedColor.G, SelectedColor.B);
    }

    public int SelectedColorG
    {
        get => SelectedColor.G;
        set => SelectedColor = Color.FromArgb(255, SelectedColor.R, (byte)Math.Clamp(value, 0, 255), SelectedColor.B);
    }

    public int SelectedColorB
    {
        get => SelectedColor.B;
        set => SelectedColor = Color.FromArgb(255, SelectedColor.R, SelectedColor.G, (byte)Math.Clamp(value, 0, 255));
    }

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
                    byte r = System.Convert.ToByte(hex[..2], 16);
                    byte g = System.Convert.ToByte(hex[2..4], 16);
                    byte b = System.Convert.ToByte(hex[4..6], 16);
                    SelectedColor = Color.FromArgb(255, r, g, b);
                    OnPropertyChanged();
                }
            }
            catch { }
        }
    }

    public string ColorRgbText => $"R:{SelectedColor.R}  G:{SelectedColor.G}  B:{SelectedColor.B}";
    public SolidColorBrush SelectedColorBrush => new(SelectedColor);

    partial void OnSelectedColorChanged(Color value)
    {
        OnPropertyChanged(nameof(SelectedColorR));
        OnPropertyChanged(nameof(SelectedColorG));
        OnPropertyChanged(nameof(SelectedColorB));
        OnPropertyChanged(nameof(HexColorText));
        OnPropertyChanged(nameof(ColorRgbText));
        OnPropertyChanged(nameof(SelectedColorBrush));
    }

    public ObservableCollection<RgbDevice> Devices { get; } = new();
    public ObservableCollection<string> Profiles { get; } = new();

    public RgbViewModel(RgbService rgbService, RgbProfileService profileService, AppSettings settings)
    {
        _rgbService     = rgbService;
        _profileService = profileService;
        _settings       = settings;

        _ = InitializeAsync();
    }

    partial void OnSelectedDeviceChanged(RgbDevice? value)
    {
        if (value is not null)
        {
            DeviceLedCountInput = value.EffectiveLedCount;
            HasZones   = value.Zones.Count > 0;
            SelectedZone = value.Zones.Count > 0 ? value.Zones[0] : null;
        }
        else
        {
            HasZones     = false;
            SelectedZone = null;
        }
    }

    partial void OnSelectedZoneChanged(RgbZone? value)
    {
        if (value is not null)
            ZoneLedCountInput = value.EffectiveLedCount;
    }

    private async Task InitializeAsync()
    {
        IsLoading  = true;
        StatusText = Loc.Get("StatRGBScanning");

        try
        {
            await _rgbService.InitializeAsync();
        }
        catch (Exception ex)
        {
            HasError      = true;
            ErrorMessage  = Loc.Format("StatRGBStartupError", ex.Message);
            StatusText    = ErrorMessage;
        }

        BuildInitLogText();
        await LoadDevicesAsync();
        ReloadProfiles();
        IsLoading = false;
    }

    private void BuildInitLogText()
    {
        var lines = _rgbService.InitLog.ToList();
        var ok    = lines.Any(l => l.StartsWith("[OK]"));
        InitLogText = ok ? string.Join("\n", lines) : Loc.Get("StatRGBNoDevice");
    }

    [RelayCommand]
    private async Task LoadDevicesAsync()
    {
        Devices.Clear();
        try
        {
            var devices = await _rgbService.GetDevicesAsync();
            foreach (var d in devices)
                Devices.Add(d);
        }
        catch (Exception ex)
        {
            StatusText = Loc.Format("StatRGBScanError", ex.Message);
        }

        HasDevices = Devices.Count > 0;

        if (HasDevices)
        {
            // Restore device-level LED count overrides
            foreach (var d in Devices)
            {
                if (_settings.RgbDeviceLedCounts.TryGetValue(d.Id, out var savedCount) && savedCount > 0)
                    d.UserLedCount = savedCount;

                // Restore per-zone LED count overrides
                foreach (var zone in d.Zones)
                {
                    var key = ZoneSettingsKey(d, zone);
                    if (_settings.RgbZoneLedCounts.TryGetValue(key, out var savedZoneCount) && savedZoneCount > 0)
                        zone.UserLedCount = savedZoneCount;
                }
            }

            SelectedDevice ??= Devices[0];
            StatusText = Loc.Format("StatRGBDevices", Devices.Count);
        }
        else
        {
            StatusText = Loc.Get("StatRGBNoDevice");
        }
    }

    [RelayCommand]
    private async Task ReconnectAsync()
    {
        IsLoading    = true;
        StatusText   = Loc.Get("StatRGBScanning");
        HasError     = false;
        ErrorMessage = string.Empty;

        try
        {
            await _rgbService.ReconnectAsync();
        }
        catch (Exception ex)
        {
            HasError     = true;
            ErrorMessage = Loc.Format("StatRGBStartupError", ex.Message);
        }

        BuildInitLogText();
        await LoadDevicesAsync();
        ReloadProfiles();
        IsLoading = false;
    }

    private void ReloadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileService.ListProfiles())
            Profiles.Add(p);
    }

    // -------------------------------------------------------------------------
    // Device-level LED count (when device has no zones)
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void SaveDeviceLedCount()
    {
        if (SelectedDevice is null) return;

        int count = Math.Clamp(DeviceLedCountInput, 1, 1000);
        SelectedDevice.UserLedCount = count;
        _settings.RgbDeviceLedCounts[SelectedDevice.Id] = count;
        _settings.Save();

        _rgbService.SetDeviceColor(SelectedDevice, SelectedColor);
        StatusText = Loc.Format("StatRGBLedCountSaved", count);
    }

    [RelayCommand]
    private void ResetDeviceLedCount()
    {
        if (SelectedDevice is null) return;

        SelectedDevice.UserLedCount = 0;
        _settings.RgbDeviceLedCounts.Remove(SelectedDevice.Id);
        _settings.Save();
        DeviceLedCountInput = SelectedDevice.LedCount;
        StatusText = Loc.Get("StatRGBLedCountReset");
    }

    // -------------------------------------------------------------------------
    // Zone-level LED count (when device has zones)
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void SaveZoneLedCount()
    {
        if (SelectedDevice is null || SelectedZone is null) return;

        int count = Math.Clamp(ZoneLedCountInput, 1, 1000);
        SelectedZone.UserLedCount = count;
        var key = ZoneSettingsKey(SelectedDevice, SelectedZone);
        _settings.RgbZoneLedCounts[key] = count;
        _settings.Save();

        _rgbService.SetDeviceZoneColor(SelectedDevice, SelectedZone, SelectedColor);
        StatusText = Loc.Format("StatRGBLedCountSaved", count);
    }

    [RelayCommand]
    private void ResetZoneLedCount()
    {
        if (SelectedDevice is null || SelectedZone is null) return;

        SelectedZone.UserLedCount = 0;
        var key = ZoneSettingsKey(SelectedDevice, SelectedZone);
        _settings.RgbZoneLedCounts.Remove(key);
        _settings.Save();
        ZoneLedCountInput = SelectedZone.DefaultLedCount;
        StatusText = Loc.Get("StatRGBLedCountReset");
    }

    // -------------------------------------------------------------------------
    // Color application
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void ApplyColorToZone()
    {
        if (SelectedDevice is null || SelectedZone is null) return;
        _rgbService.SetDeviceZoneColor(SelectedDevice, SelectedZone, SelectedColor);
        StatusText = Loc.Format("StatRGBZoneColorApplied", SelectedZone.Name);
    }

    [RelayCommand]
    private void ApplyColorToDevice()
    {
        if (SelectedDevice is null) return;
        _rgbService.SetDeviceColor(SelectedDevice, SelectedColor);
        StatusText = Loc.Format("StatRGBColorApplied", SelectedDevice.Name);
    }

    [RelayCommand]
    private void ApplyColorToAll()
    {
        if (Devices.Count == 0) return;
        _rgbService.SetAllDevicesColor(Devices, SelectedColor);
        StatusText = Loc.Format("StatRGBAllApplied", Devices.Count);
    }

    // -------------------------------------------------------------------------
    // Profile management
    // -------------------------------------------------------------------------

    [RelayCommand]
    private void LoadProfile()
    {
        if (string.IsNullOrWhiteSpace(SelectedProfile)) return;
        var profile = _profileService.Load(SelectedProfile);
        if (profile is null)
        {
            StatusText = Loc.Get("StatRGBProfileError");
            return;
        }

        foreach (var entry in profile.Devices)
        {
            var device = Devices.FirstOrDefault(d => d.Name == entry.DeviceName);
            if (device is not null)
                _rgbService.SetDeviceColor(device, entry.GetColor());
        }

        StatusText = Loc.Format("StatRGBProfileLoaded", profile.Name);
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var name = NewProfileName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        var profile = new RgbProfile
        {
            Name    = name,
            Devices = Devices.Select(d => RgbProfileEntry.FromColor(d.Name, d.CurrentColor)).ToList()
        };

        _profileService.Save(profile);
        ReloadProfiles();
        NewProfileName  = string.Empty;
        SelectedProfile = name;
        StatusText = Loc.Format("StatRGBProfileSaved", name);
    }

    // -------------------------------------------------------------------------
    // Quick actions (called from Dashboard without changing ViewModel state)
    // -------------------------------------------------------------------------

    public void QuickSetDeviceColor(RgbDevice device, Windows.UI.Color color)
    {
        _rgbService.SetDeviceColor(device, color);
        StatusText = Loc.Format("StatRGBColorApplied", device.Name);
    }

    public void QuickLoadProfile(string profileName)
    {
        var profile = _profileService.Load(profileName);
        if (profile is null) return;
        foreach (var entry in profile.Devices)
        {
            var device = Devices.FirstOrDefault(d => d.Name == entry.DeviceName);
            if (device is not null)
                _rgbService.SetDeviceColor(device, entry.GetColor());
        }
        StatusText = Loc.Format("StatRGBProfileLoaded", profile.Name);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string ZoneSettingsKey(RgbDevice device, RgbZone zone)
        => $"{device.Id}:ch{zone.Channel}";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rgbService.Dispose();
    }
}
