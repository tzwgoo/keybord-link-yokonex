using System.Windows.Threading;
using System.IO;
using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Configuration;
using KeyboardSpeed.Core.Diagnostics;
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
    private readonly SettingsStore _settingsStore;
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
            new SettingsStore(GetSettingsFilePath()))
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener,
        BleDeviceManager bleDeviceManager,
        SpeedRuleCoordinator speedRuleCoordinator,
        SettingsStore settingsStore)
    {
        _typingSpeedCalculator = typingSpeedCalculator ?? throw new ArgumentNullException(nameof(typingSpeedCalculator));
        _keyboardListener = keyboardListener ?? throw new ArgumentNullException(nameof(keyboardListener));
        _bleDeviceManager = bleDeviceManager ?? throw new ArgumentNullException(nameof(bleDeviceManager));
        _speedRuleCoordinator = speedRuleCoordinator ?? throw new ArgumentNullException(nameof(speedRuleCoordinator));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        var settings = _settingsStore.LoadAsync().GetAwaiter().GetResult();
        _waveforms = settings.Waveforms.Count == 0
            ? BuiltinWaveforms.CreateDefaults().ToList()
            : settings.Waveforms.ToList();
        _speedRules = settings.SpeedRules.Count == 0
            ? AppSettings.CreateDefault().SpeedRules.ToList()
            : settings.SpeedRules.ToList();
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

    public BluetoothTelemetrySnapshot BluetoothTelemetry => _bleDeviceManager.GetTelemetrySnapshot();

    public int PacketHistoryCount => _bleDeviceManager.PacketHistory.Count;

    public IReadOnlyList<EmsWaveformDefinition> Waveforms => _waveforms;

    public IReadOnlyList<SpeedRangeRule> SpeedRules => _speedRules;

    public string CurrentRuleName => _currentRuleName;

    public string CurrentWaveformName => _currentWaveformName;

    public string SettingsFilePath => GetSettingsFilePath();

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

    public async Task AddOrUpdateWaveformAsync(string? existingWaveformId, string name, string script, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var trimmedName = string.IsNullOrWhiteSpace(name) ? "自定义波形" : name.Trim();
        var steps = WaveformScriptSerializer.Parse(script);
        var existing = _waveforms.FirstOrDefault(item => string.Equals(item.Id, existingWaveformId, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            _waveforms.Add(new EmsWaveformDefinition
            {
                Id = $"wave-{Guid.NewGuid():N}"[..13],
                Name = trimmedName,
                Steps = steps
            });
        }
        else
        {
            var index = _waveforms.IndexOf(existing);
            _waveforms[index] = existing with
            {
                Name = trimmedName,
                Steps = steps
            };
        }

        await SaveSettingsAsync(cancellationToken);
    }

    public async Task DeleteWaveformAsync(string waveformId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _waveforms.RemoveAll(item => string.Equals(item.Id, waveformId, StringComparison.OrdinalIgnoreCase));
        if (_waveforms.Count == 0)
        {
            _waveforms.AddRange(BuiltinWaveforms.CreateDefaults());
        }

        await SaveSettingsAsync(cancellationToken);
    }

    public async Task AddOrUpdateRuleAsync(
        string? existingRuleId,
        string name,
        double minValue,
        double maxValue,
        string waveformId,
        int cooldownMs,
        bool enabled,
        bool stopOnExit,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var trimmedName = string.IsNullOrWhiteSpace(name) ? "新规则" : name.Trim();
        var existing = _speedRules.FirstOrDefault(item => string.Equals(item.Id, existingRuleId, StringComparison.OrdinalIgnoreCase));
        var rule = new SpeedRangeRule(
            existing?.Id ?? $"rule-{Guid.NewGuid():N}"[..13],
            trimmedName,
            SpeedMetricType.Kpm,
            minValue,
            maxValue,
            waveformId,
            cooldownMs,
            enabled,
            true,
            false,
            stopOnExit);

        if (existing is null)
        {
            _speedRules.Add(rule);
        }
        else
        {
            var index = _speedRules.IndexOf(existing);
            _speedRules[index] = rule;
        }

        await SaveSettingsAsync(cancellationToken);
    }

    public async Task DeleteRuleAsync(string ruleId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _speedRules.RemoveAll(item => string.Equals(item.Id, ruleId, StringComparison.OrdinalIgnoreCase));
        if (_speedRules.Count == 0)
        {
            _speedRules.AddRange(AppSettings.CreateDefault().SpeedRules);
        }

        await SaveSettingsAsync(cancellationToken);
    }

    private void HandleKeystrokeCaptured(object? sender, KeystrokeCapturedEventArgs e)
    {
        LastKeystrokeAt = e.Timestamp;
        _typingSpeedCalculator.RecordKeystroke(e.Timestamp);
        PublishSnapshot(e.Timestamp);
    }

    private void HandleBluetoothStatusChanged(BluetoothConnectionStatus status)
    {
        if (BluetoothStatusUpdated is null)
        {
            return;
        }

        foreach (var handler in BluetoothStatusUpdated.GetInvocationList().Cast<Action<BluetoothConnectionStatus>>())
        {
            try
            {
                handler(status);
            }
            catch (Exception ex)
            {
                AppDiagnostics.WriteException("AppBootstrapper.BluetoothStatusUpdated", ex);
            }
        }
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

    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _settingsStore.SaveAsync(new AppSettings
        {
            SpeedRules = _speedRules.ToList(),
            Waveforms = _waveforms.ToList()
        }, cancellationToken);
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "KeyboardSpeed-YOKONEX", "app-settings.json");
    }
}
