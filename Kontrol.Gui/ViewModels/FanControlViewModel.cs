using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kontrol.Fan;
using Kontrol.Services;
using Microsoft.UI.Dispatching;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Kontrol.ViewModels;

public partial class FanControlViewModel : ObservableObject
{
    private readonly FanControllerService _controller;
    private readonly HardwareService _hardwareService;
    private readonly DispatcherQueue _dispatcher = DispatcherQueue.GetForCurrentThread();

    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private FanAssignmentViewModel? _selectedFan;
    [ObservableProperty] private FanCurve? _selectedCurve;
    [ObservableProperty] private TempSource? _selectedTempSource;
    [ObservableProperty] private bool _showHiddenFans = false;

    public bool HasSelectedCurve => SelectedCurve is not null;
    public bool HasSelectedTempSource => SelectedTempSource is not null;

    public string ShowHiddenButtonText => ShowHiddenFans
        ? Loc.Get("FanCtrlHideHidden")
        : Loc.Get("FanCtrlShowHidden");

    partial void OnShowHiddenFansChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowHiddenButtonText));
        foreach (var vm in FanAssignments) vm.NotifyVisibility();
    }

    partial void OnSelectedCurveChanged(FanCurve? value)
        => OnPropertyChanged(nameof(HasSelectedCurve));

    partial void OnSelectedTempSourceChanged(TempSource? value)
        => OnPropertyChanged(nameof(HasSelectedTempSource));

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

    private void OnConfigChanged() => LoadFromConfig();

    private void OnFanUpdated(string fanKey, float targetPercent, float tempC, float currentRpm)
    {
        _dispatcher.TryEnqueue(() =>
        {
            var vm = FanAssignments.FirstOrDefault(a => a.FanKey == fanKey);
            if (vm is null) return;
            vm.CurrentTargetPercent = targetPercent;
            vm.CurrentTempC = tempC;
            vm.CurrentRpm = currentRpm;
        });
    }

    private void LoadFromConfig()
    {
        var config = _controller.Config;

        Curves.Clear();
        foreach (var c in config.Curves) Curves.Add(c);

        TempSources.Clear();
        foreach (var s in config.TempSources) TempSources.Add(s);

        SyncFanAssignments(config);
        SetInitialTemps();

        int hidden = FanAssignments.Count(a => a.IsHidden);
        StatusText = Loc.Format("StatFanCount", FanAssignments.Count, hidden, Curves.Count, TempSources.Count);
    }

    private void SetInitialTemps()
    {
        try
        {
            var readings = _hardwareService.GetAllReadings();
            float maxTemp = readings
                .Where(r => r.Category == SensorCategory.Temperature)
                .Select(r => r.Value)
                .DefaultIfEmpty(0f)
                .Max();

            foreach (var vm in FanAssignments)
                if (vm.CurrentTempC == 0)
                    vm.CurrentTempC = maxTemp;
        }
        catch { }
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
                newList.Add(new FanAssignmentViewModel(assignment, fan, this));
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
        StatusText = Loc.Format("StatFansFound", FanAssignments.Count);
    }

    [RelayCommand]
    private void SaveConfig()
    {
        ApplyViewModelsToConfig();
        _controller.SaveConfig();
        StatusText = Loc.Format("StatSaved", DateTime.Now.ToString("HH:mm:ss"));
    }

    [RelayCommand]
    private void ToggleShowHidden() => ShowHiddenFans = !ShowHiddenFans;

    [RelayCommand]
    private void AddCurve()
    {
        var curve = new FanCurve
        {
            Name = Loc.Format("StatNewCurve", Curves.Count + 1),
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
            Name = Loc.Format("StatNewSource", TempSources.Count + 1),
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
    }

    [RelayCommand]
    private void RemoveCurvePoint(CurvePoint? point)
    {
        if (SelectedCurve is null || point is null) return;
        if (SelectedCurve.Points.Count <= 2) return;
        SelectedCurve.Points.Remove(point);
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
                AvailableSensors.Add($"{r.HardwareName} → {r.SensorName}");
        }
        catch { }
    }

    [RelayCommand]
    private async Task ExportConfig()
    {
        try
        {
            ApplyViewModelsToConfig();
            var picker = new FileSavePicker();
            var hwnd = WindowNative.GetWindowHandle(App.Window!);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeChoices.Add("JSON file", new List<string> { ".json" });
            picker.SuggestedFileName = "kontrol_fan_profile.json";

            var file = await picker.PickSaveFileAsync();
            if (file is not null)
            {
                var json = JsonSerializer.Serialize(_controller.Config, new JsonSerializerOptions { WriteIndented = true });
                await Windows.Storage.FileIO.WriteTextAsync(file, json);
                StatusText = Loc.Format("StatProfileExported", file.Name);
            }
        }
        catch (Exception ex)
        {
            StatusText = Loc.Format("StatExportError", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ImportConfig()
    {
        try
        {
            var picker = new FileOpenPicker();
            var hwnd = WindowNative.GetWindowHandle(App.Window!);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add(".json");

            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                var json = await Windows.Storage.FileIO.ReadTextAsync(file);
                var config = JsonSerializer.Deserialize<FanConfig>(json);
                if (config is null)
                {
                    StatusText = Loc.Get("StatImportInvalid");
                    return;
                }

                _controller.UpdateConfig(config);
                LoadFromConfig();
                StatusText = Loc.Format("StatProfileImported", file.Name);
            }
        }
        catch (Exception ex)
        {
            StatusText = Loc.Format("StatImportError", ex.Message);
        }
    }

    internal void OnAssignmentChanged(string fanKey)
    {
        ApplyViewModelsToConfig();
        _controller.SaveConfig();
        _controller.ForceApplyNow(fanKey);
    }

    private void ApplyViewModelsToConfig()
    {
        var config = _controller.Config;
        config.Assignments.Clear();
        foreach (var vm in FanAssignments)
            config.Assignments.Add(vm.ToAssignment());
    }
}

public partial class FanAssignmentViewModel : ObservableObject
{
    private readonly FanControlViewModel _parent;
    private FanControl _fan;

    public string FanKey { get; private set; }
    public string HardwareName => _fan.HardwareName;
    public string FanName => _fan.Name;
    public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? FanName : CustomName;
    public bool IsVisible => !IsHidden || _parent.ShowHiddenFans;
    public string LiveTempDisplay => $"{CurrentTempC:F1}°C → {CurrentTargetPercent:F0}%";

    public ObservableCollection<FanCurve> AvailableCurves => _parent.Curves;
    public ObservableCollection<TempSource> AvailableSources => _parent.TempSources;
    public static IReadOnlyList<FanMode> AllFanModes { get; } = Enum.GetValues<FanMode>();

    public FanCurve? SelectedCurve
    {
        get => _parent.Curves.FirstOrDefault(c => c.Id == CurveId);
        set { CurveId = value?.Id; OnPropertyChanged(); }
    }

    public TempSource? SelectedSource
    {
        get => _parent.TempSources.FirstOrDefault(s => s.Id == TempSourceId);
        set { TempSourceId = value?.Id; OnPropertyChanged(); }
    }

    [ObservableProperty] private string? _customName;
    [ObservableProperty] private bool _isHidden;
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private FanMode _mode;
    [ObservableProperty] private float _manualPercent;
    [ObservableProperty] private string? _curveId;
    [ObservableProperty] private string? _tempSourceId;
    [ObservableProperty] private float _minPercent;
    [ObservableProperty] private float _maxPercent;
    [ObservableProperty] private float? _zeroRpmBelowC;
    [ObservableProperty] private float _currentTargetPercent;
    [ObservableProperty] private float _currentTempC;
    [ObservableProperty] private float _currentRpm;

    public FanAssignmentViewModel(FanAssignment assignment, FanControl fan, FanControlViewModel parent)
    {
        _parent = parent;
        _fan = fan;
        FanKey = assignment.FanKey;
        _customName = assignment.CustomName;
        _isHidden = assignment.IsHidden;
        _mode = assignment.Mode;
        _manualPercent = assignment.ManualPercent;
        _curveId = assignment.CurveId;
        _tempSourceId = assignment.TempSourceId;
        _minPercent = assignment.MinPercent;
        _maxPercent = assignment.MaxPercent;
        _zeroRpmBelowC = assignment.ZeroRpmBelowC;
    }

    public void UpdateFrom(FanAssignment assignment, FanControl fan)
    {
        _fan = fan;
        CustomName = assignment.CustomName;
        IsHidden = assignment.IsHidden;
        OnPropertyChanged(nameof(FanName));
        OnPropertyChanged(nameof(HardwareName));
        OnPropertyChanged(nameof(DisplayName));
        CurrentRpm = fan.CurrentRpm;
    }

    public void NotifyVisibility() => OnPropertyChanged(nameof(IsVisible));

    public FanAssignment ToAssignment() => new()
    {
        FanKey = FanKey,
        CustomName = string.IsNullOrWhiteSpace(CustomName) ? null : CustomName.Trim(),
        IsHidden = IsHidden,
        Mode = Mode,
        ManualPercent = ManualPercent,
        CurveId = CurveId,
        TempSourceId = TempSourceId,
        MinPercent = MinPercent,
        MaxPercent = MaxPercent,
        ZeroRpmBelowC = ZeroRpmBelowC
    };

    [RelayCommand]
    private void StartRename() => IsRenaming = true;

    [RelayCommand]
    private void ConfirmRename()
    {
        IsRenaming = false;
        OnPropertyChanged(nameof(DisplayName));
        NotifyChanged();
    }

    [RelayCommand]
    private void ToggleHidden()
    {
        IsHidden = !IsHidden;
        NotifyChanged();
    }

    private void NotifyChanged() => _parent.OnAssignmentChanged(FanKey);

    partial void OnIsHiddenChanged(bool value) => OnPropertyChanged(nameof(IsVisible));
    partial void OnModeChanged(FanMode value) => NotifyChanged();
    partial void OnManualPercentChanged(float value) => NotifyChanged();
    partial void OnCurveIdChanged(string? value) { NotifyChanged(); OnPropertyChanged(nameof(SelectedCurve)); }
    partial void OnTempSourceIdChanged(string? value) { NotifyChanged(); OnPropertyChanged(nameof(SelectedSource)); }
    partial void OnMinPercentChanged(float value) => NotifyChanged();
    partial void OnMaxPercentChanged(float value) => NotifyChanged();
    partial void OnZeroRpmBelowCChanged(float? value) => NotifyChanged();
    partial void OnCurrentTempCChanged(float value) => OnPropertyChanged(nameof(LiveTempDisplay));
    partial void OnCurrentTargetPercentChanged(float value) => OnPropertyChanged(nameof(LiveTempDisplay));
}
