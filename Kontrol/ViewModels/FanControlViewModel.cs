using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Models;
using Kontrol.Models.Fan;
using Kontrol.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;

namespace Kontrol.ViewModels;

public partial class FanControlViewModel : ObservableObject
{
    private readonly FanControllerService _controller;
    private readonly HardwareService _hardwareService;

    [ObservableProperty] private string _statusText = "Hazır";
    [ObservableProperty] private FanAssignmentViewModel? _selectedFan;
    [ObservableProperty] private FanCurve? _selectedCurve;
    [ObservableProperty] private TempSource? _selectedTempSource;

    public ObservableCollection<FanAssignmentViewModel> FanAssignments { get; } = new();
    public ObservableCollection<FanCurve> Curves { get; } = new();
    public ObservableCollection<TempSource> TempSources { get; } = new();
    public ObservableCollection<string> AvailableSensors { get; } = new();

    public static IReadOnlyList<FanMode> FanModes { get; } = Enum.GetValues<FanMode>();
    public static IReadOnlyList<TempSourceMode> TempSourceModes { get; } = Enum.GetValues<TempSourceMode>();
    public static IReadOnlyList<FanCurveType> CurveTypes { get; } = Enum.GetValues<FanCurveType>();

    public FanControlViewModel(FanControllerService controller, HardwareService hardwareService)
    {
        _controller = controller;
        _hardwareService = hardwareService;

        _controller.ConfigChanged += OnConfigChanged;
        _controller.FanUpdated += OnFanUpdated;

        LoadFromConfig();
        RefreshAvailableSensors();
    }

    private void OnConfigChanged()
    {
        LoadFromConfig();
    }

    private void OnFanUpdated(string fanKey, float targetPercent, float tempC)
    {
        var vm = FanAssignments.FirstOrDefault(a => a.FanKey == fanKey);
        if (vm is null) return;
        vm.CurrentTargetPercent = targetPercent;
        vm.CurrentTempC = tempC;
    }

    private void LoadFromConfig()
    {
        var config = _controller.Config;

        Curves.Clear();
        foreach (var c in config.Curves) Curves.Add(c);

        TempSources.Clear();
        foreach (var s in config.TempSources) TempSources.Add(s);

        SyncFanAssignments(config);

        StatusText = $"{FanAssignments.Count} fan, {Curves.Count} eğri, {TempSources.Count} kaynak";
    }

    private void SyncFanAssignments(FanConfig config)
    {
        var fans = _controller.Fans;
        var newList = new List<FanAssignmentViewModel>();

        foreach (var fan in fans)
        {
            var key = _controller.GetFanKey(fan);
            var assignment = config.Assignments.FirstOrDefault(a => a.FanKey == key);

            if (assignment is null)
            {
                assignment = new FanAssignment { FanKey = key, Mode = FanMode.Auto };
                config.Assignments.Add(assignment);
            }

            var existing = FanAssignments.FirstOrDefault(a => a.FanKey == key);
            if (existing is not null)
            {
                existing.UpdateFrom(assignment, fan);
                newList.Add(existing);
            }
            else
            {
                var vm = new FanAssignmentViewModel(assignment, fan, this);
                newList.Add(vm);
            }
        }

        FanAssignments.Clear();
        foreach (var vm in newList) FanAssignments.Add(vm);
    }

    [RelayCommand]
    private void RescanFans()
    {
        _controller.DiscoverFans();
        LoadFromConfig();
        RefreshAvailableSensors();
        StatusText = $"{FanAssignments.Count} fan bulundu";
    }

    [RelayCommand]
    private void SaveConfig()
    {
        ApplyViewModelsToConfig();
        _controller.SaveConfig();
        StatusText = $"Kaydedildi {DateTime.Now:HH:mm:ss}";
    }

    [RelayCommand]
    private void AddCurve()
    {
        var curve = new FanCurve
        {
            Name = $"Yeni Eğri {Curves.Count + 1}",
            Points = new()
            {
                new() { TempC = 30, Percent = 25 },
                new() { TempC = 50, Percent = 50 },
                new() { TempC = 70, Percent = 75 },
                new() { TempC = 90, Percent = 100 }
            }
        };
        _controller.Config.Curves.Add(curve);
        Curves.Add(curve);
        SelectedCurve = curve;
    }

