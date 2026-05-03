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
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _fanStatusText = "";
    [ObservableProperty] private int _pollingIntervalMs = 2000;
    [ObservableProperty] private bool _hasControllableFans;

    // Live summary values for sidebar and quick-stats strip
    [ObservableProperty] private string _maxCpuTempText = "—";
    [ObservableProperty] private string _maxGpuTempText = "—";
    [ObservableProperty] private string _maxFanRpmText = "—";
    [ObservableProperty] private int _activeFanCount;

    public ObservableCollection<SensorGroup> TemperatureGroups { get; } = new();
    public ObservableCollection<SensorGroup> FanGroups { get; } = new();
    public ObservableCollection<SensorGroup> LoadGroups { get; } = new();
    public ObservableCollection<FanControl> ControllableFans { get; } = new();

    public FanViewModel(HardwareService hardwareService, FanControlService fanControlService, int pollingIntervalMs = 2000)
    {
        _hardwareService = hardwareService;
        _fanControlService = fanControlService;
        _pollingIntervalMs = pollingIntervalMs;

        StatusText = Loc.Get("StatReadingSensors");
        FanStatusText = Loc.Get("StatReady");

        if (!hardwareService.IsAvailable)
        {
            StatusText = Loc.Format("StatSensorError", hardwareService.InitError ?? "Unknown error");
            FanStatusText = Loc.Get("StatHWFailed");
            IsLoading = false;
        }

        DiscoverFans();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(pollingIntervalMs) };
        _timer.Tick += async (_, _) => await TickAsync();
        _timer.Start();

        _ = RefreshReadings();
    }

    partial void OnPollingIntervalMsChanged(int value)
        => _timer.Interval = TimeSpan.FromMilliseconds(value);

    private async Task TickAsync()
    {
        try
        {
            var readings = await Task.Run(() => _hardwareService.GetAllReadings());
            UpdateGroups(TemperatureGroups, readings.Where(r => r.Category == SensorCategory.Temperature));
            UpdateGroups(FanGroups, readings.Where(r => r.Category == SensorCategory.FanSpeed));
            UpdateGroups(LoadGroups, readings.Where(r => r.Category == SensorCategory.Load));
            UpdateLiveSummary(readings);
            StatusText = Loc.Format("StatLastUpdate", DateTime.Now.ToString("HH:mm:ss"));
            IsLoading = false;
            RefreshFanStates();
        }
        catch (Exception ex)
        {
            StatusText = Loc.Format("StatError", ex.Message);
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshReadings()
    {
        try
        {
            var readings = await Task.Run(() => _hardwareService.GetAllReadings());
            UpdateGroups(TemperatureGroups, readings.Where(r => r.Category == SensorCategory.Temperature));
            UpdateGroups(FanGroups, readings.Where(r => r.Category == SensorCategory.FanSpeed));
            UpdateGroups(LoadGroups, readings.Where(r => r.Category == SensorCategory.Load));
            UpdateLiveSummary(readings);
            StatusText = Loc.Format("StatLastUpdate", DateTime.Now.ToString("HH:mm:ss"));
            IsLoading = false;
        }
        catch (Exception ex)
        {
            StatusText = Loc.Format("StatError", ex.Message);
            IsLoading = false;
        }
    }

    private void UpdateLiveSummary(List<SensorReading> readings)
    {
        var temps = readings.Where(r => r.Category == SensorCategory.Temperature).ToList();
        var fans = readings.Where(r => r.Category == SensorCategory.FanSpeed).ToList();

        var cpuMax = temps
            .Where(r => r.HardwareName.Contains("CPU", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).DefaultIfEmpty(0).Max();
        MaxCpuTempText = cpuMax > 0 ? $"{cpuMax:F0}°C" : "—";

        var gpuMax = temps
            .Where(r => r.HardwareName.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                        r.HardwareName.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase) ||
                        r.HardwareName.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Value).DefaultIfEmpty(0).Max();
        MaxGpuTempText = gpuMax > 0 ? $"{gpuMax:F0}°C" : "—";

        var fanMax = fans.Select(r => r.Value).DefaultIfEmpty(0).Max();
        MaxFanRpmText = fanMax > 0 ? $"{fanMax:F0} RPM" : "—";

        ActiveFanCount = fans.Count;
    }

    [RelayCommand]
    private void RescanFans() => DiscoverFans();

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
                    FanStatusText = Loc.Format("StatFanPercent", f.Name, percent);
                };
                fan.OnModeChanged = (f, manual) =>
                {
                    if (manual)
                    {
                        _fanControlService.SetSpeed(f, f.CurrentSpeed);
                        FanStatusText = Loc.Format("StatFanManual", f.Name);
                    }
                    else
                    {
                        _fanControlService.SetAuto(f);
                        FanStatusText = Loc.Format("StatFanAuto", f.Name);
                    }
                };

                _fanControlService.RefreshFanState(fan);
                ControllableFans.Add(fan);
            }

            HasControllableFans = ControllableFans.Count > 0;
            FanStatusText = HasControllableFans
                ? Loc.Format("StatControllableFans", ControllableFans.Count)
                : Loc.Get("StatNoControllableFans");
        }
        catch (Exception ex)
        {
            FanStatusText = Loc.Format("StatScanError", ex.Message);
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
