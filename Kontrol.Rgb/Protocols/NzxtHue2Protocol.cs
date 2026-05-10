using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// NZXT HUE 2 / Kraken X3/Z3 protokolü (VID: 0x2433).
///
/// Protokol özeti (NZXT CAM reverse-engineering / liquidctl kaynaklı):
///   Report boyutu: 64 byte
///   0x28, 0x03 — "Fixed" (static) mod başlıkları
///   LED verisi: her byte sırasıyla G, R, B (dikkat: GRB sırası!)
///   Sonlandırma: 0x28, 0x02 commit paketi
/// </summary>
public class NzxtHue2Protocol : IHidProtocol
{
    public string ProtocolName => "NzxtHue2";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        // NZXT protokolü explicit init gerektirmez
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        var colors = new WinColor[def.LedCount];
        Array.Fill(colors, color);
        SetColors(stream, def, colors);
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        try
        {
            // Mod başlatma paketi
            var startPkt = new byte[64];
            startPkt[0] = 0x28;
            startPkt[1] = 0x03; // fixed/static mod
            stream.Write(startPkt);

            // LED verisini 14'lük parçalar halinde gönder (GRB sırası!)
            int offset = 0;
            bool first = true;
            while (offset < def.LedCount)
            {
                int chunk = Math.Min(14, def.LedCount - offset);
                var pkt = new byte[64];
                pkt[0] = first ? (byte)0x28 : (byte)0x28;
                pkt[1] = 0x03;
                pkt[2] = (byte)(offset & 0xFF);

                for (int i = 0; i < chunk; i++)
                {
                    int src = Math.Min(offset + i, colors.Length - 1);
                    pkt[3 + i * 3] = colors[src].G; // GRB!
                    pkt[4 + i * 3] = colors[src].R;
                    pkt[5 + i * 3] = colors[src].B;
                }

                stream.Write(pkt);
                offset += chunk;
                first = false;
            }

            // Commit paketi
            var commit = new byte[64];
            commit[0] = 0x28;
            commit[1] = 0x02;
            stream.Write(commit);
        }
        catch { }
    }
}
