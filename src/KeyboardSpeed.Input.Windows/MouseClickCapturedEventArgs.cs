namespace KeyboardSpeed.Input.Windows;

public sealed class MouseClickCapturedEventArgs : EventArgs
{
    public MouseClickCapturedEventArgs(
        DateTimeOffset timestamp,
        MouseClickButton button,
        int x,
        int y)
    {
        Timestamp = timestamp;
        Button = button;
        X = x;
        Y = y;
    }

    public DateTimeOffset Timestamp { get; }

    public MouseClickButton Button { get; }

    public int X { get; }

    public int Y { get; }
}
