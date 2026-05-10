using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// Corsair HID protokolü (eski nesil Corsair ürünleri için basit implementasyon).
///
/// Kaynak: OpenRGB Controllers/CorsairController
///
/// Kritik bilgiler:
///   - HID Report ID: 0x00
///   - Paket başına 64 veri baytı (65 bayt toplam, ilki 0x00)
///   - Static renk için komut: 0x01 (SetSingleColor benzeri)
///   - Commit için: 0x35 komutu
/// </summary>
public class CorsairHidProtocol : IHidProtocol
{
    public string ProtocolName => "CorsairHid";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        // Corsair cihazları genellikle ek init gerektirmez
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        // Basit static renk paketi
        var buf = new byte[65];
        buf[0x00] = 0x00; // Report ID
        buf[0x01] = 0x01; // SetColor command
        buf[0x02] = color.R;
        buf[0x03] = color.G;
        buf[0x04] = color.B;

        try { stream.Write(buf); } catch { }
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        SetColor(stream, def, colors.Length > 0 ? colors[0] : default);
    }
}
