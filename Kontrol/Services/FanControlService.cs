using Kontrol.Models;
using LibreHardwareMonitor.Hardware;

namespace Kontrol.Services;

public class FanControlService
{
    public List<FanControl> DiscoverControllableFans(IEnumerable<IHardware> hardware)
    {
        var fans = new List<FanControl>();

        foreach (var hw in hardware)
        {
            CollectFans(hw, fans);
            foreach (var sub in hw.SubHardware)
                CollectFans(sub, fans);
        }

        return fans;
    }

    private static void CollectFans(IHardware hardware, List<FanControl> fans)
    {
        try { hardware.Update(); } catch { }

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Control) continue;
            if (sensor.Control is null) continue;

            var fanRpmSensor = FindMatchingFanSensor(hardware, sensor);

            fans.Add(new FanControl
            {
                Name = sensor.Name,
                HardwareName = hardware.Name,
                Control = sensor.Control,
                ControlSensor = sensor,
                FanSensor = fanRpmSensor,
            });
        }
    }

    private static ISensor? FindMatchingFanSensor(IHardware hardware, ISensor controlSensor)
    {
        var fans = hardware.Sensors.Where(s => s.SensorType == SensorType.Fan).ToList();
        if (fans.Count == 0) return null;

        var match = fans.FirstOrDefault(f => f.Index == controlSensor.Index);
        if (match is not null) return match;

        match = fans.FirstOrDefault(f =>
            ExtractNumber(f.Name) is int n1 &&
            ExtractNumber(controlSensor.Name) is int n2 &&
            n1 == n2);
        if (match is not null) return match;

        return fans.Count == 1 ? fans[0] : null;
    }

    private static int? ExtractNumber(string text)
    {
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : null;
    }

    public void SetSpeed(FanControl fan, float percent)
    {
        if (fan.Control is null) return;
        var clamped = Math.Clamp(percent, fan.MinValue, fan.MaxValue);
        try { fan.Control.SetSoftware(clamped); } catch { }
    }

    public void SetAuto(FanControl fan)
    {
        if (fan.Control is null) return;
        try { fan.Control.SetDefault(); } catch { }
    }

    public void RefreshFanState(FanControl fan)
    {
        try
        {
            float? rpm = fan.FanSensor?.Value;
            float? percent = fan.ControlSensor?.Value;
            ControlMode mode = fan.Control?.ControlMode ?? ControlMode.Default;
            fan.UpdateFromHardware(percent, rpm, mode);
        }
        catch { }
    }
}
