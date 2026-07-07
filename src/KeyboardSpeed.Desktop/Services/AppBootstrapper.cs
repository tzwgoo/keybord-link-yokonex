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
    private readonly IGlobalMouseListener _mouseListener;
    private readonly BleDeviceManager _bleDeviceManager;
    private readonly SpeedRuleCoordinator _speedRuleCoordinator;
    private readonly WaveformTriggerRouter _waveformTriggerRouter;
    private readonly SettingsStore _settingsStore;
    private readonly DispatcherTimer _snapshotTimer;
    private readonly List<EmsWaveformDefinition> _waveforms;
    private readonly List<SpeedRangeRule> _speedRules;
    private readonly List<SpecificKeyTriggerBinding> _specificKeyTriggers;
    private readonly HashSet<int> _holdPlaybackVirtualKeys = [];
    private bool _disposed;
    private WaveformTriggerMode _triggerMode;
    private string _keypressWaveformId = string.Empty;
    private MouseClickTriggerPayloadType _mouseClickPayloadType;
    private int _mouseClickFixedAStrength;
    private int _mouseClickFixedBStrength;
    private int _mouseClickFixedDurationMs;
    private bool _idleTriggerEnabled;
    private int _idleTriggerTimeoutMs;
    private string _idleWaveformId = string.Empty;
    private bool _idleTriggerDispatched;
    private string _currentRuleName = "未命中";
    private string _currentWaveformName = "未触发";
    private CancellationTokenSource? _holdPlaybackCts;

    public AppBootstrapper()
        : this(
            CreateDefaultTypingSpeedCalculator(),
            CreateDefaultKeyboardListener(),
            CreateDefaultMouseListener(),
            CreateDefaultBleDeviceManager(),
            CreateDefaultSpeedRuleCoordinator(),
            CreateDefaultSettingsStore())
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener,
        BleDeviceManager bleDeviceManager,
        SpeedRuleCoordinator speedRuleCoordinator,
        SettingsStore settingsStore)
        : this(
            typingSpeedCalculator,
            keyboardListener,
            CreateDefaultMouseListener(),
            bleDeviceManager,
            speedRuleCoordinator,
            settingsStore)
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener,
        IGlobalMouseListener mouseListener,
        BleDeviceManager bleDeviceManager,
        SpeedRuleCoordinator speedRuleCoordinator,
        SettingsStore settingsStore)
    {
        _typingSpeedCalculator = typingSpeedCalculator ?? throw new ArgumentNullException(nameof(typingSpeedCalculator));
        _keyboardListener = keyboardListener ?? throw new ArgumentNullException(nameof(keyboardListener));
        _mouseListener = mouseListener ?? throw new ArgumentNullException(nameof(mouseListener));
        _bleDeviceManager = bleDeviceManager ?? throw new ArgumentNullException(nameof(bleDeviceManager));
        _speedRuleCoordinator = speedRuleCoordinator ?? throw new ArgumentNullException(nameof(speedRuleCoordinator));
        _waveformTriggerRouter = new WaveformTriggerRouter(_speedRuleCoordinator);
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        AppDiagnostics.WriteInfo("AppBootstrapper.ctor", "开始读取本地配置。");
        var settings = _settingsStore.Load();
        AppDiagnostics.WriteInfo("AppBootstrapper.ctor", $"配置读取完成：rules={settings.SpeedRules.Count}, waveforms={settings.Waveforms.Count}");
        _waveforms = MergeWaveformsWithBuiltins(settings.Waveforms);
        var loadedRules = settings.SpeedRules.Count == 0
            ? AppSettings.CreateDefault().SpeedRules
            : settings.SpeedRules;
        _speedRules = SpeedRuleMetricNormalizer.NormalizeToCharactersPerMinute(loadedRules.Select(NormalizeRuntimeRuleBehavior)).ToList();
        _specificKeyTriggers = NormalizeSpecificKeyTriggers(
            settings.SpecificKeyTriggers,
            settings.SpecificKeyVirtualKey,
            settings.SpecificKeyWaveformId);
        _triggerMode = settings.TriggerMode;
        _keypressWaveformId = NormalizeKeypressWaveformId(settings.KeypressWaveformId);
        _mouseClickPayloadType = settings.MouseClickPayloadType;
        _mouseClickFixedAStrength = NormalizeMouseClickFixedStrength(settings.MouseClickFixedAStrength);
        _mouseClickFixedBStrength = NormalizeMouseClickFixedStrength(settings.MouseClickFixedBStrength);
        _mouseClickFixedDurationMs = NormalizeMouseClickFixedDurationMs(settings.MouseClickFixedDurationMs);
        _idleTriggerEnabled = settings.IdleTriggerEnabled;
        _idleTriggerTimeoutMs = NormalizeIdleTriggerTimeoutMs(settings.IdleTriggerTimeoutMs);
        _idleWaveformId = NormalizeIdleWaveformId(settings.IdleWaveformId);
        AppDiagnostics.WriteInfo("AppBootstrapper.ctor", $"规则归一化完成：rules={_speedRules.Count}, waveforms={_waveforms.Count}");
        _keyboardListener.KeystrokeCaptured += HandleKeystrokeCaptured;
        _mouseListener.MouseClickCaptured += HandleMouseClickCaptured;
        _bleDeviceManager.StatusChanged += HandleBluetoothStatusChanged;

        _snapshotTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _snapshotTimer.Tick += HandleSnapshotTimerTick;
        CurrentSnapshot = _typingSpeedCalculator.CreateSnapshot(DateTimeOffset.Now);
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
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

    public WaveformTriggerMode TriggerMode => _triggerMode;

    public string KeypressWaveformId => _keypressWaveformId;

    public MouseClickTriggerPayloadType MouseClickPayloadType => _mouseClickPayloadType;

    public int MouseClickFixedAStrength => _mouseClickFixedAStrength;

    public int MouseClickFixedBStrength => _mouseClickFixedBStrength;

    public int MouseClickFixedDurationMs => _mouseClickFixedDurationMs;

    public IReadOnlyList<SpecificKeyTriggerBinding> SpecificKeyTriggers => _specificKeyTriggers;

    public bool IdleTriggerEnabled => _idleTriggerEnabled;

    public int IdleTriggerTimeoutMs => _idleTriggerTimeoutMs;

    public string IdleWaveformId => _idleWaveformId;

    public string CurrentRuleName => _currentRuleName;

    public string CurrentWaveformName => _currentWaveformName;

    public string SettingsFilePath => GetSettingsFilePath();

    public void Start()
    {
        ThrowIfDisposed();

        _keyboardListener.Start();
        _mouseListener.Start();
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
        _mouseListener.Stop();
        IsListening = false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        CancelHoldPlayback(stopDevice: true);
        _keyboardListener.KeystrokeCaptured -= HandleKeystrokeCaptured;
        _mouseListener.MouseClickCaptured -= HandleMouseClickCaptured;
        _bleDeviceManager.StatusChanged -= HandleBluetoothStatusChanged;
        _keyboardListener.Dispose();
        _mouseListener.Dispose();
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
        AppDiagnostics.WriteInfo("AppBootstrapper.ConnectBluetoothAsync", $"开始连接设备: {deviceId}");
        return ConnectBluetoothCoreAsync(deviceId, cancellationToken);
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

        _keypressWaveformId = NormalizeKeypressWaveformId(_keypressWaveformId);
        ReplaceSpecificKeyTriggers(NormalizeSpecificKeyTriggers(_specificKeyTriggers, 0, null));
        _idleWaveformId = NormalizeIdleWaveformId(_idleWaveformId);
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
        await SaveSettingsAsync(cancellationToken);
    }

    public async Task UpdateTriggerModeAsync(
        WaveformTriggerMode mode,
        string? keypressWaveformId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _triggerMode = mode;
        _keypressWaveformId = NormalizeKeypressWaveformId(keypressWaveformId);
        if (mode != WaveformTriggerMode.HoldKeypress)
        {
            CancelHoldPlayback(stopDevice: true);
        }

        if (mode == WaveformTriggerMode.SpecificKeypress)
        {
            _currentWaveformName = "未触发";
        }
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
        await SaveSettingsAsync(cancellationToken);
    }

    public async Task UpdateMouseClickTriggerAsync(
        MouseClickTriggerPayloadType payloadType,
        int fixedAStrength,
        int fixedBStrength,
        int fixedDurationMs,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _mouseClickPayloadType = payloadType;
        _mouseClickFixedAStrength = NormalizeMouseClickFixedStrength(fixedAStrength);
        _mouseClickFixedBStrength = NormalizeMouseClickFixedStrength(fixedBStrength);
        _mouseClickFixedDurationMs = NormalizeMouseClickFixedDurationMs(fixedDurationMs);
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
        await SaveSettingsAsync(cancellationToken);
    }

    public async Task AddOrUpdateSpecificKeyTriggerAsync(
        int virtualKey,
        string? waveformId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var normalizedVirtualKey = NormalizeSpecificKeyVirtualKey(virtualKey);
        if (normalizedVirtualKey <= 0)
        {
            throw new InvalidOperationException("请先指定一个按键。");
        }

        var normalizedWaveformId = NormalizeSpecificKeyWaveformId(waveformId);
        if (string.IsNullOrWhiteSpace(normalizedWaveformId))
        {
            throw new InvalidOperationException("请先选择一个波形。");
        }

        var existingIndex = _specificKeyTriggers.FindIndex(item => item.VirtualKey == normalizedVirtualKey);
        var binding = new SpecificKeyTriggerBinding
        {
            VirtualKey = normalizedVirtualKey,
            WaveformId = normalizedWaveformId
        };

        // 指定按键模式走“一个键一条映射”，同键保存时直接覆盖。
        if (existingIndex >= 0)
        {
            _specificKeyTriggers[existingIndex] = binding;
        }
        else
        {
            _specificKeyTriggers.Add(binding);
        }

        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
        await SaveSettingsAsync(cancellationToken);
    }

    public async Task DeleteSpecificKeyTriggerAsync(int virtualKey, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var normalizedVirtualKey = NormalizeSpecificKeyVirtualKey(virtualKey);
        if (normalizedVirtualKey <= 0)
        {
            return;
        }

        _specificKeyTriggers.RemoveAll(item => item.VirtualKey == normalizedVirtualKey);
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
        await SaveSettingsAsync(cancellationToken);
    }

    public async Task UpdateIdleTriggerAsync(
        bool enabled,
        int timeoutMs,
        string? waveformId,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _idleTriggerEnabled = enabled;
        _idleTriggerTimeoutMs = NormalizeIdleTriggerTimeoutMs(timeoutMs);
        _idleWaveformId = NormalizeIdleWaveformId(waveformId);
        _idleTriggerDispatched = false;
        ApplyTriggerState(CurrentSnapshot, DateTimeOffset.Now);
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
            NormalizeRuleCooldownMs(cooldownMs),
            enabled,
            true,
            true,
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
        // 触发模式要能拿到所有按键，但键速和空闲提醒只认有效输入键。
        if (e.Action == KeystrokeAction.Down && e.IsCounted)
        {
            LastKeystrokeAt = e.Timestamp;
            _idleTriggerDispatched = false;
            _typingSpeedCalculator.RecordKeystroke(e.Timestamp);
        }

        DispatchKeypressWaveformIfNeeded(e);
        PublishSnapshot(e.Timestamp);
    }

    private void HandleMouseClickCaptured(object? sender, MouseClickCapturedEventArgs e)
    {
        DispatchMouseClickWaveformIfNeeded(e);
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
        ApplyTriggerState(CurrentSnapshot, now);
        SnapshotUpdated?.Invoke(CurrentSnapshot);
    }

    private void ApplyTriggerState(TypingSpeedSnapshot snapshot, DateTimeOffset now)
    {
        if (_triggerMode == WaveformTriggerMode.AnyKeypress)
        {
            ApplyAnyKeypressModeState();
        }
        else if (_triggerMode == WaveformTriggerMode.MouseClick)
        {
            ApplyMouseClickModeState();
        }
        else if (_triggerMode == WaveformTriggerMode.HoldKeypress)
        {
            ApplyHoldKeypressModeState();
        }
        else if (_triggerMode == WaveformTriggerMode.SpecificKeypress)
        {
            ApplySpecificKeypressModeState();
        }
        else
        {
            ApplySpeedRules(snapshot, now);
        }

        ApplyIdleTriggerIfNeeded(now);
    }

    private void ApplySpeedRules(TypingSpeedSnapshot snapshot, DateTimeOffset now)
    {
        var evaluation = _waveformTriggerRouter.EvaluateSnapshot(snapshot, _speedRules, _triggerMode, now);
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

    private void DispatchMouseClickWaveformIfNeeded(MouseClickCapturedEventArgs mouseClick)
    {
        // 鼠标点击只触发输出，不计入键速，也不重置“未输入”计时。
        if (_triggerMode != WaveformTriggerMode.MouseClick)
        {
            return;
        }

        if (_mouseClickPayloadType == MouseClickTriggerPayloadType.FixedStrength)
        {
            DispatchMouseClickFixedStrength();
            return;
        }

        var evaluation = _waveformTriggerRouter.EvaluateMouseClick(_triggerMode, _keypressWaveformId);
        if (!evaluation.ShouldDispatch || string.IsNullOrWhiteSpace(evaluation.WaveformId))
        {
            return;
        }

        var waveform = ResolveWaveformById(evaluation.WaveformId);
        if (waveform is null)
        {
            return;
        }

        _currentRuleName = "鼠标点击触发";
        _currentWaveformName = waveform.Name;
        if (!_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        _ = _bleDeviceManager.PlayWaveformAsync(waveform);
    }

    private void DispatchMouseClickFixedStrength()
    {
        var waveform = BuildMouseClickFixedStrengthWaveform();

        _currentRuleName = "鼠标点击触发";
        _currentWaveformName = $"固定强度 A{_mouseClickFixedAStrength}/B{_mouseClickFixedBStrength}";
        if (!_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        _ = _bleDeviceManager.PlayWaveformAsync(waveform);
    }

    private void DispatchKeypressWaveformIfNeeded(KeystrokeCapturedEventArgs keystroke)
    {
        if (_triggerMode == WaveformTriggerMode.HoldKeypress)
        {
            HandleHoldKeypressPlayback(keystroke);
            return;
        }

        if (keystroke.Action != KeystrokeAction.Down)
        {
            return;
        }

        // 这里统一做按键触发分流，避免把“任意按键”和“指定按键”拆成两套分支。
        var evaluation = _waveformTriggerRouter.EvaluateKeystroke(
            _triggerMode,
            _keypressWaveformId,
            ResolveSpecificKeyWaveformId(keystroke.VirtualKey));
        if (!evaluation.ShouldDispatch || string.IsNullOrWhiteSpace(evaluation.WaveformId))
        {
            return;
        }

        var waveform = ResolveWaveformById(evaluation.WaveformId);
        if (waveform is null)
        {
            return;
        }

        _currentRuleName = _triggerMode == WaveformTriggerMode.SpecificKeypress ? "指定按键触发" : "按键即触发";
        _currentWaveformName = waveform.Name;
        if (!_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        _ = _bleDeviceManager.PlayWaveformAsync(waveform);
    }

    private void ApplyAnyKeypressModeState()
    {
        _currentRuleName = "按键即触发";
        _currentWaveformName = ResolveWaveformById(_keypressWaveformId)?.Name ?? "未触发";
    }

    private void ApplyMouseClickModeState()
    {
        _currentRuleName = "鼠标点击触发";
        _currentWaveformName = _mouseClickPayloadType == MouseClickTriggerPayloadType.FixedStrength
            ? $"固定强度 A{_mouseClickFixedAStrength}/B{_mouseClickFixedBStrength}"
            : ResolveWaveformById(_keypressWaveformId)?.Name ?? "未触发";
    }

    private void ApplyHoldKeypressModeState()
    {
        _currentRuleName = "按住持续触发";
        if (_holdPlaybackVirtualKeys.Count == 0 && _currentWaveformName != "已停止")
        {
            _currentWaveformName = ResolveWaveformById(_keypressWaveformId)?.Name ?? "未触发";
        }
    }

    private void ApplySpecificKeypressModeState()
    {
        _currentRuleName = "指定按键触发";
    }

    private void ApplyIdleTriggerIfNeeded(DateTimeOffset now)
    {
        if (_triggerMode == WaveformTriggerMode.HoldKeypress && _holdPlaybackVirtualKeys.Count > 0)
        {
            return;
        }

        if (!_idleTriggerEnabled || !LastKeystrokeAt.HasValue)
        {
            return;
        }

        var idleElapsed = now - LastKeystrokeAt.Value;
        if (idleElapsed < TimeSpan.FromMilliseconds(_idleTriggerTimeoutMs))
        {
            return;
        }

        var waveform = ResolveWaveformById(_idleWaveformId);
        if (waveform is null)
        {
            return;
        }

        _currentRuleName = "空闲超时触发";
        _currentWaveformName = waveform.Name;
        if (_idleTriggerDispatched || !_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        _idleTriggerDispatched = true;
        _ = _bleDeviceManager.PlayWaveformAsync(waveform);
    }

    private void HandleHoldKeypressPlayback(KeystrokeCapturedEventArgs keystroke)
    {
        if (keystroke.Action == KeystrokeAction.Up)
        {
            if (!_holdPlaybackVirtualKeys.Remove(keystroke.VirtualKey) || _holdPlaybackVirtualKeys.Count > 0)
            {
                return;
            }

            _currentWaveformName = "已停止";
            CancelHoldPlayback(stopDevice: true);
            return;
        }

        // 长按时 Windows 会重复发送 KeyDown，这里只响应首次按下。
        if (!_holdPlaybackVirtualKeys.Add(keystroke.VirtualKey) || _holdPlaybackVirtualKeys.Count > 1)
        {
            return;
        }

        var waveform = ResolveWaveformById(_keypressWaveformId);
        if (waveform is null)
        {
            return;
        }

        _currentRuleName = "按住持续触发";
        _currentWaveformName = waveform.Name;
        if (!_bleDeviceManager.CurrentStatus.IsConnected)
        {
            return;
        }

        StartHoldPlayback(waveform);
    }

    private void StartHoldPlayback(EmsWaveformDefinition waveform)
    {
        CancelHoldPlaybackLoop(stopDevice: false);

        var cts = new CancellationTokenSource();
        _holdPlaybackCts = cts;
        _ = RunHoldPlaybackLoopAsync(waveform, cts.Token);
    }

    private async Task RunHoldPlaybackLoopAsync(EmsWaveformDefinition waveform, CancellationToken cancellationToken)
    {
        var refreshDelay = ResolveHoldPlaybackDelay(waveform);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _bleDeviceManager.PlayWaveformAsync(waveform, cancellationToken, autoStop: false);
                await Task.Delay(refreshDelay, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppDiagnostics.WriteException("AppBootstrapper.RunHoldPlaybackLoopAsync", ex);
        }
    }

    private void CancelHoldPlayback(bool stopDevice)
    {
        _holdPlaybackVirtualKeys.Clear();
        CancelHoldPlaybackLoop(stopDevice);
    }

    private void CancelHoldPlaybackLoop(bool stopDevice)
    {
        if (_holdPlaybackCts is not null)
        {
            try
            {
                _holdPlaybackCts.Cancel();
            }
            catch
            {
            }

            _holdPlaybackCts.Dispose();
            _holdPlaybackCts = null;
        }

        if (stopDevice && _bleDeviceManager.CurrentStatus.IsConnected)
        {
            _ = _bleDeviceManager.StopAsync();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private async Task<bool> ConnectBluetoothCoreAsync(string deviceId, CancellationToken cancellationToken)
    {
        var connected = await _bleDeviceManager.ConnectAsync(deviceId, cancellationToken);
        AppDiagnostics.WriteInfo(
            "AppBootstrapper.ConnectBluetoothAsync",
            $"连接结束: deviceId={deviceId}, connected={connected}, lastError={_bleDeviceManager.CurrentStatus.LastError}");
        return connected;
    }

    private async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        await _settingsStore.SaveAsync(new AppSettings
        {
            TriggerMode = _triggerMode,
            KeypressWaveformId = NormalizeKeypressWaveformId(_keypressWaveformId),
            MouseClickPayloadType = _mouseClickPayloadType,
            MouseClickFixedAStrength = NormalizeMouseClickFixedStrength(_mouseClickFixedAStrength),
            MouseClickFixedBStrength = NormalizeMouseClickFixedStrength(_mouseClickFixedBStrength),
            MouseClickFixedDurationMs = NormalizeMouseClickFixedDurationMs(_mouseClickFixedDurationMs),
            SpecificKeyVirtualKey = 0,
            SpecificKeyWaveformId = string.Empty,
            SpecificKeyTriggers = _specificKeyTriggers.ToList(),
            IdleTriggerEnabled = _idleTriggerEnabled,
            IdleTriggerTimeoutMs = NormalizeIdleTriggerTimeoutMs(_idleTriggerTimeoutMs),
            IdleWaveformId = NormalizeIdleWaveformId(_idleWaveformId),
            SpeedRules = _speedRules.ToList(),
            Waveforms = _waveforms.ToList()
        }, cancellationToken);
    }

    private EmsWaveformDefinition? ResolveWaveformById(string? waveformId)
    {
        var waveform = _waveforms.FirstOrDefault(item => string.Equals(item.Id, waveformId, StringComparison.OrdinalIgnoreCase));
        return waveform ?? _waveforms.FirstOrDefault();
    }

    private EmsWaveformDefinition BuildMouseClickFixedStrengthWaveform()
    {
        return new EmsWaveformDefinition
        {
            Id = "mouse-click-fixed-strength",
            Name = "鼠标点击固定强度",
            Steps =
            [
                new EmsWaveformStep
                {
                    DurationMs = _mouseClickFixedDurationMs,
                    AStrength = _mouseClickFixedAStrength,
                    BStrength = _mouseClickFixedBStrength
                }
            ]
        };
    }

    private static TimeSpan ResolveHoldPlaybackDelay(EmsWaveformDefinition waveform)
    {
        var totalStepDurationMs = waveform.Steps.Sum(static step => Math.Max(1, step.DurationMs));
        var totalDurationMs = Math.Max(1, waveform.LoopCount) * totalStepDurationMs;

        // 续发略早于波形结束，减少蓝牙和设备处理延迟造成的断感。
        var refreshMs = Math.Max(50, (int)Math.Round(totalDurationMs * 0.85, MidpointRounding.AwayFromZero));
        return TimeSpan.FromMilliseconds(refreshMs);
    }

    private string NormalizeKeypressWaveformId(string? waveformId)
    {
        return ResolveWaveformById(waveformId)?.Id ?? string.Empty;
    }

    private static int NormalizeMouseClickFixedStrength(int strength)
    {
        return EmsWaveformStep.ClampStrength(strength);
    }

    private static int NormalizeMouseClickFixedDurationMs(int durationMs)
    {
        return durationMs > 0 ? durationMs : AppSettings.DefaultMouseClickFixedDurationMs;
    }

    private string NormalizeSpecificKeyWaveformId(string? waveformId)
    {
        return ResolveWaveformById(waveformId)?.Id ?? string.Empty;
    }

    private string? ResolveSpecificKeyWaveformId(int virtualKey)
    {
        return _specificKeyTriggers
            .FirstOrDefault(item => item.VirtualKey == virtualKey)?
            .WaveformId;
    }

    private static int NormalizeSpecificKeyVirtualKey(int virtualKey)
    {
        return virtualKey > 0 ? virtualKey : 0;
    }

    private List<SpecificKeyTriggerBinding> NormalizeSpecificKeyTriggers(
        IReadOnlyList<SpecificKeyTriggerBinding>? specificKeyTriggers,
        int legacyVirtualKey,
        string? legacyWaveformId)
    {
        var normalized = new List<SpecificKeyTriggerBinding>();

        // 先吃新结构；如果用户还留着旧配置，再自动迁进来，避免升级后丢设置。
        if (specificKeyTriggers is not null)
        {
            foreach (var trigger in specificKeyTriggers)
            {
                AddOrReplaceSpecificKeyTrigger(normalized, trigger.VirtualKey, trigger.WaveformId);
            }
        }

        if (normalized.Count == 0 && legacyVirtualKey > 0)
        {
            AddOrReplaceSpecificKeyTrigger(normalized, legacyVirtualKey, legacyWaveformId);
        }

        return normalized;
    }

    private void ReplaceSpecificKeyTriggers(List<SpecificKeyTriggerBinding> normalizedTriggers)
    {
        _specificKeyTriggers.Clear();
        _specificKeyTriggers.AddRange(normalizedTriggers);
    }

    private void AddOrReplaceSpecificKeyTrigger(List<SpecificKeyTriggerBinding> bindings, int virtualKey, string? waveformId)
    {
        var normalizedVirtualKey = NormalizeSpecificKeyVirtualKey(virtualKey);
        var normalizedWaveformId = NormalizeSpecificKeyWaveformId(waveformId);
        if (normalizedVirtualKey <= 0 || string.IsNullOrWhiteSpace(normalizedWaveformId))
        {
            return;
        }

        var existingIndex = bindings.FindIndex(item => item.VirtualKey == normalizedVirtualKey);
        var binding = new SpecificKeyTriggerBinding
        {
            VirtualKey = normalizedVirtualKey,
            WaveformId = normalizedWaveformId
        };

        if (existingIndex >= 0)
        {
            bindings[existingIndex] = binding;
        }
        else
        {
            bindings.Add(binding);
        }
    }

    private string NormalizeIdleWaveformId(string? waveformId)
    {
        var resolvedWaveformId = string.Equals(waveformId, "heartbeat", StringComparison.OrdinalIgnoreCase)
            ? AppSettings.DefaultIdleReminderWaveformId
            : waveformId;

        return ResolveWaveformById(resolvedWaveformId ?? AppSettings.DefaultIdleReminderWaveformId)?.Id ?? string.Empty;
    }

    private static int NormalizeIdleTriggerTimeoutMs(int timeoutMs)
    {
        return timeoutMs > 0 ? timeoutMs : AppSettings.DefaultIdleTriggerTimeoutMs;
    }

    private static SpeedRangeRule NormalizeRuntimeRuleBehavior(SpeedRangeRule rule)
    {
        return rule with
        {
            CooldownMs = NormalizeRuleCooldownMs(rule.CooldownMs),
            TriggerOnEnter = true,
            RepeatWithinRange = true
        };
    }

    private static int NormalizeRuleCooldownMs(int cooldownMs)
    {
        return cooldownMs > 0
            ? Math.Min(cooldownMs, AppSettings.DefaultRuleRepeatCooldownMs)
            : AppSettings.DefaultRuleRepeatCooldownMs;
    }

    private static string GetSettingsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "KeyboardSpeed-YOKONEX", "app-settings.json");
    }

    private static List<EmsWaveformDefinition> MergeWaveformsWithBuiltins(IReadOnlyList<EmsWaveformDefinition> savedWaveforms)
    {
        var mergedWaveforms = savedWaveforms.Count == 0
            ? []
            : savedWaveforms.ToList();

        var existingIds = new HashSet<string>(
            mergedWaveforms.Select(item => item.Id),
            StringComparer.OrdinalIgnoreCase);

        foreach (var builtinWaveform in BuiltinWaveforms.CreateDefaults())
        {
            if (existingIds.Add(builtinWaveform.Id))
            {
                mergedWaveforms.Add(builtinWaveform);
            }
        }

        return mergedWaveforms;
    }

    private static TypingSpeedCalculator CreateDefaultTypingSpeedCalculator()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 TypingSpeedCalculator。");
        return new TypingSpeedCalculator(new TypingSpeedOptions());
    }

    private static IGlobalKeyboardListener CreateDefaultKeyboardListener()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 GlobalKeyboardListener。");
        return new GlobalKeyboardListener();
    }

    private static IGlobalMouseListener CreateDefaultMouseListener()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 GlobalMouseListener。");
        return new GlobalMouseListener();
    }

    private static BleDeviceManager CreateDefaultBleDeviceManager()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 BleDeviceManager。");
        return new BleDeviceManager();
    }

    private static SpeedRuleCoordinator CreateDefaultSpeedRuleCoordinator()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 SpeedRuleCoordinator。");
        return new SpeedRuleCoordinator(new SpeedRuleEngine());
    }

    private static SettingsStore CreateDefaultSettingsStore()
    {
        AppDiagnostics.WriteInfo("AppBootstrapper.CreateDefaults", "创建 SettingsStore。");
        return new SettingsStore(GetSettingsFilePath());
    }
}
