namespace Kontrol.Fan;

public interface IAlertSettings
{
    bool TempAlertsEnabled { get; }
    float TempAlertThresholdC { get; }
}
