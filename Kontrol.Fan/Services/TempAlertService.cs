namespace Kontrol.Fan;

public class TempAlertService : IDisposable
{
    private readonly FanControllerService _fanController;
    private readonly IAlertSettings _settings;
    private readonly HashSet<string> _activeAlerts = new();
    private DateTime _lastAlertTime = DateTime.MinValue;
    private bool _disposed;

    private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(60);

    public event Action<string, string>? AlertTriggered;

    public TempAlertService(FanControllerService fanController, IAlertSettings settings)
    {
        _fanController = fanController;
        _settings = settings;
    }

    public void Start()
    {
        _fanController.ReadingsUpdated += CheckTemperatures;
    }

    private void CheckTemperatures(List<SensorReading> readings)
    {
        if (!_settings.TempAlertsEnabled) return;

        try
        {
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
        _fanController.ReadingsUpdated -= CheckTemperatures;
    }
}
