using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Tests.Bluetooth;

public sealed class BluetoothNotificationParserTests
{
    [Fact]
    public void ParseStatus_ShouldReadEmsChannelAStatus()
    {
        var parser = new BluetoothNotificationParser();
        var packet = new byte[] { 0x35, 0x71, 0x01, 0x02, 0x01, 0x00, 0x24, 0x03, 0x00 };

        var status = parser.ParseStatus(BluetoothDeviceType.Ems, packet);

        Assert.NotNull(status);
        Assert.Equal(2, status.ChannelAElectrodeStatus);
        Assert.True(status.ChannelAEnabled);
        Assert.Equal(36, status.ChannelAStrength);
        Assert.Equal(3, status.ChannelAMode);
    }

    [Fact]
    public void ParseStatus_ShouldReadEmsStepCount()
    {
        var parser = new BluetoothNotificationParser();
        var packet = new byte[] { 0x35, 0x71, 0x05, 0x00, 0x08, 0x00 };

        var status = parser.ParseStatus(BluetoothDeviceType.Ems, packet);

        Assert.NotNull(status);
        Assert.Equal(8, status.StepCount);
    }
}
