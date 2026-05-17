using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Versioning;
using KeyboardSpeed.Core.Bluetooth;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using CoreBluetoothConnectionStatus = KeyboardSpeed.Core.Bluetooth.BluetoothConnectionStatus;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

[SupportedOSPlatform("windows10.0.15063.0")]
public sealed class WindowsBlePlatformBridge : IWindowsBlePlatformBridge
{
    private const string EmsWriteUuid = "0000ff31-0000-1000-8000-00805f9b34fb";
    private const string EmsNotifyUuid = "0000ff32-0000-1000-8000-00805f9b34fb";

    private readonly BluetoothNotificationParser _notificationParser = new();
    private BluetoothLEDevice? _connectedDevice;
    private GattCharacteristic? _writeCharacteristic;
    private GattCharacteristic? _notifyCharacteristic;
    private CoreBluetoothConnectionStatus _currentStatus = new();

    public event Action<CoreBluetoothConnectionStatus>? StatusUpdated;

    public bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 15063);

    public async Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var discovered = new ConcurrentDictionary<string, BluetoothDeviceDescriptor>(StringComparer.OrdinalIgnoreCase);
        await ScanAdvertisementsAsync(discovered, cancellationToken);
        await ScanKnownDevicesAsync(discovered, cancellationToken);
        return discovered.Values
            .OrderBy(static item => item.DeviceType)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<CoreBluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
    {
        try
        {
            await DisconnectAsync(cancellationToken);

            _connectedDevice = await OpenDeviceAsync(device.DeviceId);
            if (_connectedDevice is null)
            {
                _currentStatus = new CoreBluetoothConnectionStatus
                {
                    IsConnected = false,
                    Device = device,
                    LastError = "未能打开蓝牙设备。"
                };
                return _currentStatus;
            }

            _writeCharacteristic = await ResolveWriteCharacteristicAsync(_connectedDevice, device.DeviceType);
            _notifyCharacteristic = await ResolveNotifyCharacteristicAsync(_connectedDevice, device.DeviceType);
            if (_writeCharacteristic is null)
            {
                _currentStatus = new CoreBluetoothConnectionStatus
                {
                    IsConnected = false,
                    Device = device,
                    LastError = "未找到可写入的蓝牙特征。"
                };
                return _currentStatus;
            }

            _currentStatus = new CoreBluetoothConnectionStatus
            {
                IsConnected = true,
                BatteryLevel = 100,
                Device = device
            };

            await SubscribeNotificationsAsync(cancellationToken);
            return await RefreshStatusAsync(_currentStatus, cancellationToken);
        }
        catch (Exception ex)
        {
            _currentStatus = new CoreBluetoothConnectionStatus
            {
                IsConnected = false,
                Device = device,
                LastError = ex.Message
            };
            return _currentStatus;
        }
    }

    public async Task<CoreBluetoothConnectionStatus> RefreshStatusAsync(CoreBluetoothConnectionStatus currentStatus, CancellationToken cancellationToken = default)
    {
        _currentStatus = currentStatus;
        if (!_currentStatus.IsConnected || _currentStatus.Device is null || _writeCharacteristic is null)
        {
            return _currentStatus;
        }

        if (_currentStatus.Device.DeviceType == BluetoothDeviceType.Ems)
        {
            foreach (var queryType in new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
            {
                await WriteAsync(BuildEmsQueryPacket(queryType), cancellationToken);
                await Task.Delay(40, cancellationToken);
            }
        }

        return _currentStatus;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_notifyCharacteristic is not null)
        {
            _notifyCharacteristic.ValueChanged -= OnNotifyCharacteristicValueChanged;
        }

        _notifyCharacteristic = null;
        _writeCharacteristic = null;

        if (_connectedDevice is not null)
        {
            await Task.Yield();
            _connectedDevice.Dispose();
            _connectedDevice = null;
        }

        _currentStatus = new CoreBluetoothConnectionStatus();
    }

    public async Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic is null)
        {
            return;
        }

        using var writer = new DataWriter();
        writer.WriteBytes(packet);
        await _writeCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse).AsTask(cancellationToken);
    }

    private static async Task ScanAdvertisementsAsync(
        ConcurrentDictionary<string, BluetoothDeviceDescriptor> discovered,
        CancellationToken cancellationToken)
    {
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += OnAdvertisementReceived;
        watcher.Start();
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
        finally
        {
            watcher.Stop();
            watcher.Received -= OnAdvertisementReceived;
        }

        void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher _, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            if (!BluetoothAdvertisementDeviceClassifier.TryResolveDeviceType(
                    args.Advertisement.ServiceUuids,
                    args.Advertisement.LocalName,
                    out var deviceType,
                    out var serviceUuid))
            {
                return;
            }

            var deviceId = args.BluetoothAddress.ToString("X12", CultureInfo.InvariantCulture);
            var name = string.IsNullOrWhiteSpace(args.Advertisement.LocalName)
                ? $"{BuildDeviceTypePrefix(deviceType)}_{deviceId}"
                : args.Advertisement.LocalName;

            discovered[deviceId] = new BluetoothDeviceDescriptor
            {
                DeviceId = deviceId,
                Name = name,
                DeviceType = deviceType,
                ProtocolProfile = BluetoothAdvertisementDeviceClassifier.ResolveProtocolProfile(deviceType, name),
                ServiceUuid = serviceUuid
            };
        }
    }

    private static async Task ScanKnownDevicesAsync(
        ConcurrentDictionary<string, BluetoothDeviceDescriptor> discovered,
        CancellationToken cancellationToken)
    {
        var selector = BluetoothLEDevice.GetDeviceSelector();
        var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);
        foreach (var device in devices)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var descriptor = await TryCreateDescriptorAsync(device);
            if (descriptor is not null)
            {
                discovered[descriptor.DeviceId] = descriptor;
            }
        }
    }

    private static async Task<BluetoothDeviceDescriptor?> TryCreateDescriptorAsync(DeviceInformation device)
    {
        if (BluetoothAdvertisementDeviceClassifier.TryResolveDeviceType(
                Array.Empty<Guid>(),
                device.Name,
                out var nameMatchedType,
                out var nameMatchedServiceUuid))
        {
            return new BluetoothDeviceDescriptor
            {
                DeviceId = device.Id,
                Name = device.Name,
                DeviceType = nameMatchedType,
                ProtocolProfile = BluetoothAdvertisementDeviceClassifier.ResolveProtocolProfile(nameMatchedType, device.Name),
                ServiceUuid = nameMatchedServiceUuid
            };
        }

        var bleDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
        if (bleDevice is null)
        {
            return null;
        }

        try
        {
            var servicesResult = await bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (servicesResult.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            foreach (var service in servicesResult.Services)
            {
                var serviceUuid = service.Uuid.ToString().ToLowerInvariant();
                if (serviceUuid == BluetoothAdvertisementDeviceClassifier.EmsServiceUuid)
                {
                    var resolvedName = string.IsNullOrWhiteSpace(device.Name) ? bleDevice.Name : device.Name;
                    return new BluetoothDeviceDescriptor
                    {
                        DeviceId = device.Id,
                        Name = resolvedName,
                        DeviceType = BluetoothDeviceType.Ems,
                        ProtocolProfile = BluetoothAdvertisementDeviceClassifier.ResolveProtocolProfile(BluetoothDeviceType.Ems, resolvedName),
                        ServiceUuid = service.Uuid.ToString()
                    };
                }
            }

            return null;
        }
        finally
        {
            bleDevice.Dispose();
        }
    }

    private static async Task<BluetoothLEDevice?> OpenDeviceAsync(string deviceId)
    {
        if (ulong.TryParse(deviceId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bluetoothAddress))
        {
            var byAddress = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (byAddress is not null)
            {
                return byAddress;
            }
        }

        return await BluetoothLEDevice.FromIdAsync(deviceId);
    }

    private static async Task<GattCharacteristic?> ResolveWriteCharacteristicAsync(BluetoothLEDevice device, BluetoothDeviceType deviceType)
    {
        if (deviceType != BluetoothDeviceType.Ems)
        {
            return null;
        }

        var servicesResult = await device.GetGattServicesForUuidAsync(
            Guid.Parse(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid),
            BluetoothCacheMode.Uncached);
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            return null;
        }

        foreach (var service in servicesResult.Services)
        {
            var characteristicResult = await service.GetCharacteristicsForUuidAsync(
                Guid.Parse(EmsWriteUuid),
                BluetoothCacheMode.Uncached);
            if (characteristicResult.Status == GattCommunicationStatus.Success)
            {
                return characteristicResult.Characteristics.FirstOrDefault();
            }
        }

        return null;
    }

    private static async Task<GattCharacteristic?> ResolveNotifyCharacteristicAsync(BluetoothLEDevice device, BluetoothDeviceType deviceType)
    {
        if (deviceType != BluetoothDeviceType.Ems)
        {
            return null;
        }

        var servicesResult = await device.GetGattServicesForUuidAsync(
            Guid.Parse(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid),
            BluetoothCacheMode.Uncached);
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            return null;
        }

        foreach (var service in servicesResult.Services)
        {
            var characteristicResult = await service.GetCharacteristicsForUuidAsync(
                Guid.Parse(EmsNotifyUuid),
                BluetoothCacheMode.Uncached);
            if (characteristicResult.Status == GattCommunicationStatus.Success)
            {
                return characteristicResult.Characteristics.FirstOrDefault();
            }
        }

        return null;
    }

    private async Task SubscribeNotificationsAsync(CancellationToken cancellationToken)
    {
        if (_notifyCharacteristic is null)
        {
            return;
        }

        _notifyCharacteristic.ValueChanged -= OnNotifyCharacteristicValueChanged;
        var result = await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(cancellationToken);
        if (result == GattCommunicationStatus.Success)
        {
            _notifyCharacteristic.ValueChanged += OnNotifyCharacteristicValueChanged;
        }
    }

    private void OnNotifyCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        if (_currentStatus.Device is null)
        {
            return;
        }

        var reader = DataReader.FromBuffer(args.CharacteristicValue);
        var packet = new byte[reader.UnconsumedBufferLength];
        reader.ReadBytes(packet);

        if (_notificationParser.TryReadBatteryLevel(_currentStatus.Device.DeviceType, packet, out var batteryLevel))
        {
            _currentStatus = _currentStatus with
            {
                BatteryLevel = batteryLevel
            };
        }

        var parsedStatus = _notificationParser.ParseStatus(_currentStatus.Device.DeviceType, packet);
        if (parsedStatus is not null)
        {
            _currentStatus = _currentStatus with
            {
                BatteryLevel = parsedStatus.BatteryLevel ?? _currentStatus.BatteryLevel,
                ChannelAElectrodeStatus = parsedStatus.ChannelAElectrodeStatus ?? _currentStatus.ChannelAElectrodeStatus,
                ChannelAEnabled = parsedStatus.ChannelAEnabled ?? _currentStatus.ChannelAEnabled,
                ChannelAStrength = parsedStatus.ChannelAStrength ?? _currentStatus.ChannelAStrength,
                ChannelAMode = parsedStatus.ChannelAMode ?? _currentStatus.ChannelAMode,
                ChannelBElectrodeStatus = parsedStatus.ChannelBElectrodeStatus ?? _currentStatus.ChannelBElectrodeStatus,
                ChannelBEnabled = parsedStatus.ChannelBEnabled ?? _currentStatus.ChannelBEnabled,
                ChannelBStrength = parsedStatus.ChannelBStrength ?? _currentStatus.ChannelBStrength,
                ChannelBMode = parsedStatus.ChannelBMode ?? _currentStatus.ChannelBMode,
                MotorState = parsedStatus.MotorState ?? _currentStatus.MotorState,
                StepCount = parsedStatus.StepCount ?? _currentStatus.StepCount,
                ErrorCode = parsedStatus.ErrorCode ?? _currentStatus.ErrorCode,
                LastError = string.Empty
            };
        }

        StatusUpdated?.Invoke(_currentStatus);
    }

    private static byte[] BuildEmsQueryPacket(byte queryType)
    {
        var bytes = new byte[] { 0x35, 0x71, queryType };
        var checksum = (byte)((bytes[0] + bytes[1] + bytes[2]) & 0xFF);
        return [bytes[0], bytes[1], bytes[2], checksum];
    }

    private static string BuildDeviceTypePrefix(BluetoothDeviceType deviceType)
    {
        return deviceType switch
        {
            BluetoothDeviceType.Ems => "EMS",
            BluetoothDeviceType.Toy => "TOY",
            _ => "BLE"
        };
    }
}
