using System.Collections.Concurrent;

namespace Kontrol.Fan;

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
        float effectiveTemp = ApplyHysteresis(curve, tempC, fanKey);
        return CurveInterpolator.Interpolate(curve.Points, effectiveTemp, curve.Type);
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
