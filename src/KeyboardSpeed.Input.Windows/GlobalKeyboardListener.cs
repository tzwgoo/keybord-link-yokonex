namespace KeyboardSpeed.Input.Windows;

public sealed class GlobalKeyboardListener : IGlobalKeyboardListener
{
    private readonly GlobalKeyboardHook _globalKeyboardHook;
    private bool _disposed;

    public GlobalKeyboardListener()
        : this(new GlobalKeyboardHook())
    {
    }

    internal GlobalKeyboardListener(GlobalKeyboardHook globalKeyboardHook)
    {
        _globalKeyboardHook = globalKeyboardHook ?? throw new ArgumentNullException(nameof(globalKeyboardHook));
        _globalKeyboardHook.KeyDown += HandleKeyDown;
    }

    public event EventHandler<KeystrokeCapturedEventArgs>? KeystrokeCaptured;

    public void Start()
    {
        ThrowIfDisposed();
        _globalKeyboardHook.Install();
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _globalKeyboardHook.Uninstall();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _globalKeyboardHook.KeyDown -= HandleKeyDown;
        _globalKeyboardHook.Dispose();
        _disposed = true;
    }

    private void HandleKeyDown(object? sender, KeystrokeCapturedEventArgs e)
    {
        KeystrokeCaptured?.Invoke(this, e);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
