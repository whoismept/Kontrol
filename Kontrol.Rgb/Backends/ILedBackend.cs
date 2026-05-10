using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Backends;

/// <summary>
/// Common interface every RGB backend must implement.
/// A single backend can manage multiple physical devices.
/// </summary>
public interface ILedBackend : IDisposable
{
    string Name { get; }

    /// <summary>Discovers devices supported by this backend.</summary>
    Task<List<RgbDevice>> DiscoverDevicesAsync();

    /// <summary>Sets all LEDs of a device to a single color.</summary>
    void SetColor(RgbDevice device, WinColor color);

    /// <summary>Sends individual colors per LED (array length must match device LED count).</summary>
    void SetColors(RgbDevice device, WinColor[] colors);

    /// <summary>
    /// Sets a single zone of a device to a specific color.
    /// Default implementation falls back to whole-device color for backends that
    /// do not support per-zone control (e.g. LampArray).
    /// </summary>
    void SetZoneColor(RgbDevice device, RgbZone zone, WinColor color)
    {
        SetColor(device, color);
    }
}
