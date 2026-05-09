using Windows.UI;

namespace Kontrol.Rgb;

public class RgbProfileEntry
{
    public string DeviceName { get; set; } = string.Empty;
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color GetColor() => Color.FromArgb(255, R, G, B);

    public static RgbProfileEntry FromColor(string name, Color c) =>
        new() { DeviceName = name, R = c.R, G = c.G, B = c.B };
}

public class RgbProfile
{
    public string Name { get; set; } = string.Empty;
    public List<RgbProfileEntry> Devices { get; set; } = new();
}
