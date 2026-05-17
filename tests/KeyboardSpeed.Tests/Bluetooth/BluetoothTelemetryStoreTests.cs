using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Tests.Bluetooth;

public sealed class BluetoothTelemetryStoreTests
{
    [Fact]
    public void RecordStatus_ShouldCaptureChannelStrengthsIntoTelemetry()
    {
        var store = new BluetoothTelemetryStore();
        var now = DateTimeOffset.UtcNow;

        store.RecordStatus(new BluetoothConnectionStatus
        {
            IsConnected = true,
            Device = new BluetoothDeviceDescriptor
            {
                DeviceId = "dev-1",
                Name = "YYC-DJ-V2",
                DeviceType = BluetoothDeviceType.Ems
            },
            BatteryLevel = 87,
            ChannelAEnabled = true,
            ChannelAStrength = 28,
            ChannelBEnabled = false,
            ChannelBStrength = 14
        }, now);

        var snapshot = store.GetSnapshot(now);
        var sample = Assert.Single(snapshot.Samples);
        Assert.Equal(87, sample.BatteryLevel);
        Assert.True(sample.ChannelAEnabled);
        Assert.Equal(28, sample.ChannelAStrength);
        Assert.False(sample.ChannelBEnabled);
        Assert.Equal(14, sample.ChannelBStrength);
    }
}
