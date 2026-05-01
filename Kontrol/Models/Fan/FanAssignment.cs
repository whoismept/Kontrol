namespace Kontrol.Models.Fan;

public enum FanMode
{
    Auto,
    ManualConstant,
    Curve,
    Off
}

public class FanAssignment
{
    public string FanKey { get; set; } = string.Empty;
    public FanMode Mode { get; set; } = FanMode.Auto;
    public float ManualPercent { get; set; } = 50f;
    public string? CurveId { get; set; }
    public string? TempSourceId { get; set; }
    public float MinPercent { get; set; } = 20f;
    public float MaxPercent { get; set; } = 100f;
    public float? ZeroRpmBelowC { get; set; }
}
