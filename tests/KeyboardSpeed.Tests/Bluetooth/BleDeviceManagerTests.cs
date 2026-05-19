using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Tests.Bluetooth;

public sealed class BleDeviceManagerTests
{
    [Fact]
    public async Task ScanAsync_ShouldExposePlatformDevices()
    {
        var expectedDevice = new BluetoothDeviceDescriptor
        {
            DeviceId = "dev-1",
            Name = "YYC-DJ-V2-001",
            DeviceType = BluetoothDeviceType.Ems,
            ProtocolProfile = BluetoothProtocolProfile.EmsV2,
            ServiceUuid = BluetoothAdvertisementDeviceClassifier.EmsServiceUuid
        };

        var manager = new BleDeviceManager(new FakeWindowsBlePlatformBridge([expectedDevice]));

        var devices = await manager.ScanAsync(CancellationToken.None);

        Assert.Single(devices);
        Assert.Equal("dev-1", devices[0].DeviceId);
    }

    [Fact]
    public async Task ConnectAsync_ShouldReturnFalseWhenDeviceDoesNotExist()
    {
        var manager = new BleDeviceManager(new FakeWindowsBlePlatformBridge([]));

        var connected = await manager.ConnectAsync("missing-device", CancellationToken.None);

        Assert.False(connected);
        Assert.Equal("未找到设备: missing-device", manager.CurrentStatus.LastError);
    }

    [Fact]
    public async Task ConnectAsync_ShouldReturnUnsupportedErrorWhenPlatformBridgeIsNotSupported()
    {
        var manager = new BleDeviceManager(new UnsupportedWindowsBlePlatformBridge());

        var connected = await manager.ConnectAsync("dev-1", CancellationToken.None);

        Assert.False(connected);
        Assert.Equal("当前系统不支持 Windows BLE 平台桥接。", manager.CurrentStatus.LastError);
    }

    [Fact]
    public async Task WriteAsync_ShouldStorePacketHistory()
    {
        var bridge = new FakeWindowsBlePlatformBridge([]);
        var manager = new BleDeviceManager(bridge);

        await manager.WriteAsync([0x35, 0x11, 0x01], CancellationToken.None);

        Assert.Single(manager.PacketHistory);
        Assert.Equal([0x35, 0x11, 0x01], manager.PacketHistory[0]);
    }

    [Fact]
    public async Task ConnectAsync_ShouldRemainSuccessfulWhenStatusSubscriberThrows()
    {
        var device = new BluetoothDeviceDescriptor
        {
            DeviceId = "dev-1",
            Name = "YYC-DJ-V2-001",
            DeviceType = BluetoothDeviceType.Ems,
            ProtocolProfile = BluetoothProtocolProfile.EmsV2,
            ServiceUuid = BluetoothAdvertisementDeviceClassifier.EmsServiceUuid
        };

        var bridge = new FakeWindowsBlePlatformBridge([device]);
        var manager = new BleDeviceManager(bridge);
        manager.StatusChanged += _ => throw new InvalidOperationException("UI handler failed");

        await manager.ScanAsync(CancellationToken.None);
        var connected = await manager.ConnectAsync(device.DeviceId, CancellationToken.None);

        Assert.True(connected);
        Assert.True(manager.CurrentStatus.IsConnected);
    }

