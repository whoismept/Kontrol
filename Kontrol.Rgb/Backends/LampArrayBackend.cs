using Windows.Devices.Enumeration;
using Windows.Devices.Lights;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Backends;

/// <summary>
/// Windows.Devices.Lights.LampArray (Windows Dynamic Lighting) backend.
/// Win10 2004+ üzerinde standart USB HID LampArray cihazlarını destekler.
/// Kurulum gerektirmez; işletim sistemi doğrudan sürücüyü sağlar.
/// </summary>
public sealed class LampArrayBackend : ILedBackend
{
    private readonly List<LampArray> _openArrays = [];
    private bool _disposed;

    public string Name => "LampArray";

    public async Task<List<RgbDevice>> DiscoverDevicesAsync()
    {
        var result = new List<RgbDevice>();
        try
        {
            var selector = LampArray.GetDeviceSelector();
            var deviceInfoList = await DeviceInformation.FindAllAsync(selector);

            foreach (var info in deviceInfoList)
            {
                try
                {
                    var lampArray = await LampArray.FromIdAsync(info.Id);
                    if (lampArray is null) continue;

                    _openArrays.Add(lampArray);
                    result.Add(new RgbDevice
                    {
                        Id          = $"lamparray:{info.Id}",
                        Name        = info.Name,
                        Vendor      = ExtractVendor(info.Name),
                        Type        = lampArray.LampArrayKind.ToString(),
                        LedCount    = lampArray.LampCount,
                        BackendName = Name,
                        BackendData = lampArray,
                    });
                }
                catch { /* cihaz açılamadı, atla */ }
            }
        }
        catch { /* LampArray enum hatası, sessizce devam */ }

        return result;
    }

    public void SetColor(RgbDevice device, WinColor color)
    {
        if (device.BackendData is not LampArray la) return;
        if (!la.IsConnected) return;
        try
        {
            int count = la.LampCount;
            var indices = new int[count];
            var colors  = new WinColor[count];
            for (int i = 0; i < count; i++) { indices[i] = i; colors[i] = color; }
            la.SetColorsForIndices(colors, indices);
            device.CurrentColor = color;
        }
        catch { }
    }

    public void SetColors(RgbDevice device, WinColor[] colors)
    {
        if (device.BackendData is not LampArray la) return;
        if (!la.IsConnected) return;
        try
        {
            int count = Math.Min(colors.Length, la.LampCount);
            var indices = new int[count];
            var winColors = new WinColor[count];
            for (int i = 0; i < count; i++) { indices[i] = i; winColors[i] = colors[i]; }
            la.SetColorsForIndices(winColors, indices);
            if (colors.Length > 0) device.CurrentColor = colors[0];
        }
        catch { }
    }

    private static string ExtractVendor(string name)
    {
        var parts = name.Split(' ');
        return parts.Length > 0 ? parts[0] : "Unknown";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // LampArray is not IDisposable; references are released by GC
        _openArrays.Clear();
    }
}
