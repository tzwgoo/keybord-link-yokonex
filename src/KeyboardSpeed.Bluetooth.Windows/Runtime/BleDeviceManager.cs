using KeyboardSpeed.Bluetooth.Windows.Protocol;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Diagnostics;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public sealed class BleDeviceManager : IBluetoothDeviceManager
{
    private const string UnsupportedPlatformMessage = "当前系统不支持 Windows BLE 平台桥接。";
    private readonly Func<IWindowsBlePlatformBridge> _platformBridgeFactory;
    private readonly Func<TimeSpan, CancellationToken, Task> _delayAsync;
    private IWindowsBlePlatformBridge? _platformBridge;
    private readonly BluetoothTelemetryStore _telemetryStore = new();
    private readonly EmsBleProtocolAdapter _emsProtocolAdapter = new();
    private readonly List<BluetoothDeviceDescriptor> _availableDevices = [];
    private readonly List<byte[]> _packetHistory = [];
    private CancellationTokenSource? _pendingAutoStopCts;
    private BluetoothConnectionStatus _status = new();

    public BleDeviceManager(IWindowsBlePlatformBridge? platformBridge = null)
        : this(() => platformBridge ?? CreateDefaultPlatformBridge(), DefaultDelayAsync)
    {
    }

    public BleDeviceManager(Func<IWindowsBlePlatformBridge> platformBridgeFactory)
        : this(platformBridgeFactory, DefaultDelayAsync)
    {
    }

    public BleDeviceManager(
        Func<IWindowsBlePlatformBridge> platformBridgeFactory,
        Func<TimeSpan, CancellationToken, Task> delayAsync)
    {
        _platformBridgeFactory = platformBridgeFactory ?? throw new ArgumentNullException(nameof(platformBridgeFactory));
        _delayAsync = delayAsync ?? throw new ArgumentNullException(nameof(delayAsync));
    }

    public event Action<BluetoothConnectionStatus>? StatusChanged;

    public IReadOnlyList<BluetoothDeviceDescriptor> AvailableDevices => _availableDevices;

    public IReadOnlyList<byte[]> PacketHistory => _packetHistory;

    public BluetoothConnectionStatus CurrentStatus => _status;

    public BluetoothTelemetrySnapshot GetTelemetrySnapshot() => _telemetryStore.GetSnapshot();

    public async Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bridge = GetOrCreatePlatformBridge();
        AppDiagnostics.WriteInfo("BleDeviceManager.ScanAsync", $"开始扫描，platformSupported={bridge.IsSupported}");

        _availableDevices.Clear();
        if (!bridge.IsSupported)
        {
            AppDiagnostics.WriteInfo("BleDeviceManager.ScanAsync", $"扫描已跳过：{UnsupportedPlatformMessage}");
            return Array.Empty<BluetoothDeviceDescriptor>();
        }

        try
        {
            var devices = await bridge.ScanAsync(cancellationToken);
            _availableDevices.AddRange(devices);
            AppDiagnostics.WriteInfo("BleDeviceManager.ScanAsync", $"扫描完成，deviceCount={_availableDevices.Count}");
            return _availableDevices.ToArray();
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("BleDeviceManager.ScanAsync", ex);
            UpdateStatus(_status with
            {
                LastError = ex.Message
            });
            return Array.Empty<BluetoothDeviceDescriptor>();
        }
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var bridge = GetOrCreatePlatformBridge();
        AppDiagnostics.WriteInfo("BleDeviceManager.ConnectAsync", $"准备连接设备: {deviceId}, platformSupported={bridge.IsSupported}");

        if (!bridge.IsSupported)
        {
            UpdateStatus(_status with
            {
                IsConnected = false,
                LastError = UnsupportedPlatformMessage
            });
            AppDiagnostics.WriteInfo("BleDeviceManager.ConnectAsync", $"连接已跳过：{UnsupportedPlatformMessage}");
            return false;
        }

        var device = _availableDevices.FirstOrDefault(item => string.Equals(item.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
        if (device is null)
        {
            UpdateStatus(_status with
            {
                LastError = $"未找到设备: {deviceId}"
            });
            AppDiagnostics.WriteInfo("BleDeviceManager.ConnectAsync", $"连接失败，设备不存在: {deviceId}");
            return false;
        }

        try
        {
            var status = await bridge.ConnectAsync(device, cancellationToken);
            UpdateStatus(status with
            {
                Device = status.Device ?? device
            });
            AppDiagnostics.WriteInfo(
                "BleDeviceManager.ConnectAsync",
                $"连接完成: connected={_status.IsConnected}, device={_status.Device?.Name ?? device.Name}, lastError={_status.LastError}");
            return _status.IsConnected;
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("BleDeviceManager.ConnectAsync", ex);
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

        if (_status.IsConnected && _status.Device is not null && _platformBridge is not null)
        {
            if (!_platformBridge.IsSupported)
            {
                AppDiagnostics.WriteInfo("BleDeviceManager.RefreshStatusAsync", $"刷新已跳过：{UnsupportedPlatformMessage}");
                return;
            }

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
        AppDiagnostics.WriteInfo("BleDeviceManager.DisconnectAsync", $"断开设备: {_status.Device?.Name ?? _status.Device?.DeviceId ?? "none"}");
        CancelPendingAutoStop();
        if (_platformBridge is not null && _platformBridge.IsSupported)
        {
            await _platformBridge.DisconnectAsync(cancellationToken);
        }
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
            var bridge = GetOrCreatePlatformBridge();
            if (!bridge.IsSupported)
            {
                AppDiagnostics.WriteInfo("BleDeviceManager.WriteAsync", $"写入已跳过：{UnsupportedPlatformMessage}");
                return;
            }

            await bridge.WriteAsync(packet, cancellationToken);
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

        ScheduleAutoStop(waveform);
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        CancelPendingAutoStop();
        return StopCoreAsync(cancellationToken);
    }

    private Task StopCoreAsync(CancellationToken cancellationToken)
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

    private IWindowsBlePlatformBridge GetOrCreatePlatformBridge()
    {
        if (_platformBridge is not null)
        {
            return _platformBridge;
        }

        _platformBridge = _platformBridgeFactory();
        _platformBridge.StatusUpdated += HandlePlatformStatusUpdated;
        return _platformBridge;
    }

    private void ScheduleAutoStop(EmsWaveformDefinition waveform)
    {
        CancelPendingAutoStop();

        var duration = ResolveWaveformDuration(waveform);
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        var stopCts = new CancellationTokenSource();
        _pendingAutoStopCts = stopCts;
        _ = ExecutePendingAutoStopAsync(duration, stopCts);
    }

    private async Task ExecutePendingAutoStopAsync(TimeSpan duration, CancellationTokenSource stopCts)
    {
        try
        {
            await _delayAsync(duration, stopCts.Token);
            if (stopCts.IsCancellationRequested)
            {
                return;
            }

            await StopCoreAsync(stopCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("BleDeviceManager.ExecutePendingAutoStopAsync", ex);
        }
        finally
        {
            if (ReferenceEquals(_pendingAutoStopCts, stopCts))
            {
                _pendingAutoStopCts = null;
            }

            stopCts.Dispose();
        }
    }

    private void CancelPendingAutoStop()
    {
        if (_pendingAutoStopCts is null)
        {
            return;
        }

        try
        {
            _pendingAutoStopCts.Cancel();
        }
        catch
        {
        }

        _pendingAutoStopCts = null;
    }

    private static TimeSpan ResolveWaveformDuration(EmsWaveformDefinition waveform)
    {
        var totalStepDurationMs = waveform.Steps.Sum(static step => Math.Max(1, step.DurationMs));
        var totalDurationMs = Math.Max(1, waveform.LoopCount) * totalStepDurationMs;
        return TimeSpan.FromMilliseconds(totalDurationMs);
    }

    private static Task DefaultDelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
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
                LastError = UnsupportedPlatformMessage
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
