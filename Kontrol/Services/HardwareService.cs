using Kontrol.Models;
using LibreHardwareMonitor.Hardware;

namespace Kontrol.Services;

public class HardwareService : IDisposable
{
    private readonly Computer _computer;
    private bool _disposed;
    public bool IsAvailable { get; private set; }
    public string? InitError { get; private set; }

    public HardwareService()
    {
        _computer = new Computer
        {
            IsCpuEnabled        = true,
            IsGpuEnabled        = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled    = true,
            IsMemoryEnabled     = true,
            IsControllerEnabled = true,
            IsNetworkEnabled    = false
        };

        try
        {
            _computer.Open();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            InitError = ex.Message;
        }
    }

    public IEnumerable<IHardware> GetHardwareList() =>
        IsAvailable ? _computer.Hardware : Array.Empty<IHardware>();

    public void UpdateAll()
    {
        if (!IsAvailable) return;
        try
        {
            foreach (var hw in _computer.Hardware)
            {
                hw.Update();
                foreach (var sub in hw.SubHardware) sub.Update();
            }
        }
        catch { }
    }

    public List<SensorReading> GetAllReadings()
    {
        if (!IsAvailable) return [];

        var readings = new List<SensorReading>();
        try
        {
            foreach (var hardware in _computer.Hardware)
            {
                hardware.Update();
                CollectReadings(hardware, readings);

                foreach (var sub in hardware.SubHardware)
                {
                    sub.Update();
                    CollectReadings(sub, readings);
                }
            }
        }
        catch { }

        return readings;
    }

    private static void CollectReadings(IHardware hardware, List<SensorReading> readings)
    {
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is null) continue;

            var category = sensor.SensorType switch
            {
                SensorType.Temperature => SensorCategory.Temperature,
                SensorType.Fan         => SensorCategory.FanSpeed,
                SensorType.Load        => SensorCategory.Load,
                SensorType.Voltage     => SensorCategory.Voltage,
                SensorType.Power       => SensorCategory.Power,
                _                      => (SensorCategory?)null
            };

            if (category is null) continue;

            readings.Add(new SensorReading
            {
                HardwareName = hardware.Name,
                SensorName   = sensor.Name,
                Category     = category.Value,
                Value        = sensor.Value.Value
            });
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _computer.Close(); } catch { }
    }
}
