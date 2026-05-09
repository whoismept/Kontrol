using LibreHardwareMonitor.Hardware;

namespace Kontrol.Fan;

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

    public record HardwareInfo(string Name, string TypeLabel);

    public List<HardwareInfo> GetHardwareInfos()
    {
        var list = new List<HardwareInfo>();
        foreach (var hw in GetHardwareList())
        {
            var label = hw.HardwareType switch
            {
                HardwareType.Cpu       => "CPU",
                HardwareType.GpuNvidia => "GPU",
                HardwareType.GpuAmd    => "GPU",
                // GpuIntel = integrated graphics — shares the CPU die,
                // reports identical temperatures and has no independent fan.
                // Exclude it so the dashboard doesn't show a duplicate card.
                _ => null
            };
            if (label is not null)
                list.Add(new HardwareInfo(hw.Name, label));
        }
        return list;
    }

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
                    // Use the parent hardware name so sub-hardware sensors
                    // (e.g. GPU fan reported under a GPU sub-component) are
                    // attributed to the top-level device that GetHardwareInfos() returns.
                    CollectReadings(sub, readings, parentName: hardware.Name);
                }
            }
        }
        catch { }

        return readings;
    }

    private static void CollectReadings(IHardware hardware, List<SensorReading> readings,
                                        string? parentName = null)
    {
        var hwName = parentName ?? hardware.Name;
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
                HardwareName = hwName,
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
