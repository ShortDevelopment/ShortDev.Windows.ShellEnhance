using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShortDev.Windows.ShellEnhance.UI.Flyouts;

public sealed partial class EnergyFlyoutPage : Page, IShellEnhanceFlyout
{
    public EnergyFlyoutPage()
    {
        InitializeComponent();

        Loaded += EnergyFlyoutPage_Loaded;
    }

    IReadOnlyList<Guid> _powerSchemas;
    private void EnergyFlyoutPage_Loaded(object sender, RoutedEventArgs e)
    {
        _powerSchemas = PowerApi.AllPowerSchemas;
        SelectPowerPlanListView.ItemsSource = _powerSchemas.Select(PowerApi.GetFriendlyName);

        var activeSchema = PowerApi.ActiveSchema;
        for (int i = 0; i < _powerSchemas.Count; i++)
        {
            if (_powerSchemas[i] == activeSchema)
                SelectPowerPlanListView.SelectedIndex = i;
        }
    }

    public string IconAssetId
        => "battery";

    public ushort IconId
        => 0x2022;

    private void SelectPowerPlanListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        PowerApi.ActiveSchema = _powerSchemas[SelectPowerPlanListView.SelectedIndex];
    }
}