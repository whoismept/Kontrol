using Kontrol.Fan;
using Kontrol.Models;
using Kontrol.Services;
using Kontrol.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using WinRT.Interop;

namespace Kontrol;

public partial class App : Application
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Kontrol_startup.log");

    private TrayService? _trayService;
    private MainWindow? _mainWindow;
    private FanControllerService? _fanController;
    private TempAlertService? _tempAlertService;

    public static Window? Window { get; private set; }
    public static HardwareService? HardwareServiceInstance { get; private set; }
    public static FanControlService? FanControlServiceInstance { get; private set; }
    public static FanControllerService? FanControllerInstance { get; private set; }
    public static MainViewModel? MainVm { get; internal set; }

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        WriteLog("OnLaunched started");
        try
        {
            var settings = AppSettings.Load();
            WriteLog("Settings loaded");

            Loc.Load(settings.Language);

            HardwareServiceInstance = new HardwareService();
            FanControlServiceInstance = new FanControlService();
            var fanConfigService = new FanConfigService();

            _fanController = new FanControllerService(
                HardwareServiceInstance,
                FanControlServiceInstance,
                fanConfigService,
                settings.PollingIntervalMs);
            FanControllerInstance = _fanController;
            _fanController.Start();
            WriteLog("FanControllerService started");

            _tempAlertService = new TempAlertService(_fanController, settings);
            _tempAlertService.AlertTriggered += OnAlertTriggered;
            _tempAlertService.Start();
            WriteLog("TempAlertService started");

            _mainWindow = new MainWindow(settings);
            Window = _mainWindow;

            try { SetupTrayIcon(); } catch (Exception ex) { WriteLog("Tray icon warning: " + ex.Message); }

            if (!settings.StartMinimized)
                _mainWindow.Activate();

            WriteLog("Startup complete");
        }
        catch (Exception ex)
        {
            WriteLog("OnLaunched ERROR: " + ex);
            var errWin = new Microsoft.UI.Xaml.Window();
            errWin.Content = new TextBlock
            {
                Text = "Startup error:\n\n" + ex.Message,
                Margin = new Microsoft.UI.Xaml.Thickness(32)
            };
            errWin.Activate();
        }
    }

    private void OnAlertTriggered(string title, string message)
    {
        try { _trayService?.ShowNotification(title, message); } catch { }
        MainVm?.ShowGlobalAlert(title, message);
    }

    private void SetupTrayIcon()
    {
        if (_mainWindow is null) return;
        var hwnd = WindowNative.GetWindowHandle(_mainWindow);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
        _trayService = new TrayService(
            hwnd,
            tooltip: "Kontrol — Fan + RGB Dashboard",
            showHideText: Loc.Get("TrayShowHide"),
            exitText: Loc.Get("TrayExit"),
            iconPath: iconPath);
        _trayService.OnLeftClick = ToggleMainWindow;
        _trayService.OnExit = () => { CleanUp(); _mainWindow?.Close(); };
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null) return;
        if (_mainWindow.AppWindow.IsVisible)
            _mainWindow.AppWindow.Hide();
        else
            _mainWindow.Activate();
    }

    internal void CleanUp()
    {
        try { _tempAlertService?.Dispose(); } catch { }
        try { _fanController?.Dispose(); } catch { }
        try { HardwareServiceInstance?.Dispose(); } catch { }
        try { _trayService?.Dispose(); } catch { }
    }

    private static void WriteLog(string msg)
    {
        try { File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n"); } catch { }
    }
}
