using Kontrol.Fan;
using Microsoft.Win32;
using System.IO;
using System.Text.Json;

namespace Kontrol.Models;

public class AppSettings : IAlertSettings
{
    public int PollingIntervalMs { get; set; } = 2000;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool LaunchOnWindowsStartup { get; set; } = false;
    public bool TempAlertsEnabled { get; set; } = true;
    public float TempAlertThresholdC { get; set; } = 90f;
    public string Theme { get; set; } = "Dark"; // Dark, Light, System
    public string Language { get; set; } = "en";
    public bool AdvancedMode { get; set; } = false;

    /// <summary>
    /// ID of the currently active fan profile (preset or custom).
    /// Matches FanProfile.Id. Empty = no active profile (manual state).
    /// </summary>
    public string ActiveFanProfileId { get; set; } = string.Empty;

    /// <summary>Per-device LED count overrides. Key: RgbDevice.Id, Value: user-specified count.</summary>
    public Dictionary<string, int> RgbDeviceLedCounts { get; set; } = [];

    /// <summary>
    /// Per-zone LED count overrides.
    /// Key format: "{RgbDevice.Id}:ch{zone.Channel}", Value: user-specified count.
    /// Used for resizable ARGB zones where the LED count depends on the connected strip.
    /// </summary>
    public Dictionary<string, int> RgbZoneLedCounts { get; set; } = [];

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Kontrol", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }

        ApplyStartupRegistry();
    }

    private void ApplyStartupRegistry()
    {
        const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "Kontrol";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(keyPath, writable: true);
            if (key is null) return;

            if (LaunchOnWindowsStartup)
            {
                var exePath = Environment.ProcessPath ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath is not null)
                    key.SetValue(valueName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(valueName, throwOnMissingValue: false);
            }
        }
        catch { }
    }
}
