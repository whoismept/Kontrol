using CommunityToolkit.Mvvm.ComponentModel;
using RGB.NET.Core;
using Windows.Devices.Lights;
using WColor = System.Windows.Media.Color;
using WColors = System.Windows.Media.Colors;

namespace Kontrol.Models;

public enum RgbBackend { RgbNet, LampArray }

public partial class RgbDevice : ObservableObject
{
    // RGB.NET path — null for LampArray devices
    public IRGBDevice? Device { get; init; }

    // Windows native LampArray path — null for RGB.NET devices
    public LampArray? LampArray { get; init; }

    public RgbBackend Backend { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int LedCount { get; set; }

    [ObservableProperty]
    private WColor _currentColor = WColors.Black;
}
