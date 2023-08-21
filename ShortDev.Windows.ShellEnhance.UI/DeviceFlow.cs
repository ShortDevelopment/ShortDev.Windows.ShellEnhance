using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace ShortDev.Windows.ShellEnhance.UI;

internal static class DeviceFlow
{
    public static async Task ConnectAsync(string aepId)
        => await ConnectionWorker(aepId, connect: true);

    public static async Task DisconnectAsync(string aepId)
        => await ConnectionWorker(aepId, connect: false);

    static async Task ConnectionWorker(string aepId, bool connect)
    {
        var aepInfo = await DeviceInformation.CreateFromIdAsync(
            aepId,
            new[]
            {
                "System.Devices.Aep.ContainerId"
            },
            DeviceInformationKind.AssociationEndpoint
        );
        var devices = await GetDevicesFromAepAsync(aepInfo);
        foreach (var device in devices)
        {
            var endpointId = GetProp<string>(device, "System.Devices.DeviceInstanceId");
            if (endpointId.Contains("MMDEVAPI"))
            {
                endpointId = endpointId.Replace(@"SWD\MMDEVAPI\", null);
                Marshal.ThrowExceptionForHR(ConnectBtEndpoint(endpointId, connect));
            }
        }

        if (!connect)
        {
            var btDevice = await BluetoothDevice.FromIdAsync(aepId);
            Marshal.ThrowExceptionForHR(BluetoothDisconnectDevice(IntPtr.Zero, btDevice.BluetoothAddress));
        }
    }

    static async Task<DeviceInformationCollection> GetDevicesFromAepAsync(DeviceInformation aepInfo)
    {
        if (aepInfo.Kind != DeviceInformationKind.AssociationEndpoint)
            throw new ArgumentException();

        var containerId = GetProp<Guid>(aepInfo, "System.Devices.Aep.ContainerId");

        var deviceContainer = await DeviceInformation.CreateFromIdAsync(
            containerId.ToString(),
            new[]
            {
                "System.Devices.CategoryIds",
                "System.Devices.HardwareIds",
                "System.Devices.Paired"
            },
            DeviceInformationKind.DeviceContainer
        );
        return await DeviceInformation.FindAllAsync(
            $"System.Devices.ContainerId:=\"{{{containerId}}}\""
        );
    }

    static T GetProp<T>(DeviceInformation deviceInformation, string propId)
        => (T)deviceInformation.Properties[propId];


    [DllImport("DeviceFlow.dll", CharSet = CharSet.Unicode)]
    static extern int ConnectBtEndpoint(string deviceId, bool connect);

    [DllImport("DeviceFlow.dll", CharSet = CharSet.Unicode)]
    static extern int GetDevice(out string deviceId);

    [DllImport("BluetoothApis.dll", CharSet = CharSet.Unicode)]
    static extern int BluetoothDisconnectDevice(IntPtr reserved, ulong deviceId);
}
