namespace Kontrol.Fan;

/// <summary>
/// A named snapshot of fan assignments.
/// Preset profiles apply a uniform mode to all fans; custom profiles store per-fan settings.
/// </summary>
public class FanProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;

    /// <summary>True for built-in presets — these cannot be deleted by the user.</summary>
    public bool IsPreset { get; set; }

    /// <summary>
    /// Per-fan assignment snapshots.
    /// Key: FanKey (matches FanAssignment.FanKey).
    /// For presets this is empty — the service applies a uniform mode at apply time.
    /// </summary>
    public Dictionary<string, FanAssignmentSnapshot> Assignments { get; set; } = [];
}

/// <summary>
/// A lightweight snapshot of a single fan's assignment, used inside a FanProfile.
/// </summary>
public class FanAssignmentSnapshot
{
    public FanMode Mode { get; set; } = FanMode.Auto;
    public float ManualPercent { get; set; } = 50f;
    public string? CurveId { get; set; }
    public string? TempSourceId { get; set; }
}
