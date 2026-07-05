using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Configuration;
using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Desktop.Services;
using KeyboardSpeed.Input.Windows;
using System.Reflection;

namespace KeyboardSpeed.Tests.Desktop;

public sealed class AppBootstrapperTriggerModeTests : IDisposable
{
    private readonly string _directoryPath;

    public AppBootstrapperTriggerModeTests()
    {
        _directoryPath = Path.Combine(Path.GetTempPath(), "KeyboardSpeed.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directoryPath);
    }

    [Fact]
    public async Task AnyKeypressMode_ShouldDispatchWaveformForEachCapturedKeystroke()
    {
        var settingsPath = Path.Combine(_directoryPath, "trigger-mode-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = WaveformTriggerMode.AnyKeypress,
            KeypressWaveformId = "soft-pulse"
        }, CancellationToken.None);

        var keyboardListener = new FakeKeyboardListener();
        var bridge = new FakeWindowsBlePlatformBridge();
        var deviceManager = new BleDeviceManager(bridge);
        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            keyboardListener,
            deviceManager,
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        await bootstrapper.ScanBluetoothAsync(CancellationToken.None);
        await bootstrapper.ConnectBluetoothAsync(bridge.Device.DeviceId, CancellationToken.None);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now, virtualKey: 65));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > 0, 1000));
        var firstPacketCount = deviceManager.PacketHistory.Count;

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now.AddMilliseconds(20), virtualKey: 66));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > firstPacketCount, 1000));
        Assert.Equal("按键即触发", bootstrapper.CurrentRuleName);
        Assert.Equal("柔和脉冲", bootstrapper.CurrentWaveformName);
    }

    [Fact]
    public async Task SpecificKeypressMode_ShouldOnlyDispatchWhenConfiguredKeyIsPressed()
    {
        var settingsPath = Path.Combine(_directoryPath, "specific-key-trigger-mode-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = WaveformTriggerMode.SpecificKeypress,
            SpecificKeyTriggers =
            [
                new SpecificKeyTriggerBinding
                {
                    VirtualKey = 0x11,
                    WaveformId = "wave-cascade"
                },
                new SpecificKeyTriggerBinding
                {
                    VirtualKey = 0x12,
                    WaveformId = "idle-jolt"
                }
            ]
        }, CancellationToken.None);

        var keyboardListener = new FakeKeyboardListener();
        var bridge = new FakeWindowsBlePlatformBridge();
        var deviceManager = new BleDeviceManager(bridge);
        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            keyboardListener,
            deviceManager,
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        await bootstrapper.ScanBluetoothAsync(CancellationToken.None);
        await bootstrapper.ConnectBluetoothAsync(bridge.Device.DeviceId, CancellationToken.None);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now, virtualKey: 0x13, isCounted: false));
        Assert.Empty(deviceManager.PacketHistory);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now.AddMilliseconds(20), virtualKey: 0x11, isCounted: false));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > 0, 1000));
        var firstDispatchCount = deviceManager.PacketHistory.Count;
        Assert.Equal("指定按键触发", bootstrapper.CurrentRuleName);
        Assert.Equal("波浪级联", bootstrapper.CurrentWaveformName);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now.AddMilliseconds(40), virtualKey: 0x12, isCounted: false));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > firstDispatchCount, 1000));
        Assert.Equal("高压警醒", bootstrapper.CurrentWaveformName);
        Assert.Equal(0, bootstrapper.CurrentSnapshot.ActiveSampleCount);
    }

    [Fact]
    public async Task SpecificKeypressMode_ShouldMigrateLegacySingleSpecificKeyConfiguration()
    {
        var settingsPath = Path.Combine(_directoryPath, "legacy-specific-key-trigger-mode-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = WaveformTriggerMode.SpecificKeypress,
            SpecificKeyVirtualKey = 0x11,
            SpecificKeyWaveformId = "wave-cascade"
        }, CancellationToken.None);

        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new FakeKeyboardListener(),
            new BleDeviceManager(new FakeWindowsBlePlatformBridge()),
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        var binding = Assert.Single(bootstrapper.SpecificKeyTriggers);
        Assert.Equal(0x11, binding.VirtualKey);
        Assert.Equal("wave-cascade", binding.WaveformId);
    }

    [Fact]
    public async Task HoldKeypressMode_ShouldLoopUntilLastHeldKeyIsReleased()
    {
        var settingsPath = Path.Combine(_directoryPath, "hold-keypress-trigger-mode-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = WaveformTriggerMode.HoldKeypress,
            KeypressWaveformId = "hold-wave",
            Waveforms =
            [
                new Core.Waveforms.EmsWaveformDefinition
                {
                    Id = "hold-wave",
                    Name = "按住波形",
                    Steps =
                    [
                        new Core.Waveforms.EmsWaveformStep
                        {
                            DurationMs = 2000,
                            AStrength = 18,
                            BStrength = 16
                        }
                    ]
                }
            ]
        }, CancellationToken.None);

        var keyboardListener = new FakeKeyboardListener();
        var bridge = new FakeWindowsBlePlatformBridge();
        var deviceManager = new BleDeviceManager(bridge);
        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            keyboardListener,
            deviceManager,
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        await bootstrapper.ScanBluetoothAsync(CancellationToken.None);
        await bootstrapper.ConnectBluetoothAsync(bridge.Device.DeviceId, CancellationToken.None);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now, virtualKey: 65));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > 0, 1000));
        var firstPacketCount = deviceManager.PacketHistory.Count;

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now.AddMilliseconds(20), virtualKey: 65));
        Assert.Equal(firstPacketCount, deviceManager.PacketHistory.Count);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(DateTimeOffset.Now.AddMilliseconds(40), virtualKey: 66));
        Assert.Equal(firstPacketCount, deviceManager.PacketHistory.Count);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(
            DateTimeOffset.Now.AddMilliseconds(60),
            virtualKey: 65,
            action: KeystrokeAction.Up));
        Assert.Equal(firstPacketCount, deviceManager.PacketHistory.Count);

        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(
            DateTimeOffset.Now.AddMilliseconds(80),
            virtualKey: 66,
            action: KeystrokeAction.Up));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > firstPacketCount, 1000));
        Assert.Equal("按住持续触发", bootstrapper.CurrentRuleName);
        Assert.Equal("已停止", bootstrapper.CurrentWaveformName);
    }

    [Fact]
    public async Task IdleTrigger_ShouldDispatchOnceAfterTimeoutAndRearmAfterNextKeystroke()
    {
        var settingsPath = Path.Combine(_directoryPath, "idle-trigger-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = WaveformTriggerMode.SpeedRules,
            IdleTriggerEnabled = true,
            IdleTriggerTimeoutMs = 500,
            IdleWaveformId = "idle-jolt",
            SpeedRules =
            [
                new SpeedRangeRule("high-only", "高速区", SpeedMetricType.Kpm, 300, 999, "sprint-burst", 600, true, true, true, true)
            ]
        }, CancellationToken.None);

        var keyboardListener = new FakeKeyboardListener();
        var bridge = new FakeWindowsBlePlatformBridge();
        var deviceManager = new BleDeviceManager(bridge);
        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            keyboardListener,
            deviceManager,
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        await bootstrapper.ScanBluetoothAsync(CancellationToken.None);
        await bootstrapper.ConnectBluetoothAsync(bridge.Device.DeviceId, CancellationToken.None);

        var firstKeystrokeAt = DateTimeOffset.Parse("2026-05-19T10:00:00+08:00");
        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(firstKeystrokeAt, virtualKey: 65));

        InvokePublishSnapshot(bootstrapper, firstKeystrokeAt.AddMilliseconds(450));
        Assert.Empty(deviceManager.PacketHistory);

        InvokePublishSnapshot(bootstrapper, firstKeystrokeAt.AddMilliseconds(500));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > 0, 1000));
        var firstDispatchCount = deviceManager.PacketHistory.Count;
        Assert.Equal("空闲超时触发", bootstrapper.CurrentRuleName);
        Assert.Equal("高压警醒", bootstrapper.CurrentWaveformName);

        InvokePublishSnapshot(bootstrapper, firstKeystrokeAt.AddMilliseconds(900));
        Assert.Equal(firstDispatchCount, deviceManager.PacketHistory.Count);

        var secondKeystrokeAt = firstKeystrokeAt.AddMilliseconds(950);
        keyboardListener.RaiseKeystroke(new KeystrokeCapturedEventArgs(secondKeystrokeAt, virtualKey: 66));

        InvokePublishSnapshot(bootstrapper, secondKeystrokeAt.AddMilliseconds(400));
        Assert.Equal(firstDispatchCount, deviceManager.PacketHistory.Count);

        InvokePublishSnapshot(bootstrapper, secondKeystrokeAt.AddMilliseconds(500));
        Assert.True(SpinWait.SpinUntil(() => deviceManager.PacketHistory.Count > firstDispatchCount, 1000));
    }

    [Fact]
    public async Task Bootstrapper_ShouldNormalizeLoadedRulesToRepeatWithinRangeWithShorterCooldown()
    {
        var settingsPath = Path.Combine(_directoryPath, "rule-repeat-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            SpeedRules =
            [
                new SpeedRangeRule("low", "低速区", SpeedMetricType.Kpm, 0, 119.99, "soft-pulse", 1500, true, true, false, true)
            ]
        }, CancellationToken.None);

        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new FakeKeyboardListener(),
            new BleDeviceManager(new FakeWindowsBlePlatformBridge()),
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        var rule = Assert.Single(bootstrapper.SpeedRules);
        Assert.True(rule.RepeatWithinRange);
        Assert.Equal(600, rule.CooldownMs);
    }

    [Fact]
    public async Task Bootstrapper_ShouldMigrateLegacyHeartbeatIdleReminderToIdleJolt()
    {
        var settingsPath = Path.Combine(_directoryPath, "idle-waveform-migration-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            IdleTriggerEnabled = true,
            IdleWaveformId = "heartbeat"
        }, CancellationToken.None);

        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new FakeKeyboardListener(),
            new BleDeviceManager(new FakeWindowsBlePlatformBridge()),
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        Assert.Equal("idle-jolt", bootstrapper.IdleWaveformId);
    }

    [Fact]
    public async Task Bootstrapper_ShouldMergeMissingBuiltinWaveformsIntoSavedWaveforms()
    {
        var settingsPath = Path.Combine(_directoryPath, "waveform-merge-settings.json");
        var settingsStore = new SettingsStore(settingsPath);
        await settingsStore.SaveAsync(new AppSettings
        {
            Waveforms =
            [
                new()
                {
                    Id = "soft-pulse",
                    Name = "我的柔和脉冲",
                    Steps =
                    [
                        new() { DurationMs = 120, AStrength = 11, BStrength = 9 }
                    ]
                },
                new()
                {
                    Id = "custom-wave",
                    Name = "自定义提醒",
                    Steps =
                    [
                        new() { DurationMs = 150, AStrength = 28, BStrength = 24 }
                    ]
                }
            ]
        }, CancellationToken.None);

        using var bootstrapper = new AppBootstrapper(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new FakeKeyboardListener(),
            new BleDeviceManager(new FakeWindowsBlePlatformBridge()),
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            settingsStore);

        Assert.Contains(bootstrapper.Waveforms, item => item.Id == "custom-wave");
        Assert.Contains(bootstrapper.Waveforms, item => item.Id == "idle-jolt");
        var softPulse = Assert.Single(bootstrapper.Waveforms, item => item.Id == "soft-pulse");
        Assert.Equal("我的柔和脉冲", softPulse.Name);
        Assert.Equal(2, bootstrapper.Waveforms.Take(2).Count(item => item.Id is "soft-pulse" or "custom-wave"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }

    private sealed class FakeKeyboardListener : IGlobalKeyboardListener
    {
        public event EventHandler<KeystrokeCapturedEventArgs>? KeystrokeCaptured;

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }

        public void RaiseKeystroke(KeystrokeCapturedEventArgs args)
        {
            KeystrokeCaptured?.Invoke(this, args);
        }
    }

    private sealed class FakeWindowsBlePlatformBridge : IWindowsBlePlatformBridge
    {
        public FakeWindowsBlePlatformBridge()
        {
            Device = new BluetoothDeviceDescriptor
            {
                DeviceId = "device-1",
                Name = "YYC-DJ-V2-001",
                DeviceType = BluetoothDeviceType.Ems,
                ProtocolProfile = BluetoothProtocolProfile.EmsV2
            };
        }

        public BluetoothDeviceDescriptor Device { get; }

        public event Action<BluetoothConnectionStatus>? StatusUpdated;

        public bool IsSupported => true;

        public Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<BluetoothDeviceDescriptor>>([Device]);
        }

        public Task<BluetoothConnectionStatus> ConnectAsync(BluetoothDeviceDescriptor device, CancellationToken cancellationToken = default)
        {
            var status = new BluetoothConnectionStatus
            {
                IsConnected = true,
                Device = device,
                BatteryLevel = 88
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

    private static void InvokePublishSnapshot(AppBootstrapper bootstrapper, DateTimeOffset now)
    {
        var method = typeof(AppBootstrapper).GetMethod("PublishSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(bootstrapper, [now]);
    }
}
