using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Rgb;
using Kontrol.Services;
using static Microsoft.UI.Colors;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.UI;

namespace Kontrol.ViewModels;

public partial class RgbViewModel : ObservableObject, IDisposable
{
    private readonly RgbService _rgbService;
    private readonly RgbProfileService _profileService;
    private readonly IOpenRgbSettings _settings;
    private bool _disposed;

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private RgbDevice? _selectedDevice;
    [ObservableProperty] private Color _selectedColor = Color.FromArgb(255, 255, 0, 0);
    [ObservableProperty] private string? _selectedProfile;
    [ObservableProperty] private string _newProfileName = string.Empty;
    [ObservableProperty] private bool _hasDevices;
    [ObservableProperty] private string _initLogText = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _hasError;
    [ObservableProperty] private string _errorMessage = string.Empty;

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

    public RgbViewModel(RgbService rgbService, RgbProfileService profileService, IOpenRgbSettings settings)
    {
        _rgbService = rgbService;
        _profileService = profileService;
        _settings = settings;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        StatusText = Loc.Get("StatRGBScanning");

        try
        {
            await _rgbService.InitializeAsync(_settings);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = Loc.Format("StatRGBStartupError", ex.Message);
            StatusText = ErrorMessage;
        }

        BuildInitLogText();
        LoadDevices();
        ReloadProfiles();
        IsLoading = false;
    }

    private void BuildInitLogText()
    {
        var ok = _rgbService.InitLog.Where(l => l.StartsWith("[OK]")).ToList();
        var skip = _rgbService.InitLog.Where(l => l.StartsWith("[SKIP]")).ToList();

        var okNames = ok.Select(l => l.Replace("[OK] ", "")).ToList();
        var detail = okNames.Count > 0
            ? Loc.Format("StatInitLogActive", string.Join(", ", okNames), skip.Count)
            : Loc.Get("StatInitLogNoProvider");

        InitLogText = detail;
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
            StatusText = Loc.Format("StatRGBScanError", ex.Message);
        }

        HasDevices = Devices.Count > 0;

        if (HasDevices)
        {
            SelectedDevice ??= Devices[0];
            int lampCount = Devices.Count(d => d.Backend == RgbBackend.LampArray);
            int sdkCount = Devices.Count(d => d.Backend == RgbBackend.RgbNet);
            StatusText = Loc.Format("StatRGBDevices", Devices.Count, sdkCount, lampCount);
        }
        else
        {
            StatusText = Loc.Get("StatRGBNoDevice");
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
        StatusText = Loc.Format("StatRGBColorApplied", SelectedDevice.Name);
    }

    [RelayCommand]
    private void ApplyColorToAll()
    {
        if (Devices.Count == 0) return;
        _rgbService.SetAllDevicesColor(Devices, SelectedColor);
        StatusText = Loc.Format("StatRGBAllApplied", Devices.Count);
    }

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
            Name = name,
            Devices = Devices.Select(d => RgbProfileEntry.FromColor(d.Name, d.CurrentColor)).ToList()
        };

        _profileService.Save(profile);
        ReloadProfiles();
        NewProfileName = string.Empty;
        SelectedProfile = name;
        StatusText = Loc.Format("StatRGBProfileSaved", name);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _rgbService.Dispose();
    }
}
