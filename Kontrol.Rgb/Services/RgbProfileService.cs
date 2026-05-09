using System.IO;
using System.Text.Json;

namespace Kontrol.Rgb;

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