    [RelayCommand]
    private void DeleteCurve()
    {
        if (SelectedCurve is null) return;
        _controller.Config.Curves.Remove(SelectedCurve);
        Curves.Remove(SelectedCurve);
        SelectedCurve = Curves.FirstOrDefault();
    }

    [RelayCommand]
    private void AddTempSource()
    {
        var source = new TempSource
        {
            Name = $"Yeni Kaynak {TempSources.Count + 1}",
            SensorRefs = new() { new SensorRef { HardwareName = "*", SensorName = "*" } }
        };
        _controller.Config.TempSources.Add(source);
        TempSources.Add(source);
        SelectedTempSource = source;
    }

    [RelayCommand]
    private void DeleteTempSource()
    {
        if (SelectedTempSource is null) return;
        _controller.Config.TempSources.Remove(SelectedTempSource);
        TempSources.Remove(SelectedTempSource);
        SelectedTempSource = TempSources.FirstOrDefault();
    }

    [RelayCommand]
    private void AddCurvePoint()
    {
        if (SelectedCurve is null) return;
        var last = SelectedCurve.Points.LastOrDefault();
        SelectedCurve.Points.Add(new CurvePoint
        {
            TempC = (last?.TempC ?? 30) + 10,
            Percent = Math.Min((last?.Percent ?? 50) + 15, 100)
        });
        OnPropertyChanged(nameof(SelectedCurve));
    }

    [RelayCommand]
    private void RemoveCurvePoint(CurvePoint? point)
    {
        if (SelectedCurve is null || point is null) return;
        if (SelectedCurve.Points.Count <= 2) return;
        SelectedCurve.Points.Remove(point);
        OnPropertyChanged(nameof(SelectedCurve));
    }

    [RelayCommand]
    private void AddSensorRef()
    {
        if (SelectedTempSource is null) return;
        SelectedTempSource.SensorRefs.Add(new SensorRef { HardwareName = "*", SensorName = "*" });
        OnPropertyChanged(nameof(SelectedTempSource));
    }

    [RelayCommand]
    private void RemoveSensorRef(SensorRef? sref)
    {
        if (SelectedTempSource is null || sref is null) return;
        if (SelectedTempSource.SensorRefs.Count <= 1) return;
        SelectedTempSource.SensorRefs.Remove(sref);
        OnPropertyChanged(nameof(SelectedTempSource));
    }

    private void RefreshAvailableSensors()
    {
        AvailableSensors.Clear();
        try
        {
            var readings = _hardwareService.GetAllReadings();
            foreach (var r in readings.Where(r => r.Category == SensorCategory.Temperature))
            {
                AvailableSensors.Add($"{r.HardwareName} → {r.SensorName}");
            }
        }
        catch { }
    }

    [RelayCommand]
    private void ExportConfig()
    {
        try
        {
            ApplyViewModelsToConfig();
            var dialog = new SaveFileDialog
            {
                Title = "Fan Profili Dışa Aktar",
                Filter = "JSON dosyası (*.json)|*.json",
                FileName = "kontrol_fan_profile.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = JsonSerializer.Serialize(_controller.Config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                StatusText = $"Profil dışa aktarıldı: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Dışa aktarma hatası: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ImportConfig()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Fan Profili İçe Aktar",
                Filter = "JSON dosyası (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = File.ReadAllText(dialog.FileName);
                var config = JsonSerializer.Deserialize<FanConfig>(json);
                if (config is null)
                {
                    StatusText = "Geçersiz profil dosyası";
                    return;
                }

                _controller.UpdateConfig(config);
                LoadFromConfig();
                StatusText = $"Profil içe aktarıldı: {Path.GetFileName(dialog.FileName)}";
            }
        }
        catch (Exception ex)
        {
            StatusText = $"İçe aktarma hatası: {ex.Message}";
        }
    }

    internal void OnAssignmentChanged()
    {
        ApplyViewModelsToConfig();
        _controller.SaveConfig();
    }

    private void ApplyViewModelsToConfig()
    {
        var config = _controller.Config;
        config.Assignments.Clear();
        foreach (var vm in FanAssignments)
        {
            config.Assignments.Add(vm.ToAssignment());
        }
    }
}
