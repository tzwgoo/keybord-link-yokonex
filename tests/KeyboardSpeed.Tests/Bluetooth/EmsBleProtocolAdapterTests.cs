using KeyboardSpeed.Bluetooth.Windows.Protocol;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Bluetooth;

public sealed class EmsBleProtocolAdapterTests
{
    [Fact]
    public void Adapter_ShouldCreateStopPacket()
    {
        var adapter = new EmsBleProtocolAdapter();

        var bytes = adapter.CreateStopPacket();

        Assert.Equal([0x35, 0x11, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x49], bytes);
    }

    [Fact]
    public void Adapter_ShouldCreateFixedPacketForWaveformStep()
    {
        var adapter = new EmsBleProtocolAdapter();
        var waveform = new EmsWaveformDefinition
        {
            Id = "heartbeat",
            Name = "Heartbeat",
            Steps =
            [
                new EmsWaveformStep
                {
                    AStrength = 42,
                    AMode = 1,
                    BStrength = 38,
                    BMode = 2
                }
            ]
        };

        var packets = adapter.CreatePackets(waveform);

        Assert.Single(packets);
        Assert.Equal([0x35, 0x11, 0x01, 0x00, 0x2A, 0x01, 0x00, 0x26, 0x02, 0x9A], packets[0]);
    }

    [Fact]
    public void Adapter_ShouldUseV1StopPacketForLegacyProfile()
    {
        var adapter = new EmsBleProtocolAdapter();
        var device = new BluetoothDeviceDescriptor
        {
            DeviceId = "legacy",
            Name = "Legacy EMS",
            ProtocolProfile = BluetoothProtocolProfile.EmsV1
        };

        var bytes = adapter.CreateStopPacket(device);

        Assert.Equal([0x35, 0x11, 0x03, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00, 0x4B], bytes);
    }

    [Fact]
    public void Adapter_ShouldClampStrengthToDeviceMaxBeforePacking()
    {
        var adapter = new EmsBleProtocolAdapter();
        var waveform = new EmsWaveformDefinition
        {
            Id = "max-strength",
            Name = "Max Strength",
            Steps =
            [
                new EmsWaveformStep
                {
                    AStrength = 220,
                    AMode = 1,
                    BStrength = 205,
                    BMode = 2
                }
            ]
        };

        var packets = adapter.CreatePackets(waveform);

        Assert.Single(packets);
        Assert.Equal([0x35, 0x11, 0x01, 0x00, 0xB4, 0x01, 0x00, 0xB4, 0x02, 0xB2], packets[0]);
    }
}
