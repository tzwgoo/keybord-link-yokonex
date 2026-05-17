using KeyboardSpeed.Bluetooth.Windows.Protocol;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Diagnostics;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public sealed class BleDeviceManager : IBluetoothDeviceManager
{
    private readonly IWindowsBlePlatformBridge _platformBridge;
    private readonly BluetoothTelemetryStore _telemetryStore = new();
    private readonly EmsBleProtocolAdapter _emsProtocolAdapter = new();
    private readonly List<BluetoothDeviceDescriptor> _availableDevices = [];
    private readonly List<byte[]> _packetHistory = [];
    private BluetoothConnectionStatus _status = new();

    public BleDeviceManager(IWindowsBlePlatformBridge? platformBridge = null)
    {
        _platformBridge = platformBridge ?? CreateDefaultPlatformBridge();
        _platformBridge.StatusUpdated += HandlePlatformStatusUpdated;
    }

    public event Action<BluetoothConnectionStatus>? StatusChanged;

    public IReadOnlyList<BluetoothDeviceDescriptor> AvailableDevices => _availableDevices;

    public IReadOnlyList<byte[]> PacketHistory => _packetHistory;

    public BluetoothConnectionStatus CurrentStatus => _status;

    public BluetoothTelemetrySnapshot GetTelemetrySnapshot() => _telemetryStore.GetSnapshot();

    public async Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _availableDevices.Clear();
        var devices = await _platformBridge.ScanAsync(cancellationToken);
        _availableDevices.AddRange(devices);
        return _availableDevices.ToArray();
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var device = _availableDevices.FirstOrDefault(item => string.Equals(item.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (device is null)
        {
            UpdateStatus(_status with
            {
                LastError = $"未找到设备: {deviceId}"
            });
            return false;
        }

        try
        {
            var status = await _platformBridge.ConnectAsync(device, cancellationToken);
            UpdateStatus(status with
            {
                Device = status.Device ?? device
            });
            return _status.IsConnected;
        }
        catch (Exception ex)
        {
            UpdateStatus(_status with
            {
                IsConnected = false,
                Device = device,
                LastError = ex.Message
            });
            return false;
        }
    }

    public async Task RefreshStatusAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_status.IsConnected && _status.Device is not null)
        {
            UpdateStatus(await _platformBridge.RefreshStatusAsync(_status, cancellationToken));
        }
        else
        {
            StatusChanged?.Invoke(_status);
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _platformBridge.DisconnectAsync(cancellationToken);
        _packetHistory.Clear();
        UpdateStatus(new BluetoothConnectionStatus());
    }

    public async Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(packet);

        if (packet.Length > 0)
        {
            _packetHistory.Add(packet.ToArray());
            if (_packetHistory.Count > 64)
            {
                _packetHistory.RemoveAt(0);
            }
        }

        if (_status.IsConnected)
        {
            await _platformBridge.WriteAsync(packet, cancellationToken);
        }
    }

    public async Task PlayWaveformAsync(EmsWaveformDefinition waveform, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(waveform);

        var device = _status.Device;
        if (!_status.IsConnected || device is null)
        {
            UpdateStatus(_status with
            {
                LastError = "蓝牙设备未连接，无法下发波形"
            });
            return;
        }

        foreach (var packet in _emsProtocolAdapter.CreatePackets(waveform, device))
        {
            await WriteAsync(packet, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        var device = _status.Device;
        if (device is null)
        {
            return Task.CompletedTask;
        }

        return WriteAsync(_emsProtocolAdapter.CreateStopPacket(device), cancellationToken);
    }

    private void UpdateStatus(BluetoothConnectionStatus status)
    {
        _status = status;
        _telemetryStore.RecordStatus(_status);
        if (StatusChanged is null)
        {
            return;
        }

        foreach (var handler in StatusChanged.GetInvocationList().Cast<Action<BluetoothConnectionStatus>>())
        {
            try
            {
                handler(_status);
            }
            catch (Exception ex)
            {
                AppDiagnostics.WriteException("BleDeviceManager.StatusChanged", ex);
            }
        }
    }

    private void HandlePlatformStatusUpdated(BluetoothConnectionStatus status)
    {
        try
        {
            UpdateStatus(status with
            {
                Device = status.Device ?? _status.Device
            });
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("BleDeviceManager.HandlePlatformStatusUpdated", ex);
        }
    }

    private static IWindowsBlePlatformBridge CreateDefaultPlatformBridge()
    {
        return OperatingSystem.IsWindows()
            ? new WindowsBlePlatformBridge()
            : new UnsupportedWindowsBlePlatformBridge();
    }

    private sealed class UnsupportedWindowsBlePlatformBridge : IWindowsBlePlatformBridge
    {
        public event Action<BluetoothConnectionStatus>? StatusUpdated
        {
            add { }
            remove { }
        }

        public bool IsSupported => false;

        public Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BluetoothDeviceDescriptor>>(Array.Empty<BluetoothDeviceDescriptor>());
        }

        public Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BluetoothConnectionStatus
            {
                IsConnected = false,
                Device = device,
                LastError = "当前系统不支持 Windows BLE 平台桥接。"
            });
        }

        public Task<BluetoothConnectionStatus> RefreshStatusAsync(BluetoothConnectionStatus currentStatus, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(currentStatus);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
