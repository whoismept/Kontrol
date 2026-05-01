using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Kontrol.ViewModels;

public partial class SensorGroup : ObservableObject
{
    public string HardwareName { get; set; } = string.Empty;
    public ObservableCollection<Models.SensorReading> Sensors { get; } = new();
}
