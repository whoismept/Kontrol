using Kontrol.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Kontrol.Views;

public sealed partial class DashboardView : Page
{
    private DispatcherQueueTimer? _spinTimer;
    private DateTime _lastTick = DateTime.UtcNow;

    public DashboardView()
    {
        InitializeComponent();
        DataContext = App.MainVm;
        PageTitle.Text = Loc.Get("PageDashboard");
        SecFans.Text   = Loc.Get("DashSecFans");
        SecRgb.Text    = Loc.Get("DashSecRgb");
        NoRgbText.Text = Loc.Get("DashNoRgb");

        Loaded   += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _lastTick = DateTime.UtcNow;
        var queue = DispatcherQueue.GetForCurrentThread();
        _spinTimer = queue.CreateTimer();
        _spinTimer.Interval = TimeSpan.FromMilliseconds(16);
        _spinTimer.IsRepeating = true;
        _spinTimer.Tick += OnSpinTick;
        _spinTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _spinTimer?.Stop();
        _spinTimer = null;
    }

    private void OnSpinTick(DispatcherQueueTimer sender, object args)
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastTick).TotalSeconds;
        _lastTick = now;

        var fan = App.MainVm?.Fan;
        if (fan is null) return;

        var globalRpm = fan.MaxFanRpm;

        foreach (var device in fan.DashDevices)
        {
            // Use device-specific RPM if available, otherwise fall back to system max.
            // This ensures the icon spins even when fans live under a different hardware node.
            var rpm = device.FanRpm > 0 ? device.FanRpm : globalRpm;
            if (rpm < 50) continue;
            // Visual speed: 5% of real RPM → rpm * (360/60) * 0.05 = rpm * 0.3 deg/s
            device.FanAngle = (device.FanAngle + rpm * 0.3 * elapsed) % 360;
        }
    }
}
