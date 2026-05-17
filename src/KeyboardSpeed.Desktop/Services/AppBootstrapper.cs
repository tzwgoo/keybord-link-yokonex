using System.Windows.Threading;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Input.Windows;

namespace KeyboardSpeed.Desktop.Services;

public sealed class AppBootstrapper : IDisposable
{
    private readonly TypingSpeedCalculator _typingSpeedCalculator;
    private readonly IGlobalKeyboardListener _keyboardListener;
    private readonly DispatcherTimer _snapshotTimer;
    private bool _disposed;

    public AppBootstrapper()
        : this(
            new TypingSpeedCalculator(new TypingSpeedOptions()),
            new GlobalKeyboardListener())
    {
    }

    public AppBootstrapper(
        TypingSpeedCalculator typingSpeedCalculator,
        IGlobalKeyboardListener keyboardListener)
    {
        _typingSpeedCalculator = typingSpeedCalculator ?? throw new ArgumentNullException(nameof(typingSpeedCalculator));
        _keyboardListener = keyboardListener ?? throw new ArgumentNullException(nameof(keyboardListener));
        _keyboardListener.KeystrokeCaptured += HandleKeystrokeCaptured;

        _snapshotTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _snapshotTimer.Tick += HandleSnapshotTimerTick;
        CurrentSnapshot = _typingSpeedCalculator.CreateSnapshot(DateTimeOffset.Now);
    }

    public event Action<TypingSpeedSnapshot>? SnapshotUpdated;

    public TypingSpeedSnapshot CurrentSnapshot { get; private set; }

    public DateTimeOffset? LastKeystrokeAt { get; private set; }

    public bool IsListening { get; private set; }

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
        _keyboardListener.Dispose();
        _snapshotTimer.Tick -= HandleSnapshotTimerTick;
        _disposed = true;
    }

    private void HandleKeystrokeCaptured(object? sender, KeystrokeCapturedEventArgs e)
    {
        LastKeystrokeAt = e.Timestamp;
        _typingSpeedCalculator.RecordKeystroke(e.Timestamp);
        PublishSnapshot(e.Timestamp);
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
