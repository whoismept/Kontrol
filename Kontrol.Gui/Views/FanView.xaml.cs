using Kontrol.Services;
using Microsoft.UI.Xaml.Controls;

namespace Kontrol.Views;

public sealed partial class FanView : Page
{
    public FanView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.Fan;
        PageTitle.Text = Loc.Get("PageHardwareMonitoring");
        SecTemps.Text = Loc.Get("SecTemperatures");
        SecFans.Text = Loc.Get("SecFanSpeeds");
        SecLoad.Text = Loc.Get("SecLoad");
        ToolTipService.SetToolTip(RefreshBtn, Loc.Get("BtnRefresh"));
    }
}
