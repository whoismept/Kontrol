using Kontrol.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kontrol.Views;

public partial class RgbView : UserControl
{
    public RgbView()
    {
        InitializeComponent();
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
}
