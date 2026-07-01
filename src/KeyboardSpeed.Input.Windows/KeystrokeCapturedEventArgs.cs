namespace KeyboardSpeed.Input.Windows;

public sealed class KeystrokeCapturedEventArgs : EventArgs
{
    public KeystrokeCapturedEventArgs(DateTimeOffset timestamp, int virtualKey, bool isCounted = true)
    {
        Timestamp = timestamp;
        VirtualKey = virtualKey;
        IsCounted = isCounted;
    }

    public DateTimeOffset Timestamp { get; }

    public int VirtualKey { get; }

    public bool IsCounted { get; }
}
