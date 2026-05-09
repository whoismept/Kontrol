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
        SecOpenRGBText.Text = Loc.Get("SecOpenRGB");
        LblOpenRGBServerText.Text = Loc.Get("LblOpenRGBServer");
        LblOpenRGBServerHintText.Text = Loc.Get("LblOpenRGBServerHint");
        LblServerText.Text = Loc.Get("LblServer");
        LblPortText.Text = Loc.Get("LblPort");
        LblClientText.Text = Loc.Get("LblClient");
        LblOpenRGBNoteText.Text = Loc.Get("LblOpenRGBNote");
        SecAppearanceText.Text = Loc.Get("SecAppearance");
        LblThemeText.Text = Loc.Get("LblTheme");
        LblThemeHintText.Text = Loc.Get("LblThemeHint");
        LblLanguageText.Text = Loc.Get("LblLanguage");
        LblLanguageHintText.Text = Loc.Get("LblLanguageHint");
        BtnSaveText.Text = Loc.Get("BtnSave");
    }
}
