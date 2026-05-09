using System.IO;
using System.Text.Json;

namespace Kontrol.Fan;

public class FanConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kontrol", "fan_config.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public FanConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<FanConfig>(json, Json) ?? CreateDefault();
            }
        }
        catch { }

        var config = CreateDefault();
        Save(config);
        return config;
    }

    public void Save(FanConfig config)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, Json));
        }
        catch { }
    }

    public static FanConfig CreateDefault()
    {
        var cpuMax = new TempSource
        {
            Id = "cpu_max",
            Name = "CPU Max",
            Mode = TempSourceMode.Max,
            SensorRefs = new() { new SensorRef { HardwareName = "*CPU*", SensorName = "*" } }
        };

        var gpuMax = new TempSource
        {
            Id = "gpu_max",
            Name = "GPU Max",
            Mode = TempSourceMode.Max,
            SensorRefs = new() { new SensorRef { HardwareName = "*GPU*", SensorName = "*" } }
        };

        var mbMax = new TempSource
        {
            Id = "mb_max",
            Name = "Motherboard Max",
            Mode = TempSourceMode.Max,
            SensorRefs = new() { new SensorRef { HardwareName = "*Motherboard*", SensorName = "*" } }
        };

        var silent = new FanCurve
        {
            Id = "silent",
            Name = "Silent",
            Type = FanCurveType.Graph,
            Points = new()
            {
                new() { TempC = 30, Percent = 20 },
                new() { TempC = 50, Percent = 30 },
                new() { TempC = 65, Percent = 45 },
                new() { TempC = 75, Percent = 60 },
                new() { TempC = 85, Percent = 80 },
                new() { TempC = 95, Percent = 100 }
            },
            HysteresisC = 3f,
            ResponseTimeMs = 2000
        };

        var balanced = new FanCurve
        {
            Id = "balanced",
            Name = "Balanced",
            Type = FanCurveType.Graph,
            Points = new()
            {
                new() { TempC = 30, Percent = 30 },
                new() { TempC = 45, Percent = 40 },
                new() { TempC = 60, Percent = 55 },
                new() { TempC = 70, Percent = 70 },
                new() { TempC = 80, Percent = 85 },
                new() { TempC = 90, Percent = 100 }
            },
            HysteresisC = 2f,
            ResponseTimeMs = 1000
        };

        var performance = new FanCurve
        {
            Id = "performance",
            Name = "Performance",
            Type = FanCurveType.Graph,
            Points = new()
            {
                new() { TempC = 30, Percent = 40 },
                new() { TempC = 40, Percent = 50 },
                new() { TempC = 55, Percent = 65 },
                new() { TempC = 65, Percent = 80 },
                new() { TempC = 75, Percent = 95 },
                new() { TempC = 85, Percent = 100 }
            },
            HysteresisC = 1f,
            ResponseTimeMs = 500
        };

        return new FanConfig
        {
            TempSources = new() { cpuMax, gpuMax, mbMax },
            Curves = new() { silent, balanced, performance },
            Assignments = new()
        };
    }
}
