using HidSharp;
using Kontrol.Rgb.Definitions;
using WinColor = Windows.UI.Color;

namespace Kontrol.Rgb.Protocols;

/// <summary>
/// Kullanıcı tanımlı "ham" HID protokolü.
/// JSON tanımındaki <see cref="RawProtocolDefinition"/> alanlarından komutu oluşturur.
///
/// ColorCommand içinde "{R}", "{G}", "{B}" token'ları renk değerleriyle değiştirilir.
/// Örnek JSON:
/// "rawProtocol": {
///   "colorCommand": ["0x00","0xB0","0x00","0x00","{R}","{G}","{B}"],
///   "reportSize": 65
/// }
/// </summary>
public class RawHidProtocol : IHidProtocol
{
    public string ProtocolName => "Raw";

    public void Initialize(HidDevice device, HidStream stream, HidDeviceDefinition def)
    {
        if (def.RawProtocol?.InitCommand is { } init)
        {
            try
            {
                var pkt = PadToSize(init.ToArray(), def.RawProtocol.ReportSize);
                stream.Write(pkt);
            }
            catch { }
        }
    }

    public void SetColor(HidStream stream, HidDeviceDefinition def, WinColor color)
    {
        if (def.RawProtocol is null) return;
        try
        {
            var cmd = BuildColorPacket(def.RawProtocol, color.R, color.G, color.B);
            stream.Write(cmd);

            if (def.RawProtocol.ApplyCommand is { } apply)
                stream.Write(PadToSize(apply.ToArray(), def.RawProtocol.ReportSize));
        }
        catch { }
    }

    public void SetColors(HidStream stream, HidDeviceDefinition def, WinColor[] colors)
    {
        // Raw protokol per-LED rengi desteklemiyor; ilk rengi kullan
        var color = colors.Length > 0 ? colors[0] : default;
        SetColor(stream, def, color);
    }

    private static byte[] BuildColorPacket(RawProtocolDefinition raw, byte r, byte g, byte b)
    {
        var bytes = new List<byte>();
        foreach (var token in raw.ColorCommand)
        {
            bytes.Add(token.Trim() switch
            {
                "{R}" => r,
                "{G}" => g,
                "{B}" => b,
                var s when s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    => Convert.ToByte(s[2..], 16),
                var s => byte.TryParse(s, out var v) ? v : (byte)0,
            });
        }
        return PadToSize([.. bytes], raw.ReportSize);
    }

    private static byte[] PadToSize(byte[] src, int size)
    {
        if (src.Length >= size) return src[..size];
        var padded = new byte[size];
        Array.Copy(src, padded, src.Length);
        return padded;
    }
}
