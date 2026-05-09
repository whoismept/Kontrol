using CommunityToolkit.Mvvm.ComponentModel;
using Kontrol.Fan;
using System.Collections.ObjectModel;

namespace Kontrol.ViewModels;

public partial class SensorGroup : ObservableObject
{
    public string HardwareName { get; set; } = string.Empty;
    public ObservableCollection<SensorReading> Sensors { get; } = new();
}
