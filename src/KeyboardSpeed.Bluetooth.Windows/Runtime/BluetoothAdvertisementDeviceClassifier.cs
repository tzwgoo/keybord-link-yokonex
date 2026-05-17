using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public static class BluetoothAdvertisementDeviceClassifier
{
    public const string EmsServiceUuid = "0000ff30-0000-1000-8000-00805f9b34fb";
    public const string ToyServiceUuid = "0000ff40-0000-1000-8000-00805f9b34fb";

    private static readonly string[] EmsNamePrefixes =
    [
        "YYC-DJ-V2",
        "YYC-DJ"
    ];

    public static bool TryResolveDeviceType(
        IEnumerable<Guid> advertisedServiceUuids,
        string? deviceName,
        out BluetoothDeviceType deviceType,
        out string serviceUuid)
    {
        foreach (var guid in advertisedServiceUuids)
        {
            var normalized = guid.ToString().ToLowerInvariant();
            if (normalized == EmsServiceUuid)
            {
                deviceType = BluetoothDeviceType.Ems;
                serviceUuid = normalized;
                return true;
            }

            if (normalized == ToyServiceUuid)
            {
                deviceType = BluetoothDeviceType.Toy;
                serviceUuid = normalized;
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(deviceName) &&
            EmsNamePrefixes.Any(prefix => deviceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            deviceType = BluetoothDeviceType.Ems;
            serviceUuid = EmsServiceUuid;
            return true;
        }

        deviceType = BluetoothDeviceType.Unknown;
        serviceUuid = string.Empty;
        return false;
    }

    public static BluetoothProtocolProfile ResolveProtocolProfile(BluetoothDeviceType deviceType, string? deviceName)
    {
        if (deviceType == BluetoothDeviceType.Toy)
        {
            return BluetoothProtocolProfile.ToyV1;
        }

        if (deviceType != BluetoothDeviceType.Ems)
        {
            return BluetoothProtocolProfile.Unknown;
        }

        if (!string.IsNullOrWhiteSpace(deviceName) &&
            deviceName.StartsWith("YYC-DJ-V2", StringComparison.OrdinalIgnoreCase))
        {
            return BluetoothProtocolProfile.EmsV2;
        }

        if (!string.IsNullOrWhiteSpace(deviceName) &&
            deviceName.StartsWith("YYC-DJ", StringComparison.OrdinalIgnoreCase))
        {
            return BluetoothProtocolProfile.EmsV1;
        }

        return BluetoothProtocolProfile.EmsV2;
    }
}
