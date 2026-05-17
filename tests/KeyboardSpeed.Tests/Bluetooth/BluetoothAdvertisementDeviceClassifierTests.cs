using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Tests.Bluetooth;

public sealed class BluetoothAdvertisementDeviceClassifierTests
{
    [Fact]
    public void ShouldResolveEmsDeviceFromServiceUuid()
    {
        var serviceUuids = new[] { Guid.Parse(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid) };

        var resolved = BluetoothAdvertisementDeviceClassifier.TryResolveDeviceType(
            serviceUuids,
            "Unknown",
            out var deviceType,
            out var serviceUuid);

        Assert.True(resolved);
        Assert.Equal(BluetoothDeviceType.Ems, deviceType);
        Assert.Equal(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid, serviceUuid);
    }

    [Fact]
    public void ShouldResolveEmsV2ProfileFromDeviceName()
    {
        var profile = BluetoothAdvertisementDeviceClassifier.ResolveProtocolProfile(
            BluetoothDeviceType.Ems,
            "YYC-DJ-V2-001");

        Assert.Equal(BluetoothProtocolProfile.EmsV2, profile);
    }
}
