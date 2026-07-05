namespace KeyboardSpeed.Input.Windows;

public sealed class KeystrokeCapturedEventArgs : EventArgs
{
    public KeystrokeCapturedEventArgs(
        DateTimeOffset timestamp,
        int virtualKey,
        bool isCounted = true,
        KeystrokeAction action = KeystrokeAction.Down)
    {
        Timestamp = timestamp;
        VirtualKey = virtualKey;
        IsCounted = isCounted;
        Action = action;
    }

    public DateTimeOffset Timestamp { get; }

    public int VirtualKey { get; }

    public bool IsCounted { get; }

    public KeystrokeAction Action { get; }
}
