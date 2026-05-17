namespace KeyboardSpeed.Input.Windows;

public interface IGlobalKeyboardListener : IDisposable
{
    event EventHandler<KeystrokeCapturedEventArgs>? KeystrokeCaptured;

    void Start();

    void Stop();
}
