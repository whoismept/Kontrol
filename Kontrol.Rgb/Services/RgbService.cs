using Kontrol.Rgb.Backends;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb;

/// <summary>
/// Coordinates all RGB backends (LampArray + HID).
/// Has no dependency on OpenRGB; no external software is required.
/// </summary>
public class RgbService : IDisposable
{
    private readonly List<ILedBackend> _backends;
    private readonly List<string> _initLog = [];
    private bool _initialized;
    private bool _disposed;

    public IReadOnlyList<string> InitLog => _initLog;

    /// <summary>Total number of discovered devices across all backends.</summary>
    public int DeviceCount { get; private set; }

    public RgbService()
    {
        _backends =
        [
            new LampArrayBackend(),
            new HidBackend(),
        ];
    }

    /// <summary>Constructor for testing or custom backend injection.</summary>
    public RgbService(IEnumerable<ILedBackend> backends)
    {
        _backends = [.. backends];
    }

    /// <summary>
    /// Initializes all backends and discovers devices.
    /// Safe to call multiple times; only the first call is active.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        _initLog.Add($"[INFO] RGB service starting ({_backends.Count} backend(s))...");

        foreach (var backend in _backends)
        {
            try
            {
                var devices = await backend.DiscoverDevicesAsync();
                DeviceCount += devices.Count;

                if (devices.Count > 0)
                {
                    _initLog.Add($"[OK] {backend.Name}: {devices.Count} device(s) found");
                    foreach (var d in devices)
                    {
                        var zoneInfo = d.Zones.Count > 0 ? $", {d.Zones.Count} zone(s)" : "";
                        _initLog.Add($"  · {d.Name} ({d.LedCount} LED{zoneInfo}, {d.BackendName})");
                    }
                }
                else
                {
                    _initLog.Add($"[--] {backend.Name}: no devices found");
                }
            }
            catch (Exception ex)
            {
                _initLog.Add($"[FAIL] {backend.Name}: {ex.Message.Split('\n')[0]}");
            }
        }

        _initLog.Add($"[INFO] Total: {DeviceCount} RGB device(s)");
    }

    /// <summary>Returns the device list from all backends.</summary>
    public async Task<List<RgbDevice>> GetDevicesAsync()
    {
        var result = new List<RgbDevice>();
        foreach (var backend in _backends)
        {
            try
            {
                var devices = await backend.DiscoverDevicesAsync();
                result.AddRange(devices);
            }
            catch { }
        }
        return result;
    }

    /// <summary>Restarts all backends (e.g. after a settings change or device reconnect).</summary>
    public async Task ReconnectAsync()
    {
        _initialized = false;
        _initLog.Clear();
        DeviceCount = 0;

        foreach (var backend in _backends)
            try { backend.Dispose(); } catch { }

        _backends.Clear();
        _backends.Add(new LampArrayBackend());
        _backends.Add(new HidBackend());

        await InitializeAsync();
    }

    /// <summary>Sets all LEDs of a device to a single color.</summary>
    public void SetDeviceColor(RgbDevice device, WinColor color)
    {
        var backend = FindBackend(device);
        backend?.SetColor(device, color);
    }

    /// <summary>Sets a specific zone of a device to a color.</summary>
    public void SetDeviceZoneColor(RgbDevice device, RgbZone zone, WinColor color)
    {
        var backend = FindBackend(device);
        backend?.SetZoneColor(device, zone, color);
    }

    /// <summary>Sets multiple devices to the same color.</summary>
    public void SetAllDevicesColor(IEnumerable<RgbDevice> devices, WinColor color)
    {
        foreach (var device in devices)
            SetDeviceColor(device, color);
    }

    private ILedBackend? FindBackend(RgbDevice device)
    {
        // BackendName format: "HID-AuraUSB" → prefix is "HID" | "LampArray"
        var backendKey = device.BackendName.Split('-')[0];
        return _backends.FirstOrDefault(b =>
            b.Name.Equals(backendKey, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var backend in _backends)
            try { backend.Dispose(); } catch { }
    }
}
