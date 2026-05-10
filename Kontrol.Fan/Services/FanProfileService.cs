using System.IO;
using System.Text.Json;

namespace Kontrol.Fan;

/// <summary>
/// Manages fan profiles — both built-in presets and user-created custom profiles.
///
/// Preset profiles apply a uniform mode to every fan assignment in the config.
/// Custom profiles store a per-fan snapshot and restore it exactly.
///
/// Profiles are persisted to %APPDATA%\Kontrol\fan_profiles\.
/// Presets are always generated in memory and never written to disk.
/// </summary>
public class FanProfileService
{
    public const string PresetAutoId        = "preset:auto";
    public const string PresetSilentId      = "preset:silent";
    public const string PresetBalancedId    = "preset:balanced";
    public const string PresetPerformanceId = "preset:performance";

    private static readonly string ProfileDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kontrol", "fan_profiles");

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    // -------------------------------------------------------------------------
    // Preset definitions
    // -------------------------------------------------------------------------

    public static IReadOnlyList<FanProfile> Presets { get; } =
    [
        new() { Id = PresetAutoId,        Name = "Auto (BIOS)",  IsPreset = true },
        new() { Id = PresetSilentId,      Name = "Silent",       IsPreset = true },
        new() { Id = PresetBalancedId,    Name = "Balanced",     IsPreset = true },
        new() { Id = PresetPerformanceId, Name = "Performance",  IsPreset = true },
    ];

    // -------------------------------------------------------------------------
    // Custom profile I/O
    // -------------------------------------------------------------------------

    public List<FanProfile> LoadCustom()
    {
        var result = new List<FanProfile>();
        try
        {
            if (!Directory.Exists(ProfileDir)) return result;
            foreach (var file in Directory.GetFiles(ProfileDir, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonSerializer.Deserialize<FanProfile>(json, JsonOpts);
                    if (profile is not null && !profile.IsPreset)
                        result.Add(profile);
                }
                catch { }
            }
        }
        catch { }
        return result;
    }

    public void Save(FanProfile profile)
    {
        if (profile.IsPreset) return;
        try
        {
            Directory.CreateDirectory(ProfileDir);
            var safe = string.Concat(profile.Id.Split(Path.GetInvalidFileNameChars()));
            File.WriteAllText(
                Path.Combine(ProfileDir, $"{safe}.json"),
                JsonSerializer.Serialize(profile, JsonOpts));
        }
        catch { }
    }

    public void Delete(FanProfile profile)
    {
        if (profile.IsPreset) return;
        try
        {
            var safe = string.Concat(profile.Id.Split(Path.GetInvalidFileNameChars()));
            var path = Path.Combine(ProfileDir, $"{safe}.json");
            if (File.Exists(path)) File.Delete(path);
        }
        catch { }
    }

    // -------------------------------------------------------------------------
    // Snapshot creation
    // -------------------------------------------------------------------------

    /// <summary>Creates a custom profile snapshot from the current fan configuration.</summary>
    public FanProfile CreateSnapshot(string name, FanConfig config)
    {
        var profile = new FanProfile { Name = name };
        foreach (var a in config.Assignments)
        {
            profile.Assignments[a.FanKey] = new FanAssignmentSnapshot
            {
                Mode          = a.Mode,
                ManualPercent = a.ManualPercent,
                CurveId       = a.CurveId,
                TempSourceId  = a.TempSourceId,
            };
        }
        return profile;
    }

    // -------------------------------------------------------------------------
    // Profile application
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a profile to the given config in-place.
    /// Call FanControllerService.UpdateConfig() afterwards to persist and apply changes.
    /// </summary>
    public void ApplyProfile(FanProfile profile, FanConfig config)
    {
        if (profile.IsPreset)
            ApplyPreset(profile.Id, config);
        else
            ApplyCustom(profile, config);
    }

    private static void ApplyPreset(string presetId, FanConfig config)
    {
        // Find the best default temp source (prefer cpu_max)
        var tempSourceId = config.TempSources
            .OrderBy(s => s.Id == "cpu_max" ? 0 : 1)
            .FirstOrDefault()?.Id;

        foreach (var a in config.Assignments)
        {
            switch (presetId)
            {
                case PresetAutoId:
                    a.Mode = FanMode.Auto;
                    break;

                case PresetSilentId:
                    a.Mode        = FanMode.Curve;
                    a.CurveId     = FindCurveId(config, "silent");
                    a.TempSourceId = tempSourceId;
                    break;

                case PresetBalancedId:
                    a.Mode         = FanMode.Curve;
                    a.CurveId      = FindCurveId(config, "balanced");
                    a.TempSourceId = tempSourceId;
                    break;

                case PresetPerformanceId:
                    a.Mode         = FanMode.Curve;
                    a.CurveId      = FindCurveId(config, "performance");
                    a.TempSourceId = tempSourceId;
                    break;
            }
        }
    }

    private static void ApplyCustom(FanProfile profile, FanConfig config)
    {
        foreach (var a in config.Assignments)
        {
            if (!profile.Assignments.TryGetValue(a.FanKey, out var snap)) continue;
            a.Mode          = snap.Mode;
            a.ManualPercent = snap.ManualPercent;
            a.CurveId       = snap.CurveId;
            a.TempSourceId  = snap.TempSourceId;
        }
    }

    private static string? FindCurveId(FanConfig config, string preferredId)
    {
        return config.Curves.FirstOrDefault(c => c.Id == preferredId)?.Id
            ?? config.Curves.FirstOrDefault()?.Id;
    }
}
