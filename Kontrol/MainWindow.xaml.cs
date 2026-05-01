using Kontrol.Models;
using Kontrol.ViewModels;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Kontrol;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        _vm = new MainViewModel(_settings);
        DataContext = _vm;
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (HardwareViewPanel is null) return;
        if (sender is not RadioButton btn) return;
        if (!int.TryParse(btn.Tag?.ToString(), out var index)) return;

        ShowPanel(index);
    }

    private void ShowPanel(int index)
    {
        HardwareViewPanel.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        FansViewPanel.Visibility     = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        RgbViewPanel.Visibility      = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        SettingsViewPanel.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            ShowInTaskbar = false;
        }
        else
        {
            CleanUp();
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        CleanUp();
        base.OnClosed(e);
    }

    private void CleanUp()
    {
        try { _vm?.Fan?.Dispose(); } catch { }
        try { _vm?.Rgb?.Dispose(); } catch { }
        // HardwareService and FanControllerService are owned by App — not disposed here
    }
}
