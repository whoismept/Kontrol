using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;


public class ASRockPolyProtocol : IHidProtocol
{
    private const byte REPORT_ID = 0x00;
    private const byte CMD_SET_ZONE = 0x10; // POLYCHROME_USB_SET_ZONE
    private const byte CMD_COMMIT   = 0x12; // POLYCHROME_USB_COMMIT
    private const byte MODE_STATIC  = 0x01; // Static color mode

    public string ProtocolName => "ASRockPoly";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        // ASRock init gerekmez — doğrudan zone yazılabilir
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        var zones = def.Zones.Count > 0
            ? def.Zones
            : [new HidZoneDefinition { Name = "Default", LedStart = 0, LedCount = 1, Channel = 0 }];

        foreach (var zone in zones)
            WriteZone(stream, (byte)zone.Channel, MODE_STATIC, color, allZones: false);

        Commit(stream);
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        // ASRock zone bazlı çalışır; colors dizisinin boyutuna göre aktif zone sayısını sınırla
        var zones = def.Zones.Count > 0
            ? def.Zones
            : [new HidZoneDefinition { Name = "Default", LedStart = 0, LedCount = 1, Channel = 0 }];

        foreach (var zone in zones)
        {
            if (zone.LedStart >= colors.Length) break;
            var color = colors[zone.LedStart];
            WriteZone(stream, (byte)zone.Channel, MODE_STATIC, color, allZones: false);
        }

        Commit(stream);
    }


    private static void WriteZone(HidStream stream, byte zoneType, byte mode, WinColor color, bool allZones)
    {
        var buf = new byte[65];
        buf[0x00] = REPORT_ID;
        buf[0x01] = CMD_SET_ZONE;
        buf[0x02] = 0x00;
        buf[0x03] = zoneType;
        buf[0x04] = mode;
        buf[0x05] = color.G; // G ve R ters! (ASRock GRSwap davranışı)
        buf[0x06] = color.R;
        buf[0x07] = color.B;
        buf[0x08] = 0x00;    // speed = 0 for static
        buf[0x09] = 0xFF;
        buf[0x10] = allZones ? (byte)1 : (byte)0;

        stream.Write(buf);

        // Yanıtı oku
        try
        {
            var resp = new byte[64];
            stream.Read(resp);
        }
        catch { }
    }

    private static void Commit(HidStream stream)
    {
        var buf = new byte[65];
        buf[0x00] = REPORT_ID;
        buf[0x01] = CMD_COMMIT;
        stream.Write(buf);
        try
        {
            var resp = new byte[64];
            stream.Read(resp);
        }
        catch { }
    }
}
