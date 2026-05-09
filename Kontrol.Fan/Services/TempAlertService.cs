namespace Kontrol.Fan;

public class TempAlertService : IDisposable
{
    private readonly HardwareService _hardwareService;
    private readonly IAlertSettings _settings;
    private readonly System.Timers.Timer _timer;
    private readonly HashSet<string> _activeAlerts = new();
    private DateTime _lastAlertTime = DateTime.MinValue;
    private bool _disposed;

    private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(60);

    public event Action<string, string>? AlertTriggered;

    public TempAlertService(HardwareService hardwareService, IAlertSettings settings)
    {
        _hardwareService = hardwareService;
        _settings = settings;

        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += (_, _) => CheckTemperatures();
        _timer.AutoReset = true;
    }

    public void Start() => _timer.Start();

    private void CheckTemperatures()
    {
        if (!_settings.TempAlertsEnabled) return;

        try
        {
            var readings = _hardwareService.GetAllReadings();
            var threshold = _settings.TempAlertThresholdC;
            var criticalSensors = new List<string>();

            foreach (var r in readings)
            {
                if (r.Category != SensorCategory.Temperature) continue;
                var key = $"{r.HardwareName}|{r.SensorName}";

                if (r.Value >= threshold)
                {
                    criticalSensors.Add($"{r.SensorName}: {r.Value:F0}°C");
                    _activeAlerts.Add(key);
                }
                else
                {
                    _activeAlerts.Remove(key);
                }
            }

            if (criticalSensors.Count > 0 && DateTime.Now - _lastAlertTime > AlertCooldown)
            {
                _lastAlertTime = DateTime.Now;
                var message = string.Join("\n", criticalSensors.Take(5));
                AlertTriggered?.Invoke("High Temperature Alert", message);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }
}
