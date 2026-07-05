using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Bluetooth.Windows.Protocol;

public sealed class EmsBleProtocolAdapter
{
    public IReadOnlyList<byte[]> CreatePackets(EmsWaveformDefinition waveform, BluetoothDeviceDescriptor? device = null)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        var profile = device?.ProtocolProfile ?? BluetoothProtocolProfile.EmsV2;
        return waveform.Steps.Select(step => profile switch
        {
            BluetoothProtocolProfile.EmsV1 => CreateV1Packet(step),
            _ => CreateFixedPacket(step)
        }).ToList();
    }

    public byte[] CreateStopPacket(BluetoothDeviceDescriptor? device = null)
    {
        var profile = device?.ProtocolProfile ?? BluetoothProtocolProfile.EmsV2;
        return profile switch
        {
            BluetoothProtocolProfile.EmsV1 => CreateV1StopPacket(),
            _ => CreateFixedPacket(new EmsWaveformStep
            {
                AStrength = 0,
                AMode = 1,
                BStrength = 0,
                BMode = 1
            })
        };
    }

    private static byte[] CreateV1Packet(EmsWaveformStep step)
    {
        var channel = ResolveV1Channel(step);
        var enabled = channel != 0x00;
        var source = ResolveV1SourceStep(step, channel);
        var bytes = new List<byte>
        {
            0x35,
            0x11,
            channel,
            enabled ? (byte)0x01 : (byte)0x00,
            High(source.Strength),
            Low(source.Strength),
            (byte)source.Mode,
            source.Mode == 0x11 ? (byte)source.Frequency : (byte)0x00,
            source.Mode == 0x11 ? (byte)source.PulseWidth : (byte)0x00
        };
        bytes.Add(EmsPacketChecksum.Compute(bytes));
        return [.. bytes];
    }

    private static byte[] CreateFixedPacket(EmsWaveformStep step)
    {
        var aStrength = EmsWaveformStep.ClampStrength(step.AStrength);
        var bStrength = EmsWaveformStep.ClampStrength(step.BStrength);
        var bytes = new List<byte>
        {
            0x35,
            0x11,
            0x01,
            High(aStrength),
            Low(aStrength),
            (byte)step.AMode,
            High(bStrength),
            Low(bStrength),
            (byte)step.BMode
        };
        bytes.Add(EmsPacketChecksum.Compute(bytes));
        return [.. bytes];
    }

    private static byte[] CreateV1StopPacket()
    {
        var bytes = new List<byte>
        {
            0x35,
            0x11,
            0x03,
            0x00,
            0x00,
            0x01,
            0x01,
            0x00,
            0x00
        };
        bytes.Add(EmsPacketChecksum.Compute(bytes));
        return [.. bytes];
    }

    private static byte ResolveV1Channel(EmsWaveformStep step)
    {
        var aEnabled = EmsWaveformStep.ClampStrength(step.AStrength) > 0;
        var bEnabled = EmsWaveformStep.ClampStrength(step.BStrength) > 0;
        return (aEnabled, bEnabled) switch
        {
            (true, true) => 0x03,
            (true, false) => 0x01,
            (false, true) => 0x02,
            _ => 0x00
        };
    }

    private static V1ChannelPayload ResolveV1SourceStep(EmsWaveformStep step, byte channel)
    {
        var aStrength = EmsWaveformStep.ClampStrength(step.AStrength);
        var bStrength = EmsWaveformStep.ClampStrength(step.BStrength);
        return channel switch
        {
            0x02 => new V1ChannelPayload(bStrength, step.BMode, step.BFrequency, step.BPulseWidth),
            0x03 when bStrength > aStrength => new V1ChannelPayload(bStrength, step.BMode, step.BFrequency, step.BPulseWidth),
            _ => new V1ChannelPayload(aStrength, step.AMode, step.AFrequency, step.APulseWidth)
        };
    }

    private static byte High(int value) => (byte)((value >> 8) & 0xFF);

    private static byte Low(int value) => (byte)(value & 0xFF);

    private readonly record struct V1ChannelPayload(int Strength, int Mode, int Frequency, int PulseWidth);
}
