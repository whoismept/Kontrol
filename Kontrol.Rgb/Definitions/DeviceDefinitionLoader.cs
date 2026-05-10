using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Kontrol.Rgb.Definitions;

/// <summary>
/// Gömülü (builtin) ve kullanıcı tanımlı JSON dosyalarından cihaz tanımlarını yükler.
/// Kullanıcı tanımları: %APPDATA%\Kontrol\hid_devices\*.json
/// </summary>
public static class DeviceDefinitionLoader
{
    private static readonly string UserDefsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kontrol", "hid_devices");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Tüm tanımları yükler (builtin + kullanıcı).</summary>
    public static List<HidDeviceDefinition> LoadAll()
    {
        var defs = new Dictionary<string, HidDeviceDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in LoadBuiltin())
            defs[d.Id] = d;

        // Kullanıcı tanımları builtin'i override edebilir
        foreach (var d in LoadUserDefined())
            defs[d.Id] = d;

        return [.. defs.Values];
    }

    private static IEnumerable<HidDeviceDefinition> LoadBuiltin()
    {
        var asm = Assembly.GetExecutingAssembly();
        var prefix = $"{asm.GetName().Name}.Definitions.Builtin.";

        foreach (var resName in asm.GetManifestResourceNames())
        {
            if (!resName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            if (!resName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;

            using var stream = asm.GetManifestResourceStream(resName);
            if (stream is null) continue;

            foreach (var def in ParseStream(stream, resName))
                yield return def;
        }
    }

    private static IEnumerable<HidDeviceDefinition> LoadUserDefined()
    {
        if (!Directory.Exists(UserDefsDir)) yield break;

        foreach (var file in Directory.GetFiles(UserDefsDir, "*.json"))
        {
            using var stream = File.OpenRead(file);
            foreach (var def in ParseStream(stream, file))
                yield return def;
        }
    }

    private static IEnumerable<HidDeviceDefinition> ParseStream(Stream stream, string source)
    {
        // yield return cannot be inside try/catch — collect first, then yield
        var collected = new List<HidDeviceDefinition>();
        try
        {
            using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            });
            var root = doc.RootElement;

            // Dosya format: { "devices": [...] }  veya  [...]
            JsonElement arr = root.ValueKind == JsonValueKind.Array
                ? root
                : root.TryGetProperty("devices", out var devArr) ? devArr : default;

            if (arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var def = item.Deserialize<HidDeviceDefinition>(JsonOpts);
                    if (def is not null && !string.IsNullOrWhiteSpace(def.Id))
                        collected.Add(def);
                }
            }
        }
        catch { /* Bozuk JSON — sessizce atla */ }

        return collected;
    }
}
