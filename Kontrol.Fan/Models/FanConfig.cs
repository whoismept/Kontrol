namespace Kontrol.Fan;

public class FanConfig
{
    public List<TempSource> TempSources { get; set; } = new();
    public List<FanCurve> Curves { get; set; } = new();
    public List<FanAssignment> Assignments { get; set; } = new();
}
