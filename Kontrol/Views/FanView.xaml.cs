using System.Windows.Controls;
using System.Windows.Input;

namespace Kontrol.Views;

public partial class FanView : Page
{
    public FanView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.Fan;
    }

    private void OnScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
