namespace KeyboardSpeed.Input.Windows;

public interface IGlobalMouseListener : IDisposable
{
    event EventHandler<MouseClickCapturedEventArgs>? MouseClickCaptured;

    void Start();

    void Stop();
}
