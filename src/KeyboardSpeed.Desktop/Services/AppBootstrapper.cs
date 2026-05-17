using System.Windows.Threading;
using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;
using KeyboardSpeed.Input.Windows;

namespace KeyboardSpeed.Desktop.Services;

public sealed class AppBootstrapper : IDisposable
{
    private readonly TypingSpeedCalculator _typingSpeedCalculator;
    private readonly IGlobalKeyboardListener _keyboardListener;
    private readonly BleDeviceManager _bleDeviceManager;
    private readonly SpeedRuleCoordinator _speedRuleCoordinator;
    private readonly DispatcherTimer _snapshotTimer;
    private readonly List<EmsWaveformDefinition> _waveforms;
    private readonly List<SpeedRangeRule> _speedRules;
    private bool _disposed;
    private string _currentRuleName = "未命中";
    private string _currentWaveformName = "未触发";

    public AppBootstrapper()
        : this(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new GlobalKeyboardListener(),
            new BleDeviceManager(),
            new SpeedRuleCoordinator(new SpeedRuleEngine()),
            BuiltinWaveforms.CreateDefaults().ToList())
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener,
        BleDeviceManager bleDeviceManager,
        SpeedRuleCoordinator speedRuleCoordinator,
        List<EmsWaveformDefinition> waveforms)
    {
        _typingSpeedCalculator = typingSpeedCalculator ?? throw new ArgumentNullException(nameof(typingSpeedCalculator));
        _keyboardListener = keyboardListener ?? throw new ArgumentNullException(nameof(keyboardListener));
        _bleDeviceManager = bleDeviceManager ?? throw new ArgumentNullException(nameof(bleDeviceManager));
        _speedRuleCoordinator = speedRuleCoordinator ?? throw new ArgumentNullException(nameof(speedRuleCoordinator));
        _waveforms = waveforms ?? throw new ArgumentNullException(nameof(waveforms));
        _speedRules = CreateDefaultRules();
        _keyboardListener.KeystrokeCaptured += HandleKeystrokeCaptured;
        _bleDeviceManager.StatusChanged += HandleBluetoothStatusChanged;

        _snapshotTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _snapshotTimer.Tick += HandleSnapshotTimerTick;
        CurrentSnapshot = _typingSpeedCalculator.CreateSnapshot(DateTimeOffset.Now);
    }

    public event Action<TypingSpeedSnapshot>? SnapshotUpdated;

    public event Action<BluetoothConnectionStatus>? BluetoothStatusUpdated;

    public TypingSpeedSnapshot CurrentSnapshot { get; private set; }

    public DateTimeOffset? LastKeystrokeAt { get; private set; }

    public bool IsListening { get; private set; }

    public IReadOnlyList<BluetoothDeviceDescriptor> AvailableDevices => _bleDeviceManager.AvailableDevices;

    public BluetoothConnectionStatus BluetoothStatus => _bleDeviceManager.CurrentStatus;

    public IReadOnlyList<EmsWaveformDefinition> Waveforms => _waveforms;

    public string CurrentRuleName => _currentRuleName;

    public string CurrentWaveformName => _currentWaveformName;

    public void Start()
    {
        ThrowIfDisposed();

        _keyboardListener.Start();
        _snapshotTimer.Start();
        IsListening = true;
        PublishSnapshot(DateTimeOffset.Now);
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _snapshotTimer.Stop();
        _keyboardListener.Stop();
        IsListening = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _keyboardListener.KeystrokeCaptured -= HandleKeystrokeCaptured;
        _bleDeviceManager.StatusChanged -= HandleBluetoothStatusChanged;
        _keyboardListener.Dispose();
        _snapshotTimer.Tick -= HandleSnapshotTimerTick;
        _disposed = true;
    }

    public Task<IReadOnlyList<BluetoothDeviceDescriptor>> ScanBluetoothAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bleDeviceManager.ScanAsync(cancellationToken);
    }

    public Task<bool> ConnectBluetoothAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bleDeviceManager.ConnectAsync(deviceId, cancellationToken);
    }

    public Task DisconnectBluetoothAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bleDeviceManager.DisconnectAsync(cancellationToken);
    }

    public Task RefreshBluetoothAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bleDeviceManager.RefreshStatusAsync(cancellationToken);
    }

    public Task StopWaveformAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return _bleDeviceManager.StopAsync(cancellationToken);
    }

    public async Task PlayWaveformAsync(string waveformId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var waveform = _waveforms.FirstOrDefault(item => string.Equals(item.Id, waveformId, StringComparison.OrdinalIgnoreCase));
        if (waveform is null)
        {
            return;
        }

        _currentWaveformName = waveform.Name;
        await _bleDeviceManager.PlayWaveformAsync(waveform, cancellationToken);
    }

    private void HandleKeystrokeCaptured(object? sender, KeystrokeCapturedEventArgs e)
    {
        LastKeystrokeAt = e.Timestamp;
        _typingSpeedCalculator.RecordKeystroke(e.Timestamp);
        PublishSnapshot(e.Timestamp);
    }

    private void HandleBluetoothStatusChanged(BluetoothConnectionStatus status)
    {
        BluetoothStatusUpdated?.Invoke(status);
    }

    private void HandleSnapshotTimerTick(object? sender, EventArgs e)
    {
        PublishSnapshot(DateTimeOffset.Now);
    }

    private void PublishSnapshot(DateTimeOffset now)
    {
        CurrentSnapshot = _typingSpeedCalculator.CreateSnapshot(now);
        ApplySpeedRules(CurrentSnapshot);
        SnapshotUpdated?.Invoke(CurrentSnapshot);
    }

    private void ApplySpeedRules(TypingSpeedSnapshot snapshot)
    {
        var evaluation = _speedRuleCoordinator.Evaluate(snapshot, _speedRules, DateTimeOffset.Now);
        _currentRuleName = evaluation.ActiveRule?.Name ?? "未命中";

        if (evaluation.ShouldStop)
        {
            _currentWaveformName = "已停止";
            _ = _bleDeviceManager.StopAsync();
            return;
        }

        if (!evaluation.ShouldDispatch || string.IsNullOrWhiteSpace(evaluation.WaveformId))
        {
            return;
        }

        var waveform = _waveforms.FirstOrDefault(item => string.Equals(item.Id, evaluation.WaveformId, StringComparison.OrdinalIgnoreCase));
        if (waveform is null)
        {
            return;
        }

        _currentWaveformName = waveform.Name;
        if (!_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        _ = _bleDeviceManager.PlayWaveformAsync(waveform);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static List<SpeedRangeRule> CreateDefaultRules()
    {
        return
        [
            new SpeedRangeRule("low", "低速区", SpeedMetricType.Kpm, 0, 119.99, "soft-pulse", 1500, true, true, false, true),
            new SpeedRangeRule("mid", "中速区", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true)
        ];
    }
}
