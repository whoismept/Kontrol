using Kontrol.Models;
using Kontrol.Models.Fan;
using System.Windows.Threading;

namespace Kontrol.Services;

public class FanControllerService : IDisposable
{
    private readonly HardwareService _hardwareService;
    private readonly FanControlService _fanControlService;
    private readonly FanConfigService _configService;
    private readonly TempSourceEvaluator _tempEvaluator;
    private readonly FanCurveEvaluator _curveEvaluator;
    private readonly DispatcherTimer _timer;
    private bool _disposed;

    private FanConfig _config;
    private List<FanControl> _fans = new();

    public FanConfig Config => _config;
    public IReadOnlyList<FanControl> Fans => _fans;

    public event Action? ConfigChanged;

    // fanKey, targetPercent, tempC, currentRpm
    public event Action<string, float, float, float>? FanUpdated;

    public FanControllerService(
        HardwareService hardwareService,
        FanControlService fanControlService,
        FanConfigService configService,
        int pollingIntervalMs = 2000)
    {
        _hardwareService = hardwareService;
        _fanControlService = fanControlService;
        _configService = configService;
        _tempEvaluator = new TempSourceEvaluator();
        _curveEvaluator = new FanCurveEvaluator();

        _config = configService.Load();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(pollingIntervalMs) };
        _timer.Tick += (_, _) => Tick();
    }

    public void Start()
    {
        DiscoverFans();
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    public void SetPollingInterval(int ms)
        => _timer.Interval = TimeSpan.FromMilliseconds(ms);

    public void DiscoverFans()
        => _fans = _fanControlService.DiscoverControllableFans(_hardwareService.GetHardwareList());

    public string GetFanKey(FanControl fan) => $"{fan.HardwareName}|{fan.Name}";

    public void UpdateConfig(FanConfig config)
    {
        _config = config;
        _configService.Save(config);
        _curveEvaluator.ResetAll();
        ConfigChanged?.Invoke();
    }

    public void SaveConfig() => _configService.Save(_config);

    // Applies a single assignment immediately without waiting for the timer tick.
    // Called when the user changes mode or speed in the UI.
    public void ForceApplyNow(string fanKey)
    {
        var fan = _fans.FirstOrDefault(f => GetFanKey(f) == fanKey);
        if (fan is null) return;

        try
        {
            _hardwareService.UpdateAll();
            _fanControlService.RefreshFanState(fan);

            var assignment = _config.Assignments.FirstOrDefault(a => a.FanKey == fanKey);
            if (assignment is not null)
            {
                var readings = _hardwareService.GetAllReadings();
                ApplyAssignment(fan, assignment, readings);
            }
        }
        catch { }
    }

    private void Tick()
    {
        try
        {
            _hardwareService.UpdateAll();
            var readings = _hardwareService.GetAllReadings();

            foreach (var fan in _fans)
            {
                _fanControlService.RefreshFanState(fan);

                var fanKey = GetFanKey(fan);
                var assignment = _config.Assignments.FirstOrDefault(a => a.FanKey == fanKey);
                if (assignment is null) continue;

                ApplyAssignment(fan, assignment, readings);
            }
        }
        catch { }
    }

    private void ApplyAssignment(FanControl fan, FanAssignment assignment, List<SensorReading> readings)
    {
        var fanKey = assignment.FanKey;

        switch (assignment.Mode)
        {
            case FanMode.Auto:
                _fanControlService.SetAuto(fan);
                FanUpdated?.Invoke(fanKey, fan.CurrentSpeed, 0, fan.CurrentRpm);
                break;

            case FanMode.ManualConstant:
                _fanControlService.SetSpeed(fan, assignment.ManualPercent);
                FanUpdated?.Invoke(fanKey, assignment.ManualPercent, 0, fan.CurrentRpm);
                break;

            case FanMode.Curve:
                ApplyCurve(fan, assignment, readings);
                break;

            case FanMode.Off:
                _fanControlService.SetSpeed(fan, 0);
                FanUpdated?.Invoke(fanKey, 0, 0, fan.CurrentRpm);
                break;
        }
    }

    private void ApplyCurve(FanControl fan, FanAssignment assignment, List<SensorReading> readings)
    {
        if (assignment.CurveId is null || assignment.TempSourceId is null) return;

        var curve = _config.Curves.FirstOrDefault(c => c.Id == assignment.CurveId);
        var source = _config.TempSources.FirstOrDefault(s => s.Id == assignment.TempSourceId);
        if (curve is null || source is null) return;

        var tempC = _tempEvaluator.Evaluate(source, readings);
        if (tempC is null) return;

        var fanKey = assignment.FanKey;
        float rawPercent = _curveEvaluator.Evaluate(curve, tempC.Value, fanKey);
        float clamped = Math.Clamp(rawPercent, assignment.MinPercent, assignment.MaxPercent);

        if (assignment.ZeroRpmBelowC.HasValue && tempC.Value < assignment.ZeroRpmBelowC.Value)
            clamped = 0;

        _fanControlService.SetSpeed(fan, clamped);
        FanUpdated?.Invoke(fanKey, clamped, tempC.Value, fan.CurrentRpm);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        foreach (var fan in _fans)
        {
            try { _fanControlService.SetAuto(fan); } catch { }
        }
    }
}
