using HidSharp;
using Kontrol.Rgb.Definitions;
using Kontrol.Rgb.Protocols;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Backends;

/// <summary>
/// Backend that communicates with RGB devices directly over USB HID.
///
/// Design note: composite USB devices (ASUS motherboards, etc.) expose multiple HID
/// interfaces to Windows — keyboard, consumer-control, Aura RGB, etc.  HidSharp lists
/// these as separate HidDevice objects sharing the same VID/PID but with different
/// DevicePath values and different MaxOutputReportLength values.
/// "expectedOutputReportLength" in the device definition is therefore critical for
/// selecting the correct RGB-control interface.
///
/// Zone model (following OpenRGB RGBControllerAPI):
///   - A device may have N zones, each with an independent protocol channel.
///   - SetColor/SetZoneColor always send channel-by-channel to avoid
///     overwriting zones that should remain at their previous color.
/// </summary>
public sealed class HidBackend : ILedBackend
{
    private readonly List<HidDeviceDefinition> _definitions;
    private readonly List<string> _log = [];

    private readonly Dictionary<string, (HidStream stream, HidDeviceDefinition def, IHidProtocol protocol)>
        _openDevices = [];

    private bool _disposed;

    private static readonly Dictionary<string, IHidProtocol> Protocols =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AuraUSB"]    = new AuraUsbProtocol(),
            ["RgbFusion2"] = new RgbFusion2Protocol(),
            ["NzxtHue2"]   = new NzxtHue2Protocol(),
            ["MsiMystic"]  = new MsiMysticProtocol(),
            ["AsusTuf"]    = new AuraUsbProtocol(),   // TUF Gaming is Aura-compatible
            ["ASRockPoly"] = new ASRockPolyProtocol(),
            ["CorsairHid"] = new CorsairHidProtocol(),
            ["Raw"]        = new RawHidProtocol(),
        };

    public string Name => "HID";
    public IReadOnlyList<string> Log => _log;

    public HidBackend() : this(DeviceDefinitionLoader.LoadAll()) { }

    public HidBackend(List<HidDeviceDefinition> definitions)
    {
        _definitions = definitions;
    }

    public Task<List<RgbDevice>> DiscoverDevicesAsync()
    {
        return Task.Run(() =>
        {
            _log.Clear();
            var result  = new List<RgbDevice>();
            var seenIds = new HashSet<string>();

            IEnumerable<HidDevice> hidDevices;
            try
            {
                hidDevices = DeviceList.Local.GetHidDevices();
            }
            catch (Exception ex)
            {
                _log.Add($"[FAIL] HID enumeration error: {ex.Message}");
                return result;
            }

            foreach (var hidDev in hidDevices)
            {
                var def = FindDefinition(hidDev);
                if (def is null) continue;

                // Unique ID based on path so different interfaces of the same device are distinct.
                var deviceId  = $"hid:{hidDev.VendorID:X4}:{hidDev.ProductID:X4}:{def.Id}:{hidDev.DevicePath.GetHashCode():X8}";
                var dedupeKey = $"{hidDev.VendorID:X4}:{hidDev.ProductID:X4}:{def.Id}";
                if (seenIds.Contains(dedupeKey)) continue;

                HidStream? stream = null;
                try
                {
                    var cfg = new OpenConfiguration();
                    cfg.SetOption(OpenOption.Exclusive, false);
                    cfg.SetOption(OpenOption.Interruptible, true);

                    stream = hidDev.Open(cfg);
                    stream.WriteTimeout = 1000;
                    stream.ReadTimeout  = 500;
                }
                catch (Exception ex)
                {
                    _log.Add($"[SKIP] {def.Name} could not be opened: {ex.Message}");
                    continue;
                }

                try
                {
                    var protocol = Protocols.GetValueOrDefault(def.Protocol) ?? Protocols["Raw"];
                    protocol.Initialize(hidDev, stream, def);

                    if (_openDevices.TryGetValue(deviceId, out var old))
                        try { old.stream.Dispose(); } catch { }

                    _openDevices[deviceId] = (stream, def, protocol);
                    seenIds.Add(dedupeKey);

                    _log.Add($"[OK] {def.Name} ({def.Protocol}) · {def.LedCount} LED · reportLen={hidDev.GetMaxOutputReportLength()} · {def.Zones.Count} zone(s)");

                    var device = new RgbDevice
                    {
                        Id          = deviceId,
                        Name        = def.Name,
                        Vendor      = def.Vendor,
                        Type        = def.Type,
                        LedCount    = def.LedCount,
                        BackendName = $"{Name}-{def.Protocol}",
                        BackendData = deviceId,
                    };

                    // Populate observable zones from the definition
                    foreach (var z in def.Zones)
                    {
                        device.Zones.Add(new RgbZone
                        {
                            Name            = z.Name,
                            Channel         = z.Channel,
                            DefaultLedCount = z.LedCount,
                            Resizable       = z.Resizable,
                        });
                    }

                    result.Add(device);
                }
                catch (Exception ex)
                {
                    _log.Add($"[FAIL] {def.Name} init error: {ex.Message}");
                    try { stream?.Dispose(); } catch { }
                }
            }

            return result;
        });
    }

    // -------------------------------------------------------------------------
    // Color setters
    // -------------------------------------------------------------------------

    public void SetColor(RgbDevice device, WinColor color)
    {
        if (device.BackendData is not string deviceId) return;
        if (!_openDevices.TryGetValue(deviceId, out var entry)) return;

        try
        {
            if (device.Zones.Count > 0)
            {
                // Send per-zone so each channel gets the correct packet.
                foreach (var zone in device.Zones)
                    SetZoneColorCore(entry, zone, color);

                device.CurrentColor = color;
            }
            else
            {
                // No zones: send to the whole device as a flat LED array.
                int ledCount = device.EffectiveLedCount;
                var colors   = new WinColor[Math.Max(1, ledCount)];
                Array.Fill(colors, color);
                entry.protocol.SetColors(entry.stream, entry.def, colors);
                device.CurrentColor = color;
            }
        }
        catch (Exception ex)
        {
            _log.Add($"[ERR] SetColor {device.Name}: {ex.Message}");
        }
    }

    public void SetColors(RgbDevice device, WinColor[] colors)
    {
        if (device.BackendData is not string deviceId) return;
        if (!_openDevices.TryGetValue(deviceId, out var entry)) return;

        try
        {
            entry.protocol.SetColors(entry.stream, entry.def, colors);
            if (colors.Length > 0) device.CurrentColor = colors[0];
        }
        catch (Exception ex)
        {
            _log.Add($"[ERR] SetColors {device.Name}: {ex.Message}");
        }
    }

    public void SetZoneColor(RgbDevice device, RgbZone zone, WinColor color)
    {
        if (device.BackendData is not string deviceId) return;
        if (!_openDevices.TryGetValue(deviceId, out var entry)) return;
        SetZoneColorCore(entry, zone, color);
    }

    /// <summary>
    /// Core zone color sender.
    /// Builds a fresh colors array sized to the zone's effective LED count and sends it
    /// to the zone's channel using a minimal single-zone definition (LedStart = 0),
    /// so the colors array is indexed from 0 regardless of position in the device buffer.
    /// </summary>
    private void SetZoneColorCore(
        (HidStream stream, HidDeviceDefinition def, IHidProtocol protocol) entry,
        RgbZone zone,
        WinColor color)
    {
        int ledCount = zone.EffectiveLedCount;
        if (ledCount <= 0) return;

        var colors = new WinColor[ledCount];
        Array.Fill(colors, color);

        // A minimal definition targeting only this zone's channel.
        // LedStart = 0 because we built a dedicated colors array for this zone.
        var singleZoneDef = new HidDeviceDefinition
        {
            Protocol                  = entry.def.Protocol,
            LedCount                  = ledCount,
            ExpectedOutputReportLength = entry.def.ExpectedOutputReportLength,
            Zones = [new HidZoneDefinition
            {
                Name     = zone.Name,
                LedStart = 0,
                LedCount = ledCount,
                Channel  = zone.Channel,
            }],
        };

        try
        {
            entry.protocol.SetColors(entry.stream, singleZoneDef, colors);
            zone.CurrentColor = color;
        }
        catch (Exception ex)
        {
            _log.Add($"[ERR] SetZoneColor {zone.Name}: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Device definition matching
    // -------------------------------------------------------------------------

    /// <summary>
    /// Matches VID + PID and optionally filters by MaxOutputReportLength.
    /// The length filter is critical for composite devices with multiple HID interfaces
    /// to ensure the RGB control interface is selected rather than a keyboard/media interface.
    /// </summary>
    private HidDeviceDefinition? FindDefinition(HidDevice hidDev)
    {
        foreach (var def in _definitions)
        {
            if (def.Vid != hidDev.VendorID) continue;
            if (!def.Pids.Contains(hidDev.ProductID)) continue;

            if (def.ExpectedOutputReportLength > 0)
            {
                int actual;
                try { actual = hidDev.GetMaxOutputReportLength(); }
                catch { continue; }

                if (actual != def.ExpectedOutputReportLength) continue;
            }

            return def;
        }
        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var (stream, _, _) in _openDevices.Values)
            try { stream.Dispose(); } catch { }
        _openDevices.Clear();
    }
}
