using Kontrol.Models;
using Kontrol.Services;
using Kontrol.ViewModels;
using Kontrol.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Kontrol;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;

    public MainWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        _vm = new MainViewModel(settings);
        App.MainVm = _vm;

        SetupTitleBar();
        ApplyNavLabels();
        UpdateAdvancedNavItems(_vm.IsAdvancedMode);

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsAdvancedMode))
                UpdateAdvancedNavItems(_vm.IsAdvancedMode);
        };

        Loc.LanguageChanged += OnLanguageChanged;
        Activated += OnActivated;
        AppWindow.Closing += OnAppWindowClosing;
    }

    private void SetupTitleBar()
    {
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;

        var root = (FrameworkElement)Content;
        root.DataContext = _vm;
        root.ActualThemeChanged += (_, _) => ApplyTitleBarColors(root.ActualTheme);
        ApplyTitleBarColors(root.ActualTheme);
    }

    private void ApplyTitleBarColors(ElementTheme theme)
    {
        bool dark = theme == ElementTheme.Dark;
        var tb = AppWindow.TitleBar;
        tb.ExtendsContentIntoTitleBar = true;

        Color fg      = dark ? new() { A = 255, R = 255, G = 255, B = 255 } : new() { A = 255, R = 0,   G = 0,   B = 0   };
        Color fgDim   = dark ? new() { A = 0x99, R = 255, G = 255, B = 255 } : new() { A = 0x99, R = 0,   G = 0,   B = 0   };
        Color hover   = dark ? new() { A = 0x18, R = 255, G = 255, B = 255 } : new() { A = 0x18, R = 0,   G = 0,   B = 0   };
        Color pressed = dark ? new() { A = 0x30, R = 255, G = 255, B = 255 } : new() { A = 0x30, R = 0,   G = 0,   B = 0   };
        Color none    = new() { A = 0, R = 0, G = 0, B = 0 };

        tb.ButtonForegroundColor        = fg;
        tb.ButtonHoverForegroundColor   = fg;
        tb.ButtonPressedForegroundColor = fg;
        tb.ButtonInactiveForegroundColor = fgDim;

        tb.ButtonBackgroundColor        = none;
        tb.ButtonHoverBackgroundColor   = hover;
        tb.ButtonPressedBackgroundColor = pressed;
        tb.ButtonInactiveBackgroundColor = none;
    }

    private void ApplyNavLabels()
    {
        NavDashboard.Content  = Loc.Get("NavDashboard");
        NavFanControl.Content = Loc.Get("NavFanControl");
        NavRgb.Content        = Loc.Get("NavRGB");
        NavHardware.Content   = Loc.Get("NavHardware");
    }

    internal void UpdateAdvancedNavItems(bool isAdvanced)
    {
        var visibility = isAdvanced ? Visibility.Visible : Visibility.Collapsed;
        NavFanControl.Visibility = visibility;
        NavRgb.Visibility        = visibility;
        NavHardware.Visibility   = visibility;

        if (!isAdvanced && ContentFrame.CurrentSourcePageType is Type t &&
            (t == typeof(FanControlView) || t == typeof(RgbView) || t == typeof(FanView)))
        {
            MainNav.SelectedItem = NavDashboard;
            ContentFrame.Navigate(typeof(DashboardView));
        }
    }

    internal void NavigateTo(Type pageType)
    {
        if (ContentFrame.CurrentSourcePageType != pageType)
            ContentFrame.Navigate(pageType);

        var item = pageType.Name switch
        {
            "DashboardView"  => NavDashboard,
            "FanControlView" => NavFanControl,
            "RgbView"        => NavRgb,
            "FanView"        => NavHardware,
            _ => null
        };
        if (item is not null) MainNav.SelectedItem = item;
    }

    private void OnLanguageChanged()
    {
        ApplyNavLabels();
        var type = ContentFrame.CurrentSourcePageType;
        if (type != null)
        {
            ContentFrame.Navigate(type, null, new Microsoft.UI.Xaml.Media.Animation.SuppressNavigationTransitionInfo());
        }
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (ContentFrame.Content is null)
        {
            MainNav.SelectedItem = NavDashboard;
            ContentFrame.Navigate(typeof(DashboardView));
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            if (ContentFrame.CurrentSourcePageType != typeof(SettingsView))
                ContentFrame.Navigate(typeof(SettingsView));
            return;
        }

        if (args.SelectedItem is not NavigationViewItem item) return;

        var pageType = item.Tag?.ToString() switch
        {
            "DashboardView"  => typeof(DashboardView),
            "FanView"        => typeof(FanView),
            "FanControlView" => typeof(FanControlView),
            "RgbView"        => typeof(RgbView),
            "SettingsView"   => typeof(SettingsView),
            _ => null
        };

        if (pageType is not null && ContentFrame.CurrentSourcePageType != pageType)
            ContentFrame.Navigate(pageType);
    }

    private void OnAppWindowClosing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        if (_settings.MinimizeToTray)
        {
            args.Cancel = true;
            AppWindow.Hide();
        }
        else
        {
            CleanUp();
        }
    }

    private void CleanUp()
    {
        try { _vm?.Fan?.Dispose(); } catch { }
        try { _vm?.Rgb?.Dispose(); } catch { }
        (App.Current as App)?.CleanUp();
    }
}
