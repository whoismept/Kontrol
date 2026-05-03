using System.Windows.Controls;
using System.Windows.Input;

namespace Kontrol.Views;

public partial class FanControlView : Page
{
    public FanControlView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.FanConfig;
    }

    private void OnScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
