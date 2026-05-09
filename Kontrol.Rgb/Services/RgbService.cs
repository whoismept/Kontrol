using RGB.NET.Core;
using RGB.NET.Devices.Asus;
using RGB.NET.Devices.Corsair;
using RGB.NET.Devices.Logitech;
using RGB.NET.Devices.OpenRGB;
using RGB.NET.Devices.Razer;
using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using Windows.UI;
using NetColor = RGB.NET.Core.Color;

namespace Kontrol.Rgb;

public class RgbService : IDisposable
{
    private readonly RGBSurface _surface = new();
    private readonly List<LampArray> _lampArrays = new();
    private readonly List<string> _initLog = new();
    private bool _initialized;
    private bool _disposed;

    public IReadOnlyList<string> InitLog => _initLog;

    public async Task InitializeAsync(IOpenRgbSettings? settings = null)
    {
        if (_initialized) return;

        await Task.Run(() =>
        {
            TryLoad("ASUS Aura", () => _surface.Load(AsusDeviceProvider.Instance, RGBDeviceType.All, false));
            TryLoad("Corsair iCUE", () => _surface.Load(CorsairDeviceProvider.Instance, RGBDeviceType.All, false));
            TryLoad("Logitech", () => _surface.Load(LogitechDeviceProvider.Instance, RGBDeviceType.All, false));
            TryLoad("Razer Chroma", () => _surface.Load(RazerDeviceProvider.Instance, RGBDeviceType.All, false));

            if (settings?.OpenRgbEnabled == true)
                TryLoadOpenRgb(settings);
        });

        await LoadLampArrayDevicesAsync();

        _initialized = true;
    }

    private async Task LoadLampArrayDevicesAsync()
    {
        string selector = LampArray.GetDeviceSelector();
        var deviceInfos = await DeviceInformation.FindAllAsync(selector);

        if (deviceInfos.Count == 0)
        {
            _initLog.Add("[SKIP] LampArray: no device");
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

    private void TryLoadOpenRgb(IOpenRgbSettings settings)
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

        foreach (var d in _surface.Devices)
        {
            devices.Add(new RgbDevice
            {
                Backend = RgbBackend.RgbNet,
                Device = d,
                Name = d.DeviceInfo.DeviceName ?? "Unknown",
                Type = d.DeviceInfo.DeviceType.ToString(),
                Manufacturer = d.DeviceInfo.Manufacturer ?? string.Empty,
                LedCount = d.Count()
            });
        }

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

    public void SetDeviceColor(RgbDevice device, Windows.UI.Color color)
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
                device.LampArray.SetColor(color);
            }

            device.CurrentColor = color;
        }
        catch { }
    }

    public void SetAllDevicesColor(IEnumerable<RgbDevice> devices, Windows.UI.Color color)
    {
        var nc = ToNetColor(color);
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
                    d.LampArray.SetColor(color);
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

    private static NetColor ToNetColor(Windows.UI.Color c) => new(c.R, c.G, c.B);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _surface.Dispose(); } catch { }
    }
}
