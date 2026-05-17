namespace KeyboardSpeed.Core.Bluetooth;

public sealed record BluetoothDeviceDescriptor
{
    public string DeviceId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public BluetoothDeviceType DeviceType { get; init; } = BluetoothDeviceType.Unknown;

    public BluetoothProtocolProfile ProtocolProfile { get; init; } = BluetoothProtocolProfile.Unknown;

    public string ServiceUuid { get; init; } = string.Empty;
}