    [Fact]
    public async Task Constructor_ShouldNotCreatePlatformBridgeUntilBleOperationStarts()
    {
        var factoryCalls = 0;
        var expectedDevice = new BluetoothDeviceDescriptor
        {
            DeviceId = "dev-1",
            Name = "YYC-DJ-V2-001"
        };

        var manager = new BleDeviceManager(() =>
        {
            factoryCalls++;
            return new FakeWindowsBlePlatformBridge([expectedDevice]);
        });

        Assert.Equal(0, factoryCalls);

        await manager.ScanAsync(CancellationToken.None);

        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnEmptyWithoutCallingPlatformWhenBridgeIsNotSupported()
    {
        var manager = new BleDeviceManager(new UnsupportedWindowsBlePlatformBridge());

        var devices = await manager.ScanAsync(CancellationToken.None);

        Assert.Empty(devices);
    }

    [Fact]
    public async Task ScanAsync_ShouldReturnEmptyAndStoreLastErrorWhenPlatformScanThrows()
    {
        var manager = new BleDeviceManager(new ThrowingScanWindowsBlePlatformBridge());

        var devices = await manager.ScanAsync(CancellationToken.None);

        Assert.Empty(devices);
        Assert.Equal("scan failed", manager.CurrentStatus.LastError);
    }

    [Fact]
    public async Task PlayWaveformAsync_ShouldSendStopPacketAfterWaveformDuration()
    {
        var device = new BluetoothDeviceDescriptor
        {
            DeviceId = "dev-1",
            Name = "YYC-DJ-V2-001",
            DeviceType = BluetoothDeviceType.Ems,
            ProtocolProfile = BluetoothProtocolProfile.EmsV2,
            ServiceUuid = BluetoothAdvertisementDeviceClassifier.EmsServiceUuid
        };

        var delayController = new ControlledDelay();
        var bridge = new FakeWindowsBlePlatformBridge([device]);
        var manager = new BleDeviceManager(() => bridge, delayController.DelayAsync);
        var waveform = new Core.Waveforms.EmsWaveformDefinition
        {
            Id = "soft-pulse",
            Name = "柔和脉冲",
            LoopCount = 2,
            Steps =
            [
                new Core.Waveforms.EmsWaveformStep { DurationMs = 30, AStrength = 18, BStrength = 16 },
                new Core.Waveforms.EmsWaveformStep { DurationMs = 20, AStrength = 24, BStrength = 20 }
            ]
        };

        await manager.ScanAsync(CancellationToken.None);
        await manager.ConnectAsync(device.DeviceId, CancellationToken.None);
        await manager.PlayWaveformAsync(waveform, CancellationToken.None);

        Assert.Equal(2, manager.PacketHistory.Count);
        Assert.Single(delayController.Requests);
        Assert.Equal(TimeSpan.FromMilliseconds(100), delayController.Requests[0].Delay);

        await delayController.CompleteAsync(0);

        Assert.True(SpinWait.SpinUntil(() => manager.PacketHistory.Count == 3, 1000));
        Assert.Equal([0x35, 0x11, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x49], manager.PacketHistory[^1]);
    }

    [Fact]
    public async Task PlayWaveformAsync_ShouldCancelPreviousPendingAutoStopWhenTriggeredAgain()
    {
        var device = new BluetoothDeviceDescriptor
        {
            DeviceId = "dev-1",
            Name = "YYC-DJ-V2-001",
            DeviceType = BluetoothDeviceType.Ems,
            ProtocolProfile = BluetoothProtocolProfile.EmsV2,
            ServiceUuid = BluetoothAdvertisementDeviceClassifier.EmsServiceUuid
        };

        var delayController = new ControlledDelay();
        var bridge = new FakeWindowsBlePlatformBridge([device]);
        var manager = new BleDeviceManager(() => bridge, delayController.DelayAsync);
        var waveform = new Core.Waveforms.EmsWaveformDefinition
        {
            Id = "soft-pulse",
            Name = "柔和脉冲",
            Steps =
            [
                new Core.Waveforms.EmsWaveformStep { DurationMs = 40, AStrength = 18, BStrength = 16 }
            ]
        };

        await manager.ScanAsync(CancellationToken.None);
        await manager.ConnectAsync(device.DeviceId, CancellationToken.None);
        await manager.PlayWaveformAsync(waveform, CancellationToken.None);
        await manager.PlayWaveformAsync(waveform, CancellationToken.None);

        Assert.Equal(2, delayController.Requests.Count);
        Assert.True(delayController.Requests[0].CancellationToken.IsCancellationRequested);

        await delayController.CompleteAsync(0);
        Assert.Equal(2, manager.PacketHistory.Count);

        await delayController.CompleteAsync(1);
        Assert.True(SpinWait.SpinUntil(() => manager.PacketHistory.Count == 3, 1000));
        Assert.Equal([0x35, 0x11, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x49], manager.PacketHistory[^1]);
    }

    private sealed class FakeWindowsBlePlatformBridge : IWindowsBlePlatformBridge
    {
        private readonly IReadOnlyList<BluetoothDeviceDescriptor> _devices;

        public FakeWindowsBlePlatformBridge(IReadOnlyList<BluetoothDeviceDescriptor> devices)
        {
            _devices = devices;
        }

        public event Action<BluetoothConnectionStatus>? StatusUpdated;

        public bool IsSupported => true;

        public Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_devices);
        }

        public Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
        {
            var status = new BluetoothConnectionStatus
            {
                IsConnected = true,
                BatteryLevel = 95,
                Device = device
            };
            StatusUpdated?.Invoke(status);
            return Task.FromResult(status);
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
            throw new InvalidOperationException("不支持的平台不应触发扫描。");
        }

        public Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("不支持的平台不应触发连接。");
        }

        public Task<BluetoothConnectionStatus> RefreshStatusAsync(BluetoothConnectionStatus currentStatus, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("不支持的平台不应刷新状态。");
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("不支持的平台不应断开连接。");
        }

        public Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("不支持的平台不应写入数据。");
        }
    }

    private sealed class ThrowingScanWindowsBlePlatformBridge : IWindowsBlePlatformBridge
    {
        public event Action<BluetoothConnectionStatus>? StatusUpdated
        {
            add { }
            remove { }
        }

        public bool IsSupported => true;

        public Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("scan failed");
        }

        public Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new BluetoothConnectionStatus
            {
                IsConnected = false,
                Device = device
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

    private sealed class ControlledDelay
    {
        private readonly List<DelayRequest> _requests = [];

        public IReadOnlyList<DelayRequest> Requests => _requests;

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            var request = new DelayRequest(delay, cancellationToken);
            _requests.Add(request);
            cancellationToken.Register(() => request.TryCancel());
            return request.Task;
        }

        public async Task CompleteAsync(int index)
        {
            _requests[index].TryComplete();
            try
            {
                await _requests[index].Task;
            }
            catch (TaskCanceledException)
            {
            }
        }
    }

    private sealed class DelayRequest
    {
        private readonly TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DelayRequest(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delay = delay;
            CancellationToken = cancellationToken;
        }

        public TimeSpan Delay { get; }

        public CancellationToken CancellationToken { get; }

        public Task Task => _taskCompletionSource.Task;

        public void TryComplete()
        {
            _taskCompletionSource.TrySetResult();
        }

        public void TryCancel()
        {
            _taskCompletionSource.TrySetCanceled(CancellationToken);
        }
    }
}
