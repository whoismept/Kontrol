using Kontrol.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Kontrol.Views;

public partial class RgbView : Page
{
    public RgbView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.Rgb;
    }

    private void OnPresetColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex && DataContext is RgbViewModel vm)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                vm.SelectedColor = color;
            }
            catch { }
        }
    }

    private void OnScrollPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var sv = (ScrollViewer)sender;
        sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
