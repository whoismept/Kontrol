using CommunityToolkit.Mvvm.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace Kontrol.Fan;

public partial class FanControl : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string HardwareName { get; init; } = string.Empty;

    public IControl? Control { get; init; }
    public ISensor? FanSensor { get; init; }
    public ISensor? ControlSensor { get; init; }

    [ObservableProperty]
    private float _currentSpeed;

    [ObservableProperty]
    private float _currentRpm;

    [ObservableProperty]
    private bool _isManualMode;

    public float MinValue => Control?.MinSoftwareValue ?? 0;
    public float MaxValue => Control?.MaxSoftwareValue ?? 100;

    public Action<FanControl, float>? OnSpeedChanged { get; set; }
    public Action<FanControl, bool>? OnModeChanged { get; set; }

    private bool _suppressEvents;

    partial void OnCurrentSpeedChanged(float value)
    {
        if (_suppressEvents) return;
        if (IsManualMode) OnSpeedChanged?.Invoke(this, value);
    }

    partial void OnIsManualModeChanged(bool value)
    {
        if (_suppressEvents) return;
        OnModeChanged?.Invoke(this, value);
    }

    public void UpdateFromHardware(float? speedPercent, float? rpm, ControlMode? mode)
    {
        _suppressEvents = true;
        try
        {
            if (rpm.HasValue) CurrentRpm = rpm.Value;
            if (mode.HasValue) IsManualMode = mode.Value == ControlMode.Software;
            if (!IsManualMode && speedPercent.HasValue) CurrentSpeed = speedPercent.Value;
        }
        finally
        {
            _suppressEvents = false;
        }
    }
}
