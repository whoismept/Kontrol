using Kontrol.Models;
using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.CoolerMaster;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.Msi;
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using RGB.NET.Devices.SteelSeries;
using RGB.NET.Devices.Wooting;
<<<<<<< HEAD
=======
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
>>>>>>> claude/strange-gagarin-b63c6b
using NetColor = RGB.NET.Core.Color;
using WColor = System.Windows.Media.Color;

namespace Kontrol.Services;

public class RgbService : IDisposable
{
    private readonly RGBSurface _surface = new();
    private readonly List<LampArray> _lampArrays = new();
    private readonly List<string> _initLog = new();
    private bool _initialized;
    private bool _disposed;

    public IReadOnlyList<string> InitLog => _initLog;

    public void Initialize(AppSettings? settings = null)
    {
        if (_initialized) return;

        // ── RGB.NET vendor providers (need vendor software installed) ──────────
        TryLoad("ASUS Aura", () => _surface.Load(AsusDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Corsair iCUE", () => _surface.Load(CorsairDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("CoolerMaster", () => _surface.Load(CoolerMasterDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Logitech", () => _surface.Load(LogitechDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("MSI", () => _surface.Load(MsiDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Razer Chroma", () => _surface.Load(RazerDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("SteelSeries", () => _surface.Load(SteelSeriesDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Wooting", () => _surface.Load(WootingDeviceProvider.Instance, RGBDeviceType.All, false));

        // ── Optional OpenRGB server ──────────────────────────────────────────
        if (settings?.OpenRgbEnabled == true)
            TryLoadOpenRgb(settings);

        // ── Windows native LampArray (no vendor software needed) ────────────
        // Run async enumeration synchronously; small timeout to avoid blocking startup.
        try
        {
            var lampTask = LoadLampArrayDevicesAsync();
            lampTask.Wait(TimeSpan.FromSeconds(3));
        }
        catch (Exception ex)
        {
            _initLog.Add($"[SKIP] LampArray: {ex.Message.Split('\n')[0]}");
        }

        _initialized = true;
    }

    private async Task LoadLampArrayDevicesAsync()
    {
        string selector = LampArray.GetDeviceSelector();
        var deviceInfos = await DeviceInformation.FindAllAsync(selector);

        if (deviceInfos.Count == 0)
        {
            _initLog.Add("[SKIP] LampArray: cihaz yok");
            return;
        }

        foreach (var di in deviceInfos)
        {
            try
            {
                var lamp = await LampArray.FromIdAsync(di.Id);
                _lampArrays.Add(lamp);
                _initLog.Add($"[OK] LampArray: {di.Name} ({lamp.LampCount} LED)");
            }
            catch (Exception ex)
            {
                _initLog.Add($"[SKIP] LampArray/{di.Name}: {ex.Message.Split('\n')[0]}");
            }
        }
    }

    private void TryLoadOpenRgb(AppSettings settings)
    {
        TryLoad("OpenRGB", () =>
        {
            var provider = OpenRGBDeviceProvider.Instance;
            provider.DeviceDefinitions.Add(new OpenRGBServerDefinition
            {
                Ip = settings.OpenRgbHost,
                Port = settings.OpenRgbPort,
                ClientName = settings.OpenRgbClientName
            });
            _surface.Load(provider, RGBDeviceType.All, false);
        });
    }

    private void TryLoad(string providerName, Action loadAction)
    {
        try
        {
            loadAction();
            _initLog.Add($"[OK] {providerName}");
        }
        catch (Exception ex)
        {
            _initLog.Add($"[SKIP] {providerName}: {ex.Message.Split('\n')[0]}");
        }
    }

    public List<RgbDevice> GetDevices()
    {
        var devices = new List<RgbDevice>();

        // RGB.NET devices
        foreach (var d in _surface.Devices)
        {
            devices.Add(new RgbDevice
            {
                Backend = RgbBackend.RgbNet,
                Device = d,
                Name = d.DeviceInfo.DeviceName ?? "Bilinmeyen",
                Type = d.DeviceInfo.DeviceType.ToString(),
                Manufacturer = d.DeviceInfo.Manufacturer ?? string.Empty,
                LedCount = d.Count()
            });
        }

        // LampArray devices
        foreach (var lamp in _lampArrays)
        {
            devices.Add(new RgbDevice
            {
                Backend = RgbBackend.LampArray,
                LampArray = lamp,
                Name = $"LampArray ({lamp.LampCount} LED)",
                Type = "LampArray",
                Manufacturer = "Windows Native",
                LedCount = lamp.LampCount
            });
        }

        return devices;
    }

    public void SetDeviceColor(RgbDevice device, WColor color)
    {
        try
        {
            if (device.Backend == RgbBackend.RgbNet && device.Device is not null)
            {
                var nc = ToNetColor(color);
                foreach (var led in device.Device) led.Color = nc;
                _surface.Update();
            }
            else if (device.Backend == RgbBackend.LampArray && device.LampArray is not null)
            {
                var winColor = new Windows.UI.Color { A = 255, R = color.R, G = color.G, B = color.B };
                device.LampArray.SetColor(winColor);
            }

            device.CurrentColor = color;
        }
        catch { }
    }

    public void SetAllDevicesColor(IEnumerable<RgbDevice> devices, WColor color)
    {
        var nc = ToNetColor(color);
        var winColor = new Windows.UI.Color { A = 255, R = color.R, G = color.G, B = color.B };
        bool anyRgbNet = false;

        foreach (var d in devices)
        {
            try
            {
                if (d.Backend == RgbBackend.RgbNet && d.Device is not null)
                {
                    foreach (var led in d.Device) led.Color = nc;
                    anyRgbNet = true;
                }
                else if (d.Backend == RgbBackend.LampArray && d.LampArray is not null)
                {
                    d.LampArray.SetColor(winColor);
                }

                d.CurrentColor = color;
            }
            catch { }
        }

        if (anyRgbNet)
        {
            try { _surface.Update(); } catch { }
        }
    }

    private static NetColor ToNetColor(WColor c) => new(c.R, c.G, c.B);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _surface.Dispose(); } catch { }
    }
}
