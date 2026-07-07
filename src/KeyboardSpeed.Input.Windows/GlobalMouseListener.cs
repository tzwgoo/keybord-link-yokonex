namespace KeyboardSpeed.Input.Windows;

public sealed class GlobalMouseListener : IGlobalMouseListener
{
    private readonly GlobalMouseHook _globalMouseHook;
    private bool _disposed;

    public GlobalMouseListener()
        : this(new GlobalMouseHook())
    {
    }

    internal GlobalMouseListener(GlobalMouseHook globalMouseHook)
    {
        _globalMouseHook = globalMouseHook ?? throw new ArgumentNullException(nameof(globalMouseHook));
        _globalMouseHook.MouseClicked += HandleMouseClicked;
    }

    public event EventHandler<MouseClickCapturedEventArgs>? MouseClickCaptured;

    public void Start()
    {
        ThrowIfDisposed();
        _globalMouseHook.Install();
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _globalMouseHook.Uninstall();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _globalMouseHook.MouseClicked -= HandleMouseClicked;
        _globalMouseHook.Dispose();
        _disposed = true;
    }

    private void HandleMouseClicked(object? sender, MouseClickCapturedEventArgs e)
    {
        MouseClickCaptured?.Invoke(this, e);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
