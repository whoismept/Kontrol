using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace Kontrol.Services;

public class RgbProfileEntry
{
    public string DeviceName { get; set; } = string.Empty;
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color GetColor() => Color.FromRgb(R, G, B);

    public static RgbProfileEntry FromColor(string name, Color c) =>
        new() { DeviceName = name, R = c.R, G = c.G, B = c.B };
}

public class RgbProfile
{
    public string Name { get; set; } = string.Empty;
    public List<RgbProfileEntry> Devices { get; set; } = new();
}

public class RgbProfileService
{
    private static readonly string ProfilesDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kontrol", "rgb_profiles");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public RgbProfileService()
    {
        try { Directory.CreateDirectory(ProfilesDir); } catch { }
    }

    public List<string> ListProfiles()
    {
        try
        {
            if (!Directory.Exists(ProfilesDir)) return new();
            return Directory.GetFiles(ProfilesDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .OrderBy(s => s)
                .ToList();
        }
        catch { return new(); }
    }

    public RgbProfile? Load(string name)
    {
        var path = GetPath(name);
        if (!File.Exists(path)) return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RgbProfile>(json);
        }
        catch { return null; }
    }

    public void Save(RgbProfile profile)
    {
        try
        {
            Directory.CreateDirectory(ProfilesDir);
            File.WriteAllText(GetPath(profile.Name), JsonSerializer.Serialize(profile, Json));
        }
        catch { }
    }

    public void Delete(string name)
    {
        try { File.Delete(GetPath(name)); } catch { }
    }

    private static string GetPath(string name)
    {
        var safe = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(ProfilesDir, safe + ".json");
    }
}
