using CommunityToolkit.Mvvm.ComponentModel;
using Kontrol.Models;
using Kontrol.Models.Fan;

namespace Kontrol.ViewModels;

public partial class FanAssignmentViewModel : ObservableObject
{
    private readonly FanControlViewModel _parent;
    private FanControl _fan;

    public string FanKey { get; private set; }
    public string FanName => _fan.Name;
    public string HardwareName => _fan.HardwareName;

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
        OnPropertyChanged(nameof(FanName));
        OnPropertyChanged(nameof(HardwareName));
        CurrentRpm = fan.CurrentRpm;
    }

    public FanAssignment ToAssignment() => new()
    {
        FanKey = FanKey,
        Mode = Mode,
        ManualPercent = ManualPercent,
        CurveId = CurveId,
        TempSourceId = TempSourceId,
        MinPercent = MinPercent,
        MaxPercent = MaxPercent,
        ZeroRpmBelowC = ZeroRpmBelowC
    };

    partial void OnModeChanged(FanMode value) => _parent.OnAssignmentChanged();
    partial void OnManualPercentChanged(float value) => _parent.OnAssignmentChanged();
    partial void OnCurveIdChanged(string? value) => _parent.OnAssignmentChanged();
    partial void OnTempSourceIdChanged(string? value) => _parent.OnAssignmentChanged();
    partial void OnMinPercentChanged(float value) => _parent.OnAssignmentChanged();
    partial void OnMaxPercentChanged(float value) => _parent.OnAssignmentChanged();
    partial void OnZeroRpmBelowCChanged(float? value) => _parent.OnAssignmentChanged();
}
