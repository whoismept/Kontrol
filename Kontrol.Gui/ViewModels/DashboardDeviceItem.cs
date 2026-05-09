using CommunityToolkit.Mvvm.ComponentModel;

namespace Kontrol.ViewModels;

public partial class DashboardDeviceItem : ObservableObject
{
    public string Name      { get; init; } = "";
    public string TypeLabel { get; init; } = "";

    [ObservableProperty] private string _maxTempText = "—";
    [ObservableProperty] private string _fanRpmText  = "—";
    [ObservableProperty] private double _fanRpm;
    [ObservableProperty] private int    _fanCount;
    [ObservableProperty] private double _fanAngle;
}
