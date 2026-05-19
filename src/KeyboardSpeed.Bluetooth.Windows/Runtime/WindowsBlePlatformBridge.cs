using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Versioning;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Diagnostics;
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
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", "开始执行扫描。");
        var discovered = new ConcurrentDictionary<string, BluetoothDeviceDescriptor>(StringComparer.OrdinalIgnoreCase);
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", "准备扫描蓝牙广播。");
        await ScanAdvertisementsAsync(discovered, cancellationToken);
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", $"蓝牙广播扫描完成，currentCount={discovered.Count}");
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", "准备扫描系统已知设备。");
        await ScanKnownDevicesAsync(discovered, cancellationToken);
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", $"系统已知设备扫描完成，totalCount={discovered.Count}");
        var result = discovered.Values
            .OrderBy(static item => item.DeviceType)
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAsync", $"扫描结果整理完成，deviceCount={result.Length}");
        return result;
    }

    public async Task<CoreBluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
    {
        try
        {
            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ConnectAsync",
                $"开始连接: id={device.DeviceId}, name={device.Name}, type={device.DeviceType}, profile={device.ProtocolProfile}");
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "准备清理旧连接状态。");
            await DisconnectAsync(cancellationToken);
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "旧连接状态清理完成，准备打开设备。");

            _connectedDevice = await OpenDeviceAsync(device.DeviceId);
            if (_connectedDevice is null)
            {
                _currentStatus = new CoreBluetoothConnectionStatus
                {
                    IsConnected = false,
                    Device = device,
                    LastError = "未能打开蓝牙设备。"
                };
                AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "连接失败：未能打开蓝牙设备。");
                return _currentStatus;
            }

            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ConnectAsync",
                $"设备已打开: name={_connectedDevice.Name}, connection={_connectedDevice.ConnectionStatus}");

            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "准备解析写入特征。");
            _writeCharacteristic = await ResolveWriteCharacteristicAsync(_connectedDevice, device.DeviceType);
            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ConnectAsync",
                $"写入特征解析完成: found={_writeCharacteristic is not null}");
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "准备解析通知特征。");
            _notifyCharacteristic = await ResolveNotifyCharacteristicAsync(_connectedDevice, device.DeviceType);
            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ConnectAsync",
                $"通知特征解析完成: found={_notifyCharacteristic is not null}");
            if (_writeCharacteristic is null)
            {
                _currentStatus = new CoreBluetoothConnectionStatus
                {
                    IsConnected = false,
                    Device = device,
                    LastError = "未找到可写入的蓝牙特征。"
                };
                AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "连接失败：未找到可写入特征。");
                return _currentStatus;
            }

            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ConnectAsync",
                $"特征解析完成: write={_writeCharacteristic.Uuid}, notify={_notifyCharacteristic?.Uuid.ToString() ?? "none"}");

            _currentStatus = new CoreBluetoothConnectionStatus
            {
                IsConnected = true,
                BatteryLevel = 100,
                Device = device
            };

            await SubscribeNotificationsAsync(cancellationToken);
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ConnectAsync", "通知订阅流程结束，准备刷新设备状态。");
            return await RefreshStatusAsync(_currentStatus, cancellationToken);
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("WindowsBlePlatformBridge.ConnectAsync", ex);
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
        AppDiagnostics.WriteInfo(
            "WindowsBlePlatformBridge.RefreshStatusAsync",
            $"开始刷新状态: connected={_currentStatus.IsConnected}, device={_currentStatus.Device?.Name ?? "none"}, hasWrite={_writeCharacteristic is not null}");
        if (!_currentStatus.IsConnected || _currentStatus.Device is null || _writeCharacteristic is null)
        {
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.RefreshStatusAsync", "刷新已跳过：连接状态不满足。");
            return _currentStatus;
        }

        if (_currentStatus.Device.DeviceType == BluetoothDeviceType.Ems)
        {
            foreach (var queryType in new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
            {
                AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.RefreshStatusAsync", $"发送 EMS 状态查询包: queryType=0x{queryType:X2}");
                await WriteAsync(BuildEmsQueryPacket(queryType), cancellationToken);
                await Task.Delay(40, cancellationToken);
            }
        }

        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.RefreshStatusAsync", "状态刷新完成。");
        return _currentStatus;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        AppDiagnostics.WriteInfo(
            "WindowsBlePlatformBridge.DisconnectAsync",
            $"开始断开设备: {_currentStatus.Device?.Name ?? _currentStatus.Device?.DeviceId ?? "none"}");
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
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.DisconnectAsync", "设备断开完成。");
    }

    public async Task WriteAsync(byte[] packet, CancellationToken cancellationToken = default)
    {
        if (_writeCharacteristic is null)
        {
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.WriteAsync", "跳过写入：当前没有可用的写特征。");
            return;
        }

        using var writer = new DataWriter();
        writer.WriteBytes(packet);
        var result = await _writeCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithoutResponse).AsTask(cancellationToken);
        if (result != GattCommunicationStatus.Success)
        {
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.WriteAsync", $"写入返回状态: {result}");
        }
    }

    private static async Task ScanAdvertisementsAsync(
        ConcurrentDictionary<string, BluetoothDeviceDescriptor> discovered,
        CancellationToken cancellationToken)
    {
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAdvertisementsAsync", "创建 BluetoothLEAdvertisementWatcher。");
        var watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += OnAdvertisementReceived;
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAdvertisementsAsync", "准备启动蓝牙广播监听。");
        watcher.Start();
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAdvertisementsAsync", "蓝牙广播监听已启动，等待 3 秒采集广告。");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        }
        finally
        {
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAdvertisementsAsync", "准备停止蓝牙广播监听。");
            watcher.Stop();
            watcher.Received -= OnAdvertisementReceived;
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanAdvertisementsAsync", $"蓝牙广播监听已停止，deviceCount={discovered.Count}");
        }

        void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher _, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
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
            catch (Exception ex)
            {
                AppDiagnostics.WriteException("WindowsBlePlatformBridge.ScanAdvertisementsAsync.Received", ex);
            }
        }
    }

    private static async Task ScanKnownDevicesAsync(
        ConcurrentDictionary<string, BluetoothDeviceDescriptor> discovered,
        CancellationToken cancellationToken)
    {
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanKnownDevicesAsync", "准备创建设备选择器。");
        var selector = BluetoothLEDevice.GetDeviceSelector();
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanKnownDevicesAsync", "准备查询系统已知 BLE 设备。");
        var devices = await DeviceInformation.FindAllAsync(selector).AsTask(cancellationToken);
        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ScanKnownDevicesAsync", $"系统已知 BLE 设备查询完成，count={devices.Count}");
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
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.OpenDeviceAsync", $"准备通过蓝牙地址打开设备: {deviceId}");
            var byAddress = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (byAddress is not null)
            {
                AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.OpenDeviceAsync", $"通过蓝牙地址打开设备成功: {deviceId}");
                return byAddress;
            }

            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.OpenDeviceAsync", $"通过蓝牙地址打开设备失败，准备回退到设备 Id: {deviceId}");
        }

        AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.OpenDeviceAsync", $"准备通过设备 Id 打开设备: {deviceId}");
        return await BluetoothLEDevice.FromIdAsync(deviceId);
    }

    private static async Task<GattCharacteristic?> ResolveWriteCharacteristicAsync(BluetoothLEDevice device, BluetoothDeviceType deviceType)
    {
        if (deviceType != BluetoothDeviceType.Ems)
        {
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ResolveWriteCharacteristicAsync", $"跳过写入特征解析: unsupportedDeviceType={deviceType}");
            return null;
        }

        var servicesResult = await device.GetGattServicesForUuidAsync(
            Guid.Parse(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid),
            BluetoothCacheMode.Uncached);
        AppDiagnostics.WriteInfo(
            "WindowsBlePlatformBridge.ResolveWriteCharacteristicAsync",
            $"服务查询完成: status={servicesResult.Status}, serviceCount={servicesResult.Services.Count}");
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            return null;
        }

        foreach (var service in servicesResult.Services)
        {
            var characteristicResult = await service.GetCharacteristicsForUuidAsync(
                Guid.Parse(EmsWriteUuid),
                BluetoothCacheMode.Uncached);
            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ResolveWriteCharacteristicAsync",
                $"写入特征查询完成: status={characteristicResult.Status}, characteristicCount={characteristicResult.Characteristics.Count}");
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
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.ResolveNotifyCharacteristicAsync", $"跳过通知特征解析: unsupportedDeviceType={deviceType}");
            return null;
        }

        var servicesResult = await device.GetGattServicesForUuidAsync(
            Guid.Parse(BluetoothAdvertisementDeviceClassifier.EmsServiceUuid),
            BluetoothCacheMode.Uncached);
        AppDiagnostics.WriteInfo(
            "WindowsBlePlatformBridge.ResolveNotifyCharacteristicAsync",
            $"服务查询完成: status={servicesResult.Status}, serviceCount={servicesResult.Services.Count}");
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            return null;
        }

        foreach (var service in servicesResult.Services)
        {
            var characteristicResult = await service.GetCharacteristicsForUuidAsync(
                Guid.Parse(EmsNotifyUuid),
                BluetoothCacheMode.Uncached);
            AppDiagnostics.WriteInfo(
                "WindowsBlePlatformBridge.ResolveNotifyCharacteristicAsync",
                $"通知特征查询完成: status={characteristicResult.Status}, characteristicCount={characteristicResult.Characteristics.Count}");
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
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.SubscribeNotificationsAsync", "当前设备没有通知特征。");
            return;
        }

        _notifyCharacteristic.ValueChanged -= OnNotifyCharacteristicValueChanged;
        var result = await _notifyCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
            GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(cancellationToken);
        if (result == GattCommunicationStatus.Success)
        {
            _notifyCharacteristic.ValueChanged += OnNotifyCharacteristicValueChanged;
            AppDiagnostics.WriteInfo("WindowsBlePlatformBridge.SubscribeNotificationsAsync", "通知特征订阅成功。");
            return;
        }

        AppDiagnostics.WriteInfo(
            "WindowsBlePlatformBridge.SubscribeNotificationsAsync",
            $"通知特征订阅失败，状态: {result}");
    }

    private void OnNotifyCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        try
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
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("WindowsBlePlatformBridge.OnNotifyCharacteristicValueChanged", ex);
        }
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
