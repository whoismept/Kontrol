using Kontrol.Fan;
using Kontrol.Services;
using Kontrol.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Kontrol.Views;

public sealed partial class FanControlView : Page
{
    public FanControlView()
    {
        InitializeComponent();
        DataContext = App.MainVm?.FanConfig;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        PageTitle.Text = Loc.Get("PageFanControl");
        BtnImportText.Text = Loc.Get("BtnImport");
        BtnExportText.Text = Loc.Get("BtnExport");
        BtnRescan.Content = new FontIcon { Glyph = "", FontSize = 14 };
        ToolTipService.SetToolTip(BtnRescan, Loc.Get("TipRescan"));
        BtnSaveText.Text = Loc.Get("BtnSave");
        LblProfilesBarText.Text = Loc.Get("LblFanProfiles");
        NewProfileBox.PlaceholderText = Loc.Get("HintNewFanProfileName");
        BtnSaveProfileText.Text = Loc.Get("BtnSaveFanProfile");

        TabFans.Header = Loc.Get("SecFans");
        TabCurves.Header = Loc.Get("SecCurves");
        TabTempSources.Header = Loc.Get("SecTempSources");

        BtnAddCurveText.Text = Loc.Get("BtnAdd");
        BtnDeleteCurveText.Text = Loc.Get("BtnDelete");
        HintSelectCurveText.Text = Loc.Get("HintSelectCurve");
        LblNameCurveText.Text = Loc.Get("LblName");
        LblTypeCurveText.Text = Loc.Get("LblType");
        LblHysteresisText.Text = Loc.Get("LblHysteresis");
        LblResponseText.Text = Loc.Get("LblResponse");
        SecCurvePointsText.Text = Loc.Get("SecCurvePoints");
        BtnAddPointText.Text = Loc.Get("BtnAddPoint");
        LblTempCText.Text = Loc.Get("LblTempC");
        LblSpeedPctText.Text = Loc.Get("LblSpeedPct");

        BtnAddSourceText.Text = Loc.Get("BtnAdd");
        BtnDeleteSourceText.Text = Loc.Get("BtnDelete");
        LblNameSrcText.Text = Loc.Get("LblName");
        LblModeSrcText.Text = Loc.Get("LblMode");
        SecSensorFiltersText.Text = Loc.Get("SecSensorFilters");
        BtnAddFilterText.Text = Loc.Get("BtnAddFilter");
        HintWildcardText.Text = Loc.Get("HintWildcardSupport");
        SecDetectedSensorsText.Text = Loc.Get("SecDetectedSensors");
    }

    private void OnRemoveCurvePointClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CurvePoint point
            && DataContext is FanControlViewModel vm)
        {
            vm.RemoveCurvePointCommand.Execute(point);
        }
    }

    private void OnRemoveSensorRefClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SensorRef sref
            && DataContext is FanControlViewModel vm)
        {
            vm.RemoveSensorRefCommand.Execute(sref);
        }
    }

    private void OnProfileButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is FanProfile profile
            && DataContext is FanControlViewModel vm)
        {
            vm.ApplyFanProfileCommand.Execute(profile);
        }
    }
}
