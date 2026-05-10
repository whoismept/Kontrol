using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// Gigabyte RGB Fusion 2.0 USB protokolü — OpenRGB kaynak koduna dayalı.
///
/// Kaynak: Controllers/GigabyteRGBFusion2USBController/GigabyteRGBFusion2USBController.cpp
///
/// Kritik bilgiler:
///   - HID FEATURE reports kullanır (hid_send_feature_report / hid_get_feature_report)
///     HidSharp'ta: stream.SetFeature() / stream.GetFeature()
///   - Report ID: cihazdan dinamik okunur (0x60 sorgusundan)
///   - Reset: 0x20–0x27 arasındaki registerlara 0 yaz
///   - Zone efekti: PktEffect yapısı ile (ham byte paketi)
///   - Apply: hızlı apply = 0x28 komutu
///   - VID: 0x048D
///
/// Basitleştirilmiş implementasyon: static renk için zone 0 ve 1'e 0x21 static modu yazar.
/// </summary>
public class RgbFusion2Protocol : IHidProtocol
{
    private const int  BUF_SIZE       = 64;
    private const byte DEFAULT_REPORT = 0x60; // varsayılan report ID

    // Zone efekt komutları (IT8297 chip register adresleri)
    private static readonly byte[] ZONE_REGISTERS = [0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27];

    public string ProtocolName => "RgbFusion2";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        try
        {
            // Controller reset — tüm zone registerlarını sıfırla
            ResetController(stream);
        }
        catch { }
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        try
        {
            var zones = def.Zones.Count > 0
                ? def.Zones
                : [new HidZoneDefinition { Name = "D_LED1", LedStart = 0, LedCount = 1, Channel = 0 }];

            foreach (var zone in zones)
            {
                SetZoneStaticColor(stream, ZONE_REGISTERS[Math.Min(zone.Channel, ZONE_REGISTERS.Length - 1)], color);
            }

            // Hızlı apply (fast_apply = true)
            FastApply(stream);
        }
        catch { }
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        // RGB Fusion 2 zone bazlı; per-LED desteklemez (zone başına tek renk)
        SetColor(stream, def, colors.Length > 0 ? colors[0] : default);
    }

    /// <summary>Zone'a static renk atar (Effect type = 1 = STATIC).</summary>
    private static void SetZoneStaticColor(HidStream stream, byte zoneRegister, WinColor color)
    {
        // PktEffect yapısı (basitleştirilmiş):
        // buf[0] = report_id (0x60)
        // buf[1] = zone_register (0x20-0x27)
        // buf[2] = effect_type (0x01 = static)
        // buf[3] = 0x01 (max_brightness)
        // buf[4..7] = color (BGR format in uint32)
        var buf = new byte[BUF_SIZE];
        buf[0] = DEFAULT_REPORT;
        buf[1] = zoneRegister;  // zone register adresi
        buf[2] = 0x01;          // EFFECT_STATIC
        buf[3] = 0xFF;          // max brightness
        buf[4] = color.B;       // RGB Fusion 2 renk sırası: BGR
        buf[5] = color.G;
        buf[6] = color.R;
        buf[7] = 0xFF;          // period0 low byte (static için önemsiz)

        stream.SetFeature(buf);
    }

    /// <summary>Controller registerlarını sıfırla (0x20–0x27).</summary>
    private static void ResetController(HidStream stream)
    {
        for (byte reg = 0x20; reg <= 0x27; reg++)
        {
            var buf = new byte[BUF_SIZE];
            buf[0] = DEFAULT_REPORT;
            buf[1] = reg;
            stream.SetFeature(buf);
        }
        FastApply(stream);
    }

    /// <summary>0x28 komutunu göndererek değişiklikleri cihaza uygular.</summary>
    private static void FastApply(HidStream stream)
    {
        var buf = new byte[BUF_SIZE];
        buf[0] = DEFAULT_REPORT;
        buf[1] = 0x28;
        buf[2] = 0xFF;
        buf[3] = 0x00;
        stream.SetFeature(buf);
    }
}
