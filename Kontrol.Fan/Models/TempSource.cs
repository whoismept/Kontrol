namespace Kontrol.Fan;

public enum TempSourceMode
{
    Single,
    Max,
    Average,
    Min
}

public class SensorRef
{
    public string HardwareName { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
}

public class TempSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public TempSourceMode Mode { get; set; } = TempSourceMode.Max;
    public List<SensorRef> SensorRefs { get; set; } = new();
}
