using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace ShortDev.ShellEnhance.UI.Flyouts;

public sealed partial class BluetoothFlyoutPage : Page, IShellEnhanceFlyout
{
    public BluetoothFlyoutPage()
    {
        this.InitializeComponent();

        this.Loaded += BluetoothFlyoutPage_Loaded;
    }

    ObservableCollection<BluetoothDeviceInfo> bluetoothDeviceInfos = new();
    private async void BluetoothFlyoutPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        string selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        DeviceWatcher watcher = DeviceInformation.CreateWatcher(selector);
        watcher.Added += Watcher_Added;
        watcher.Start();

        DevicesListView.ItemsSource = bluetoothDeviceInfos;
    }

    private void Watcher_Added(DeviceWatcher sender, DeviceInformation device)
    {
        _ = Dispatcher.RunIdleAsync(async (x) =>
        {
            var result = new BluetoothDeviceInfo()
            {
                Name = device.Name,
                DeviceInformation = device
            };

            var glyph = await device.GetGlyphThumbnailAsync();
            var imgSource = new BitmapImage();
            imgSource.SetSource(glyph);
            result.Thumbnail = imgSource;

            var btDevice = await BluetoothDevice.FromIdAsync(device.Id);
            var status = btDevice.ConnectionStatus;

            if (device.Pairing != null)
            {
                result.CanPair = device.Pairing.CanPair;
                result.IsPaired = device.Pairing.IsPaired;
            }

            bluetoothDeviceInfos.Add(result);
        });
    }

    public string IconAssetId
        => "bluetooth";

    public Guid IconId
        => new("CDDDFED1-DB5B-4FEC-8E4E-231B8232CA47");

    private void DevicesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var device = ((BluetoothDeviceInfo)DevicesListView.SelectedItem).DeviceInformation;
    }

    private async void ConnectDeviceButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        var source = (FrameworkElement)e.OriginalSource;
        var device = ((BluetoothDeviceInfo)source.DataContext).DeviceInformation;
        await DeviceFlow.ConnectAsync(device.Id);
    }

    private async void DisconnectDeviceButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        var source = (FrameworkElement)e.OriginalSource;
        var device = ((BluetoothDeviceInfo)source.DataContext).DeviceInformation;
        await DeviceFlow.DisconnectAsync(device.Id);
    }

    private void MoreSettingsButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
    {
        _ = Launcher.LaunchUriAsync(new Uri("ms-settings:bluetooth"));
    }
}

public class BluetoothDeviceInfo
{
    public string Name { get; set; }
    public BitmapImage Thumbnail { get; set; }

    public bool CanPair { get; set; } = false;
    public bool IsPaired { get; set; } = false;
    public bool CanConnect { get; set; } = true;

    public DeviceInformation DeviceInformation { get; set; }
}
