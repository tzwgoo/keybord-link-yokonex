namespace KeyboardSpeed.Core.Bluetooth;

public interface IBluetoothDeviceManager
{
    BluetoothConnectionStatus CurrentStatus { get; }

    BluetoothTelemetrySnapshot GetTelemetrySnapshot();

    Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default);

    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default);
}
