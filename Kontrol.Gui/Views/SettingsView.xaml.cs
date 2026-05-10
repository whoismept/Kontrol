using Kontrol.Services;
using Microsoft.UI.Xaml.Controls;

namespace Kontrol.Views;

public sealed partial class SettingsView : Page
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.Settings;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitle.Text = Loc.Get("PageSettings");
        SecHardwareText.Text = Loc.Get("SecHardwareMonitoring");
        LblSensorIntervalText.Text = Loc.Get("LblSensorInterval");
        LblSensorIntervalHintText.Text = Loc.Get("LblSensorIntervalHint");
        SecAppBehaviorText.Text = Loc.Get("SecAppBehavior");
        LblMinimizeToTrayText.Text = Loc.Get("LblMinimizeToTray");
        LblMinimizeToTrayHintText.Text = Loc.Get("LblMinimizeToTrayHint");
        LblStartMinimizedText.Text = Loc.Get("LblStartMinimized");
        LblStartMinimizedHintText.Text = Loc.Get("LblStartMinimizedHint");
        LblLaunchOnStartupText.Text = Loc.Get("LblLaunchOnStartup");
        LblLaunchOnStartupHintText.Text = Loc.Get("LblLaunchOnStartupHint");
        SecTempAlertsText.Text = Loc.Get("SecTempAlerts");
        LblEnableAlertsText.Text = Loc.Get("LblEnableAlerts");
        LblEnableAlertsHintText.Text = Loc.Get("LblEnableAlertsHint");
        LblAlertThresholdText.Text = Loc.Get("LblAlertThreshold");
        SecAppearanceText.Text = Loc.Get("SecAppearance");
        LblThemeText.Text = Loc.Get("LblTheme");
        LblThemeHintText.Text = Loc.Get("LblThemeHint");
        LblLanguageText.Text = Loc.Get("LblLanguage");
        LblLanguageHintText.Text = Loc.Get("LblLanguageHint");
        SecAdvancedText.Text = Loc.Get("SecAdvancedMode");
        LblAdvancedModeText.Text = Loc.Get("LblAdvancedMode");
        LblAdvancedModeHintText.Text = Loc.Get("LblAdvancedModeHint");
        BtnSaveText.Text = Loc.Get("BtnSave");
    }
}
