using CommunityToolkit.Mvvm.ComponentModel;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb;

/// <summary>
/// Represents a single addressable zone within an RGB device.
///
/// Follows the OpenRGB zone model (RGBControllerAPI.md):
///   - Each zone has a name, a channel index, a default LED count, and optional user override.
///   - Resizable zones (ARGB headers) depend on what strip/device is attached, so the user
///     must manually specify the LED count.
///   - Colors are sent per-zone via independent protocol channels — zones are independent.
/// </summary>
public partial class RgbZone : ObservableObject
{
    /// <summary>Zone display name (e.g. "Addressable 1", "D_LED1 Header").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Protocol channel index for this zone (0-indexed). Matches AuraUSB channel byte.</summary>
    public int Channel { get; init; }

    /// <summary>Default LED count from the device definition JSON.</summary>
    public int DefaultLedCount { get; init; }

    /// <summary>
    /// Whether this zone's LED count is user-adjustable.
    /// True for ARGB headers where count depends on connected strip length.
    /// </summary>
    public bool Resizable { get; init; }

    /// <summary>
    /// User-specified LED count override.
    /// 0 = use DefaultLedCount.
    /// </summary>
    [ObservableProperty]
    private int _userLedCount;

    /// <summary>Current color of this zone (updated when color is sent).</summary>
    [ObservableProperty]
    private WinColor _currentColor = WinColor.FromArgb(255, 0, 0, 0);

    /// <summary>Effective LED count: user override if set, otherwise default from definition.</summary>
    public int EffectiveLedCount => UserLedCount > 0 ? UserLedCount : DefaultLedCount;

    partial void OnUserLedCountChanged(int value)
    {
        OnPropertyChanged(nameof(EffectiveLedCount));
        OnPropertyChanged(nameof(ZoneInfoText));
    }

    public string ZoneInfoText
    {
        get
        {
            var ledStr = UserLedCount > 0
                ? $"{UserLedCount} LED (custom)"
                : $"{DefaultLedCount} LED";
            return Resizable ? $"{ledStr} · adjustable" : ledStr;
        }
    }
}
