using Hardcodet.Wpf.TaskbarNotification;
using Kontrol.Models;
using System.Windows.Threading;

namespace Kontrol.Services;

public class TempAlertService : IDisposable
{
    private readonly HardwareService _hardwareService;
    private readonly TaskbarIcon _trayIcon;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly HashSet<string> _activeAlerts = new();
    private DateTime _lastAlertTime = DateTime.MinValue;
    private bool _disposed;

    private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(60);

    public TempAlertService(HardwareService hardwareService, TaskbarIcon trayIcon, AppSettings settings)
    {
        _hardwareService = hardwareService;
        _trayIcon = trayIcon;
        _settings = settings;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += (_, _) => CheckTemperatures();
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
                _trayIcon.ShowBalloonTip(
                    "Yüksek Sıcaklık Uyarısı",
                    message,
                    BalloonIcon.Warning);
            }
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
    }
}
