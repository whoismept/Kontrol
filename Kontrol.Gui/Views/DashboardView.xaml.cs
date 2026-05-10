using Kontrol.Fan;
using Kontrol.Rgb;
using Kontrol.Services;
using Kontrol.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.UI;

namespace Kontrol.Views;

public sealed partial class DashboardView : Page
{
    // Exposed for ElementName binding in fan-mode ComboBoxes
    public IReadOnlyList<FanMode> FanModeValues { get; } = Enum.GetValues<FanMode>();

    private DispatcherQueueTimer? _spinTimer;
    private DateTime _lastTick = DateTime.UtcNow;
    private RgbDevice? _quickPickDevice;

    public DashboardView()
    {
        InitializeComponent();
        DataContext = App.MainVm;
        ApplyLocalization();

        Loaded   += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void ApplyLocalization()
    {
        PageTitle.Text        = Loc.Get("PageDashboard");
        SecFans.Text          = Loc.Get("DashSecFans");
        SecFanProfiles.Text   = Loc.Get("SecFanProfiles");
        SecFanControl.Text    = Loc.Get("DashSecFanControl");
        SecRgb.Text           = Loc.Get("DashSecRgb");
        NoRgbText.Text        = Loc.Get("DashNoRgb");
        BtnGoAdvanced.Content = Loc.Get("DashGoAdvanced");

        QuickPickTitle.Text      = Loc.Get("DashRgbQuickPickTitle");
        QuickProfileLabel.Text   = Loc.Get("DashRgbLoadProfile");
        QuickOpenRgbLink.Content = Loc.Get("DashRgbOpenSettings");
    }

    // ── Spinning fan animation ───────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _lastTick = DateTime.UtcNow;
        var queue = DispatcherQueue.GetForCurrentThread();
        _spinTimer = queue.CreateTimer();
        _spinTimer.Interval    = TimeSpan.FromMilliseconds(16);
        _spinTimer.IsRepeating = true;
        _spinTimer.Tick       += OnSpinTick;
        _spinTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _spinTimer?.Stop();
        _spinTimer = null;
    }

    private void OnSpinTick(DispatcherQueueTimer sender, object args)
    {
        var now     = DateTime.UtcNow;
        var elapsed = (now - _lastTick).TotalSeconds;
        _lastTick   = now;

        var fan = App.MainVm?.Fan;
        if (fan is null) return;

        var globalRpm = fan.MaxFanRpm;
        foreach (var device in fan.DashDevices)
        {
            var rpm = device.FanRpm > 0 ? device.FanRpm : globalRpm;
            if (rpm < 50) continue;
            device.FanAngle = (device.FanAngle + rpm * 0.3 * elapsed) % 360;
        }
    }

    // ── Fan profile quick-switch ─────────────────────────────────────────────

    private void OnDashProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not FanProfile profile) return;
        App.MainVm?.ApplyFanPresetCommand.Execute(profile);
    }

    // ── Advanced mode navigation ─────────────────────────────────────────────

    private void OnGoAdvancedClick(object sender, RoutedEventArgs e)
    {
        if (App.MainVm is { } vm)
            vm.IsAdvancedMode = true;

        (App.Window as MainWindow)?.NavigateTo(typeof(FanControlView));
    }

    // ── RGB quick-color flyout ───────────────────────────────────────────────

    private void OnChangeColorButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not RgbDevice device) return;

        _quickPickDevice = device;
        QuickPickTitle.Text = device.Name;

        var profiles = App.MainVm?.Rgb.Profiles;
        if (profiles is not null && profiles.Count > 0)
        {
            QuickProfileCombo.ItemsSource = profiles;
            QuickProfileSection.Visibility = Visibility.Visible;
        }
        else
        {
            QuickProfileSection.Visibility = Visibility.Collapsed;
        }

        QuickColorFlyout.ShowAt(fe);
    }

    private void OnPresetColorPick(object sender, RoutedEventArgs e)
    {
        if (_quickPickDevice is null) return;
        if (sender is not FrameworkElement fe || fe.Tag is not string hex) return;

        var color = ParseHex(hex);
        App.MainVm?.Rgb.QuickSetDeviceColor(_quickPickDevice, color);
        QuickColorFlyout.Hide();
    }

    private void OnQuickProfileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_quickPickDevice is null) return;
        if (sender is not ComboBox cb || cb.SelectedItem is not string profileName) return;

        App.MainVm?.Rgb.QuickLoadProfile(profileName);
        QuickColorFlyout.Hide();
    }

    private void OnQuickOpenRgbClick(object sender, RoutedEventArgs e)
    {
        QuickColorFlyout.Hide();
        if (App.MainVm is { } vm)
            vm.IsAdvancedMode = true;
        (App.Window as MainWindow)?.NavigateTo(typeof(RgbView));
    }

    private static Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex[..2], 16);
            byte r = Convert.ToByte(hex[2..4], 16);
            byte g = Convert.ToByte(hex[4..6], 16);
            byte b = Convert.ToByte(hex[6..8], 16);
            return Color.FromArgb(a, r, g, b);
        }
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex[..2], 16);
            byte g = Convert.ToByte(hex[2..4], 16);
            byte b = Convert.ToByte(hex[4..6], 16);
            return Color.FromArgb(255, r, g, b);
        }
        return Color.FromArgb(255, 255, 0, 0);
    }
}
