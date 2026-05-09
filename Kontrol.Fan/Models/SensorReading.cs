using CommunityToolkit.Mvvm.ComponentModel;

namespace Kontrol.Fan;

public enum SensorCategory
{
    Temperature,
    FanSpeed,
    Load,
    Voltage,
    Power
}

public partial class SensorReading : ObservableObject
{
    public string HardwareName { get; set; } = string.Empty;
    public string SensorName { get; set; } = string.Empty;
    public SensorCategory Category { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    private float _value;

    public string Unit => Category switch
    {
        SensorCategory.Temperature => "°C",
        SensorCategory.FanSpeed => " RPM",
        SensorCategory.Load => "%",
        SensorCategory.Voltage => " V",
        SensorCategory.Power => " W",
        _ => string.Empty
    };

    public string DisplayValue => $"{Value:F0}{Unit}";
}
