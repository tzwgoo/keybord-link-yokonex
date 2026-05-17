using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public interface IWindowsBlePlatformBridge
{
    event Action<BluetoothConnectionStatus>? StatusUpdated;

    bool IsSupported { get; }

    Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default);

    Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default);

    Task<BluetoothConnectionStatus> RefreshStatusAsync(BluetoothConnectionStatus currentStatus, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default);
}
