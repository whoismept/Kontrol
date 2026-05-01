using CommunityToolkit.Mvvm.ComponentModel;
using RGB.NET.Core;
using WColor = System.Windows.Media.Color;
using WColors = System.Windows.Media.Colors;

namespace Kontrol.Models;

public partial class RgbDevice : ObservableObject
{
    public IRGBDevice Device { get; init; } = default!;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public int LedCount { get; set; }

    [ObservableProperty]
    private WColor _currentColor = WColors.Black;
}
