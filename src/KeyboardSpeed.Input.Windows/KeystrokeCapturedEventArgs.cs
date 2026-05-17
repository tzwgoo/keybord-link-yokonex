namespace KeyboardSpeed.Input.Windows;

public sealed class KeystrokeCapturedEventArgs : EventArgs
{
    public KeystrokeCapturedEventArgs(DateTimeOffset timestamp, int virtualKey)
    {
        Timestamp = timestamp;
        VirtualKey = virtualKey;
    }

    public DateTimeOffset Timestamp { get; }

    public int VirtualKey { get; }
}
