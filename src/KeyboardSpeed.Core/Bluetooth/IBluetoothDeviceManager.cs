namespace KeyboardSpeed.Core.Bluetooth;

public interface IBluetoothDeviceManager
{
    event Action<BluetoothConnectionStatus>? StatusChanged;

    IReadOnlyList<BluetoothDeviceDescriptor> AvailableDevices { get; }

    IReadOnlyList<byte[]> PacketHistory { get; }

    BluetoothConnectionStatus CurrentStatus { get; }

    BluetoothTelemetrySnapshot GetTelemetrySnapshot();

    Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default);

    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);

    Task RefreshStatusAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default);
}
