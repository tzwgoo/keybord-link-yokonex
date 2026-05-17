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
}
