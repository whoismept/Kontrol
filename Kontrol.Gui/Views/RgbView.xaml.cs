using Kontrol.Services;
using Kontrol.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Kontrol.Views;

public sealed partial class RgbView : Page
{
    public RgbView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.Rgb;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitle.Text = Loc.Get("PageRGBControl");
        ToolTipService.SetToolTip(RescanBtn, Loc.Get("TipRescan"));
        ErrorBanner.Title = Loc.Get("RgbErrorTitle");
        RgbNoDevicesText.Text = Loc.Get("RgbNoDevices");
        RgbDetectionIntroText.Text = Loc.Get("RgbDetectionIntro");
        RgbLampArrayLbl.Text = Loc.Get("RgbLampArrayLabel");
        RgbLampArrayDescText.Text = Loc.Get("RgbLampArrayDesc");
        RgbVendorSDKLbl.Text = Loc.Get("RgbVendorSDKLabel");
        RgbVendorSDKDescText.Text = Loc.Get("RgbVendorSDKDesc");
        RgbStartupSummaryText.Text = Loc.Get("RgbStartupSummary");
        RgbRestartHintText.Text = Loc.Get("HintRestartAfterSDK");
        RgbDevicesText.Text = Loc.Get("RgbDevices");
        LblHexText.Text = Loc.Get("LblHEX");
        LblPresetsText.Text = Loc.Get("LblPresetColors");
        BtnApplySelText.Text = Loc.Get("BtnApplySelected");
        BtnApplyAllText.Text = Loc.Get("BtnApplyAll");
        LblProfilesText.Text = Loc.Get("LblProfiles");
        NewProfileBox.PlaceholderText = Loc.Get("HintNewProfileName");
        BtnLoadText.Text = Loc.Get("BtnLoad");
        BtnSaveText.Text = Loc.Get("BtnSave");
    }

    private void OnPresetColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex && DataContext is RgbViewModel vm)
        {
            try
            {
                var h = hex.TrimStart('#');
                byte r = Convert.ToByte(h[..2], 16);
                byte g = Convert.ToByte(h[2..4], 16);
                byte b = Convert.ToByte(h[4..6], 16);
                vm.SelectedColor = Color.FromArgb(255, r, g, b);
            }
            catch { }
        }
    }
}
