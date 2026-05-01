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
using NetColor = RGB.NET.Core.Color;
using WColor = System.Windows.Media.Color;

namespace Kontrol.Services;

public class RgbService : IDisposable
{
    private readonly RGBSurface _surface = new();
    private readonly List<string> _initLog = new();
    private bool _initialized;
    private bool _disposed;

    public IReadOnlyList<string> InitLog => _initLog;

    public void Initialize(AppSettings? settings = null)
    {
        if (_initialized) return;

        TryLoad("ASUS", () => _surface.Load(AsusDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Corsair", () => _surface.Load(CorsairDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("CoolerMaster", () => _surface.Load(CoolerMasterDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Logitech", () => _surface.Load(LogitechDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("MSI", () => _surface.Load(MsiDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Razer", () => _surface.Load(RazerDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("SteelSeries", () => _surface.Load(SteelSeriesDeviceProvider.Instance, RGBDeviceType.All, false));
        TryLoad("Wooting", () => _surface.Load(WootingDeviceProvider.Instance, RGBDeviceType.All, false));

        if (settings?.OpenRgbEnabled == true)
        {
            TryLoadOpenRgb(settings);
        }

        _initialized = true;
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
        return _surface.Devices.Select(d => new RgbDevice
        {
            Device = d,
            Name = d.DeviceInfo.DeviceName ?? "Bilinmeyen",
            Type = d.DeviceInfo.DeviceType.ToString(),
            Manufacturer = d.DeviceInfo.Manufacturer ?? string.Empty,
            LedCount = d.Count()
        }).ToList();
    }

    public void SetDeviceColor(RgbDevice device, WColor color)
    {
        if (device.Device is null) return;
        var nc = ToNetColor(color);
        try
        {
            foreach (var led in device.Device)
                led.Color = nc;
            _surface.Update();
            device.CurrentColor = color;
        }
        catch { }
    }

    public void SetAllDevicesColor(IEnumerable<RgbDevice> devices, WColor color)
    {
        var nc = ToNetColor(color);
        try
        {
            foreach (var d in devices)
            {
                if (d.Device is null) continue;
                foreach (var led in d.Device)
                    led.Color = nc;
                d.CurrentColor = color;
            }
            _surface.Update();
        }
        catch { }
    }

    private static NetColor ToNetColor(WColor c) => new(c.R, c.G, c.B);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _surface.Dispose(); } catch { }
    }
}
