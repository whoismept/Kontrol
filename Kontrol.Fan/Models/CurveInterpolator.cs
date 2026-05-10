namespace Kontrol.Fan;

public static class CurveInterpolator
{
    public static float Interpolate(IReadOnlyList<CurvePoint> unsortedPoints, float tempC, FanCurveType type)
    {
        var points = unsortedPoints.OrderBy(p => p.TempC).ToList();
        if (points.Count == 0) return 50f;
        if (points.Count == 1 || type == FanCurveType.Flat) return points[0].Percent;

        if (tempC <= points[0].TempC) return points[0].Percent;
        if (tempC >= points[^1].TempC) return points[^1].Percent;

        for (int i = 0; i < points.Count - 1; i++)
        {
            var lo = points[i];
            var hi = points[i + 1];

            if (tempC >= lo.TempC && tempC < hi.TempC)
            {
                if (type == FanCurveType.Graph) 
                {
                    return lo.Percent;
                }
                
                float range = hi.TempC - lo.TempC;
                if (range <= 0) return lo.Percent;
                float t = (tempC - lo.TempC) / range;
                return lo.Percent + t * (hi.Percent - lo.Percent);
            }
            else if (i == points.Count - 2 && tempC == hi.TempC)
            {
                return hi.Percent;
            }
        }

        return points[^1].Percent;
    }
}
