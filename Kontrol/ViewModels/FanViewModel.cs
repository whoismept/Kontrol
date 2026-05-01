using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;
using Kontrol.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace Kontrol.ViewModels;

public partial class FanViewModel : ObservableObject, IDisposable
{
    private readonly HardwareService _hardwareService;
    private readonly FanControlService _fanControlService;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _statusText = "Sensörler okunuyor...";
    [ObservableProperty] private string _fanStatusText = "Hazır";
    [ObservableProperty] private int _pollingIntervalMs = 2000;
    [ObservableProperty] private bool _hasControllableFans;

    public ObservableCollection<SensorGroup> TemperatureGroups { get; } = new();
    public ObservableCollection<SensorGroup> FanGroups { get; } = new();
    public ObservableCollection<SensorGroup> LoadGroups { get; } = new();
    public ObservableCollection<FanControl> ControllableFans { get; } = new();

    public FanViewModel(HardwareService hardwareService, FanControlService fanControlService, int pollingIntervalMs = 2000)
    {
        _hardwareService = hardwareService;
        _fanControlService = fanControlService;
        _pollingIntervalMs = pollingIntervalMs;

        if (!hardwareService.IsAvailable)
        {
            StatusText = $"Uyarı: Sensörler okunamıyor — {hardwareService.InitError ?? "Bilinmeyen hata"}";
            FanStatusText = "Donanım servisi başlatılamadı";
            IsLoading = false;
        }

        DiscoverFans();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(pollingIntervalMs) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        RefreshReadings();
    }

    partial void OnPollingIntervalMsChanged(int value)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(value);
    }

    private void Tick()
    {
        RefreshReadings();
        RefreshFanStates();
    }

    [RelayCommand]
    private void RefreshReadings()
    {
        try
        {
            var readings = _hardwareService.GetAllReadings();

            UpdateGroups(TemperatureGroups, readings.Where(r => r.Category == SensorCategory.Temperature));
            UpdateGroups(FanGroups,         readings.Where(r => r.Category == SensorCategory.FanSpeed));
            UpdateGroups(LoadGroups,        readings.Where(r => r.Category == SensorCategory.Load));

            StatusText = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";
            IsLoading = false;
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RescanFans()
    {
        DiscoverFans();
    }

    private void DiscoverFans()
    {
        foreach (var f in ControllableFans)
        {
            f.OnSpeedChanged = null;
            f.OnModeChanged = null;
        }
        ControllableFans.Clear();

        try
        {
            var fans = _fanControlService.DiscoverControllableFans(_hardwareService.GetHardwareList());

            foreach (var fan in fans)
            {
                fan.OnSpeedChanged = (f, percent) =>
                {
                    _fanControlService.SetSpeed(f, percent);
                    FanStatusText = $"{f.Name} → %{percent:F0}";
                };
                fan.OnModeChanged = (f, manual) =>
                {
                    if (manual)
                    {
                        _fanControlService.SetSpeed(f, f.CurrentSpeed);
                        FanStatusText = $"{f.Name} manuel moda geçti";
                    }
                    else
                    {
                        _fanControlService.SetAuto(f);
                        FanStatusText = $"{f.Name} otomatik moda geçti";
                    }
                };

                _fanControlService.RefreshFanState(fan);
                ControllableFans.Add(fan);
            }

            HasControllableFans = ControllableFans.Count > 0;

            FanStatusText = HasControllableFans
                ? $"{ControllableFans.Count} kontrol edilebilir fan bulundu"
                : "Kontrol edilebilir fan yok — yönetici yetkisi gerekebilir";
        }
        catch (Exception ex)
        {
            FanStatusText = $"Fan tarama hatası: {ex.Message}";
        }
    }

    private void RefreshFanStates()
    {
        foreach (var fan in ControllableFans)
            _fanControlService.RefreshFanState(fan);
    }

    private static void UpdateGroups(ObservableCollection<SensorGroup> groups, IEnumerable<SensorReading> readings)
    {
        var grouped = readings
            .GroupBy(r => r.HardwareName)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var hardware in grouped)
        {
            var existing = groups.FirstOrDefault(g => g.HardwareName == hardware.Key);
            if (existing is null)
            {
                existing = new SensorGroup { HardwareName = hardware.Key };
                groups.Add(existing);
            }

            foreach (var reading in hardware.Value)
            {
                var existingSensor = existing.Sensors.FirstOrDefault(s => s.SensorName == reading.SensorName);
                if (existingSensor is null) existing.Sensors.Add(reading);
                else existingSensor.Value = reading.Value;
            }
        }

        var toRemove = groups.Where(g => !grouped.ContainsKey(g.HardwareName)).ToList();
        foreach (var g in toRemove) groups.Remove(g);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
    }
}
