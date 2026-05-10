using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb;

public partial class RgbDevice : ObservableObject
{
    /// <summary>Unique identifier — derived from backend name and device path.</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    /// <summary>Total LED count as reported by the device definition.</summary>
    public int LedCount { get; set; }

    /// <summary>
    /// Addressable zones within this device.
    /// Empty when the device definition has no zones (whole device treated as one zone).
    /// </summary>
    public ObservableCollection<RgbZone> Zones { get; } = [];

    /// <summary>Which backend owns this device (e.g. "LampArray", "HID-AuraUSB").</summary>
    public string BackendName { get; set; } = string.Empty;

    /// <summary>Backend-specific state — only the backend should touch this.</summary>
    public object? BackendData { get; set; }

    [ObservableProperty]
    private WinColor _currentColor = WinColor.FromArgb(255, 0, 0, 0);

    /// <summary>
    /// User-specified LED count override (device-level, used when Zones is empty).
    /// 0 = use LedCount from the JSON definition.
    /// </summary>
    [ObservableProperty]
    private int _userLedCount;

    /// <summary>Effective LED count: user override if set, otherwise JSON default.</summary>
    public int EffectiveLedCount => UserLedCount > 0 ? UserLedCount : LedCount;

    public string DeviceInfoText
    {
        get
        {
            if (Zones.Count > 0)
                return $"{Type} · {Zones.Count} zones · {Vendor}";
            if (UserLedCount > 0)
                return $"{Type} · {UserLedCount} LED (custom) · {Vendor}";
            return $"{Type} · {LedCount} LED · {Vendor}";
        }
    }

    partial void OnUserLedCountChanged(int value)
    {
        OnPropertyChanged(nameof(EffectiveLedCount));
        OnPropertyChanged(nameof(DeviceInfoText));
    }
}
