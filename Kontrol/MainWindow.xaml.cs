using Kontrol.Models;
using Kontrol.ViewModels;
using Kontrol.Views;
using System.Windows;
using Wpf.Ui.Controls;

namespace Kontrol;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();
        App.SnackbarPresenter = null;
        _settings = settings;
        _vm = new MainViewModel(_settings);
        App.MainVm = _vm;
        DataContext = _vm;

        Loaded += (_, _) => MainNav.Navigate(typeof(FanView));
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
    }
}
