using Kontrol.Models.Fan;
using System.Collections.Concurrent;

namespace Kontrol.Services;

public class FanCurveEvaluator
{
    private readonly ConcurrentDictionary<string, HysteresisState> _hysteresisStates = new();
    private readonly ConcurrentDictionary<string, SmoothingState> _smoothingStates = new();

    public float Evaluate(FanCurve curve, float tempC, string fanKey)
    {
        if (curve.Type == FanCurveType.Flat && curve.Points.Count > 0)
            return curve.Points[0].Percent;

        float rawPercent = InterpolatePoints(curve, tempC, fanKey);
        return ApplySmoothing(curve, rawPercent, fanKey);
    }

    private float InterpolatePoints(FanCurve curve, float tempC, string fanKey)
    {
        var points = curve.Points.OrderBy(p => p.TempC).ToList();
        if (points.Count == 0) return 50f;
        if (points.Count == 1) return points[0].Percent;

        float effectiveTemp = ApplyHysteresis(curve, tempC, fanKey);

        if (effectiveTemp <= points[0].TempC)
            return points[0].Percent;
        if (effectiveTemp >= points[^1].TempC)
            return points[^1].Percent;

        for (int i = 0; i < points.Count - 1; i++)
        {
            var lo = points[i];
            var hi = points[i + 1];

            if (effectiveTemp >= lo.TempC && effectiveTemp <= hi.TempC)
            {
                float range = hi.TempC - lo.TempC;
                if (range <= 0) return lo.Percent;
                float t = (effectiveTemp - lo.TempC) / range;
                return lo.Percent + t * (hi.Percent - lo.Percent);
            }
        }

        return points[^1].Percent;
    }

    private float ApplyHysteresis(FanCurve curve, float currentTemp, string fanKey)
    {
        if (curve.HysteresisC <= 0) return currentTemp;

        var state = _hysteresisStates.GetOrAdd(fanKey, _ => new HysteresisState { LastTemp = currentTemp });

        if (currentTemp > state.LastTemp)
        {
            state.LastTemp = currentTemp;
        }
        else if (currentTemp < state.LastTemp - curve.HysteresisC)
        {
            state.LastTemp = currentTemp + curve.HysteresisC;
        }

        return state.LastTemp;
    }

    private float ApplySmoothing(FanCurve curve, float targetPercent, string fanKey)
    {
        if (curve.ResponseTimeMs <= 0) return targetPercent;

        var state = _smoothingStates.GetOrAdd(fanKey, _ => new SmoothingState
        {
            CurrentPercent = targetPercent,
            LastUpdate = DateTime.UtcNow
        });

        var now = DateTime.UtcNow;
        var elapsed = (float)(now - state.LastUpdate).TotalMilliseconds;
        state.LastUpdate = now;

        if (elapsed <= 0) return state.CurrentPercent;

        float maxChange = elapsed / curve.ResponseTimeMs * 100f;
        float diff = targetPercent - state.CurrentPercent;

        if (Math.Abs(diff) <= maxChange)
        {
            state.CurrentPercent = targetPercent;
        }
        else
        {
            state.CurrentPercent += Math.Sign(diff) * maxChange;
        }

        return state.CurrentPercent;
    }

    public void ResetState(string fanKey)
    {
        _hysteresisStates.TryRemove(fanKey, out _);
        _smoothingStates.TryRemove(fanKey, out _);
    }

    public void ResetAll()
    {
        _hysteresisStates.Clear();
        _smoothingStates.Clear();
    }

    private class HysteresisState
    {
        public float LastTemp;
    }

    private class SmoothingState
    {
        public float CurrentPercent;
        public DateTime LastUpdate;
    }
}
