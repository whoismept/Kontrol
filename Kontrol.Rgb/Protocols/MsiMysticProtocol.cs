using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// MSI Mystic Light HID protokolü (VID: 0x0483).
///
/// Protokol özeti (MSI Center reverse-engineering / OpenRGB kaynaklı):
///   Report boyutu: 64 byte
///   0x01 — "Set effect" komutu
///   Byte[1]: kanal (0x30 = D_LED1, 0x31 = D_LED2)
///   Byte[2]: mod (0x01 = static)
///   Byte[3..5]: R, G, B
///   0x02 — Apply
/// </summary>
public class MsiMysticProtocol : IHidProtocol
{
    public string ProtocolName => "MsiMystic";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def) { }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        try
        {
            var zones = def.Zones.Count > 0
                ? def.Zones
                : [new HidZoneDefinition { Name = "Default", LedStart = 0, LedCount = 1, Channel = 0 }];

            foreach (var zone in zones)
            {
                var pkt = new byte[64];
                pkt[0] = 0x01;                          // set effect
                pkt[1] = (byte)(0x30 + zone.Channel);   // kanal
                pkt[2] = 0x01;                          // static mod
                pkt[3] = color.R;
                pkt[4] = color.G;
                pkt[5] = color.B;
                pkt[6] = 0xFF;                          // brightness
                stream.Write(pkt);
            }

            // Apply
            var apply = new byte[64];
            apply[0] = 0x02;
            stream.Write(apply);
        }
        catch { }
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        var color = colors.Length > 0 ? colors[0] : default;
        SetColor(stream, def, color);
    }
}
