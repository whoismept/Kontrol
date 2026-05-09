using CommunityToolkit.Mvvm.ComponentModel;
using RGB.NET.Core;
using Windows.Devices.Lights;
using Windows.UI;

namespace Kontrol.Rgb;

public enum RgbBackend { RgbNet, LampArray }

public partial class RgbDevice : ObservableObject
{
    public IRGBDevice? Device { get; init; }
    public LampArray? LampArray { get; init; }

    public RgbBackend Backend { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int LedCount { get; set; }

    [ObservableProperty]
    private Windows.UI.Color _currentColor = new() { A = 255, R = 0, G = 0, B = 0 };

    public string DeviceInfoText => $"{Type} · {LedCount} LED · {(Backend == RgbBackend.LampArray ? "Native" : "SDK")}";
}
