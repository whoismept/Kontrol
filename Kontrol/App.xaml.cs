using Hardcodet.Wpf.TaskbarNotification;
using Kontrol.Models;
using Kontrol.Services;
using Kontrol.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Kontrol;

public partial class App : Application
{
    private static readonly string LogPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Kontrol_startup.log");

    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private FanControllerService? _fanController;
    private TempAlertService? _tempAlertService;

    public static HardwareService? HardwareServiceInstance { get; private set; }
    public static FanControlService? FanControlServiceInstance { get; private set; }
    public static FanControllerService? FanControllerInstance { get; private set; }
    public static MainViewModel? MainVm { get; internal set; }
    public static SnackbarPresenter? SnackbarPresenter { get; internal set; }

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var msg = args.ExceptionObject?.ToString() ?? "Unknown error";
            WriteLog("UnhandledException: " + msg);
            System.Windows.MessageBox.Show(msg, "Critical Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        };
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        WriteLog("OnStartup started");

        DispatcherUnhandledException += (_, args) =>
        {
            WriteLog("DispatcherUnhandledException: " + args.Exception);
            System.Windows.MessageBox.Show(args.Exception?.ToString(), "UI Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            base.OnStartup(e);
            WriteLog("base.OnStartup completed");

            var settings = AppSettings.Load();
            WriteLog("Settings loaded");

            Services.Loc.Load(settings.Language);
            ApplyStartupTheme(settings.Theme);

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

            CreateTrayIcon();
            WriteLog("Tray icon created");

            _tempAlertService = new TempAlertService(HardwareServiceInstance, _trayIcon!, settings);
            _tempAlertService.Start();
            WriteLog("TempAlertService started");

            _mainWindow = new MainWindow(settings);
            WriteLog("MainWindow created");

            if (settings.StartMinimized)
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Minimized;
                _mainWindow.Hide();
            }
            else
            {
                _mainWindow.Show();
            }

            WriteLog("MainWindow shown");
        }
        catch (Exception ex)
        {
            WriteLog("OnStartup ERROR: " + ex);
            System.Windows.MessageBox.Show(
                $"Startup error:\n\n{ex.Message}\n\n{ex.InnerException?.Message}\n\nCheck Kontrol_startup.log on Desktop for details.",
                "Startup Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void CreateTrayIcon()
    {
        var contextMenu = new ContextMenu();

        var toggleItem = new System.Windows.Controls.MenuItem { Header = "Show/Hide", FontWeight = FontWeights.SemiBold };
        toggleItem.Click += (_, _) => ToggleMainWindow();
        contextMenu.Items.Add(toggleItem);

        contextMenu.Items.Add(new Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            _trayIcon?.Dispose();
            Shutdown();
        };
        contextMenu.Items.Add(exitItem);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Kontrol — Fan + RGB Dashboard",
            ContextMenu = contextMenu
        };
        _trayIcon.TrayLeftMouseDown += (_, _) => ToggleMainWindow();

        try { _trayIcon.Icon = LoadAppIcon(); }
        catch (Exception ex) { WriteLog("Icon load warning (non-critical): " + ex.Message); }
    }

    internal static System.Drawing.Icon LoadAppIcon()
    {
        var uri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute);
        var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
        if (stream is not null)
            return new System.Drawing.Icon(stream);
        throw new FileNotFoundException("Embedded app.ico not found.");
    }

    private void ToggleMainWindow()
    {
        if (_mainWindow is null) return;

        if (_mainWindow.IsVisible)
        {
            _mainWindow.Hide();
            _mainWindow.ShowInTaskbar = false;
        }
        else
        {
            ShowMainWindow();
        }
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.ShowInTaskbar = true;
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tempAlertService?.Dispose();
        _fanController?.Dispose();
        HardwareServiceInstance?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }

    private static void ApplyStartupTheme(string theme)
    {
        try { Services.ThemeHelper.Apply(theme); }
        catch { }
    }

    private static void WriteLog(string msg)
    {
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}\n");
        }
        catch { }
    }

    public static void ShowSnackbar(string title, string message, ControlAppearance appearance = ControlAppearance.Success, int durationMs = 3000)
    {
        if (SnackbarPresenter is not null)
        {
            // TODO: Fix SnackbarPresenter.Show as it doesn't exist on the control.
            // SnackbarPresenter.Show(title, message, appearance, new TimeSpan(0, 0, 0, 0, durationMs));
        }
    }
}
