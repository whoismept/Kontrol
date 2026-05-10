using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;


public class AuraUsbProtocol : IHidProtocol
{
    private const byte REPORT_ID    = 0xEC;
    private const byte CMD_DIRECT   = 0x40; // AURA_CONTROL_MODE_DIRECT
    private const byte CMD_FIRMWARE = 0x82; // AURA_REQUEST_FIRMWARE_VERSION
    private const int  LEDS_PER_PKT = 0x14; // 20 LED/paket

    public string ProtocolName => "AuraUSB";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        try
        {
            // Firmware version sorgusu — doğru arayüzü doğrular ve kontrolcüyü uyandırır
            var pkt = new byte[65];
            pkt[0] = REPORT_ID;
            pkt[1] = CMD_FIRMWARE;
            stream.Write(pkt);

            // Yanıt oku (isteğe bağlı — başarısız olursa devam et)
            try
            {
                var resp = new byte[65];
                stream.Read(resp);
            }
            catch { }
        }
        catch { }
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        var ledCount = Math.Max(1, def.LedCount);
        var colors   = new WinColor[ledCount];
        Array.Fill(colors, color);
        SetColors(stream, def, colors);
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        // Zona tanımı yoksa tek zone, channel 0 kabul et
        var zones = def.Zones.Count > 0
            ? def.Zones
            : [new HidZoneDefinition { Name = "Default", LedStart = 0, LedCount = Math.Max(1, def.LedCount), Channel = 0 }];

        foreach (var zone in zones)
        {
            // Kullanıcının gönderdiği colors dizisi boyutunu sınır olarak kullan
            // (HidBackend, UserLedCount'a göre bu diziyi zaten kırpmış olabilir)
            int available   = Math.Max(0, colors.Length - zone.LedStart);
            int zoneLedCount = Math.Min(zone.LedCount, available);
            if (zoneLedCount <= 0) continue;

            SendDirectToChannel(stream, (byte)zone.Channel, colors, zone.LedStart, zoneLedCount);
        }
    }

 
    private static void SendDirectToChannel(
        HidStream stream,
        byte channel,
        WinColor[] colors,
        int ledStart,
        int ledCount)
    {
        if (ledCount <= 0) ledCount = 1;

        int offset = 0;
        while (offset < ledCount)
        {
            int chunkSize = Math.Min(LEDS_PER_PKT, ledCount - offset);
            bool isLast   = (offset + chunkSize >= ledCount);

            var pkt = new byte[65];
            pkt[0] = REPORT_ID;
            pkt[1] = CMD_DIRECT;
            pkt[2] = (byte)((isLast ? 0x80 : 0x00) | channel); // apply flag + channel
            pkt[3] = (byte)(offset & 0xFF);                      // LED offset
            pkt[4] = (byte)chunkSize;                            // LED count in this packet

            for (int i = 0; i < chunkSize; i++)
            {
                int colorIdx = Math.Min(ledStart + offset + i, colors.Length - 1);
                pkt[5 + i * 3] = colors[colorIdx].R;
                pkt[6 + i * 3] = colors[colorIdx].G;
                pkt[7 + i * 3] = colors[colorIdx].B;
            }

            stream.Write(pkt);
            offset += chunkSize;
        }
    }
}
