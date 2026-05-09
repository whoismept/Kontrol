using System.Collections.ObjectModel;

namespace Kontrol.Fan;

public enum FanCurveType
{
    Graph,
    Linear,
    Flat
}

public class CurvePoint
{
    public float TempC { get; set; }
    public float Percent { get; set; }
}

public class FanCurve
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public FanCurveType Type { get; set; } = FanCurveType.Graph;
    public ObservableCollection<CurvePoint> Points { get; set; } = new();
    public float HysteresisC { get; set; } = 2f;
    public int ResponseTimeMs { get; set; } = 1000;
}
