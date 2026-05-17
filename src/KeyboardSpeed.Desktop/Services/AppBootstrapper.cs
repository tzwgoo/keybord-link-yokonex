using System.Windows.Threading;
using KeyboardSpeed.Bluetooth.Windows.Runtime;
using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Input.Windows;

namespace KeyboardSpeed.Desktop.Services;

public sealed class AppBootstrapper : IDisposable
{
    private readonly TypingSpeedCalculator _typingSpeedCalculator;
    private readonly IGlobalKeyboardListener _keyboardListener;
    private readonly BleDeviceManager _bleDeviceManager;
    private readonly DispatcherTimer _snapshotTimer;
    private bool _disposed;

    public AppBootstrapper()
        : this(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new GlobalKeyboardListener(),
            new BleDeviceManager())
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener,
        BleDeviceManager bleDeviceManager)
    {
        _typingSpeedCalculator = typingSpeedCalculator ?? throw new ArgumentNullException(nameof(typingSpeedCalculator));
        _keyboardListener = keyboardListener ?? throw new ArgumentNullException(nameof(keyboardListener));
        _bleDeviceManager = bleDeviceManager ?? throw new ArgumentNullException(nameof(bleDeviceManager));
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
        SnapshotUpdated?.Invoke(CurrentSnapshot);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
