using Kontrol.Models;
using Kontrol.Models.Fan;

namespace Kontrol.Services;

public class TempSourceEvaluator
{
    public float? Evaluate(TempSource source, IReadOnlyList<SensorReading> readings)
    {
        var matched = new List<float>();

        foreach (var reading in readings)
        {
            if (reading.Category != SensorCategory.Temperature) continue;

            foreach (var sref in source.SensorRefs)
            {
                if (WildcardMatch(reading.HardwareName, sref.HardwareName) &&
                    WildcardMatch(reading.SensorName, sref.SensorName))
                {
                    matched.Add(reading.Value);
                    break;
                }
            }
        }

        if (matched.Count == 0) return null;

        return source.Mode switch
        {
            TempSourceMode.Single => matched[0],
            TempSourceMode.Max => matched.Max(),
            TempSourceMode.Min => matched.Min(),
            TempSourceMode.Average => matched.Average(),
            _ => matched.Max()
        };
    }

    private static bool WildcardMatch(string text, string pattern)
    {
        if (pattern == "*") return true;

        if (pattern.StartsWith('*') && pattern.EndsWith('*') && pattern.Length > 2)
        {
            var inner = pattern[1..^1];
            return text.Contains(inner, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith('*'))
            return text.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);

        if (pattern.EndsWith('*'))
            return text.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);

        return string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);
    }
}
