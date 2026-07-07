using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyboardSpeed.Input.Windows.Interop;

namespace KeyboardSpeed.Input.Windows;

public sealed class GlobalMouseHook : IDisposable
{
    private readonly NativeMethods.LowLevelMouseProc _hookCallback;
    private nint _hookHandle;
    private bool _disposed;

    public GlobalMouseHook()
    {
        _hookCallback = HandleHookCallback;
    }

    public event EventHandler<MouseClickCapturedEventArgs>? MouseClicked;

    public bool IsInstalled => _hookHandle != nint.Zero;

    public void Install()
    {
        ThrowIfDisposed();
        if (IsInstalled)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = NativeMethods.GetModuleHandleW(module?.ModuleName);
        _hookHandle = NativeMethods.SetWindowsMouseHookExW(
            NativeMethods.WhMouseLl,
            _hookCallback,
            moduleHandle,
            0);

        if (_hookHandle == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "安装全局鼠标钩子失败。");
        }
    }

    public void Uninstall()
    {
        if (!IsInstalled)
        {
            return;
        }

        var handle = _hookHandle;
        _hookHandle = nint.Zero;
        if (!NativeMethods.UnhookWindowsHookEx(handle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "卸载全局鼠标钩子失败。");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsInstalled)
        {
            Uninstall();
        }

        _disposed = true;
    }

    private nint HandleHookCallback(int nCode, nuint wParam, nint lParam)
    {
        if (nCode >= 0 && TryResolveButton(wParam, out var button))
        {
            var hookData = Marshal.PtrToStructure<NativeMethods.MouseLlHookStruct>(lParam);
            MouseClicked?.Invoke(
                this,
                new MouseClickCapturedEventArgs(
                    DateTimeOffset.UtcNow,
                    button,
                    hookData.Pt.X,
                    hookData.Pt.Y));
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool TryResolveButton(nuint message, out MouseClickButton button)
    {
        if (message == NativeMethods.WmLButtonDown)
        {
            button = MouseClickButton.Left;
            return true;
        }

        if (message == NativeMethods.WmRButtonDown)
        {
            button = MouseClickButton.Right;
            return true;
        }

        if (message == NativeMethods.WmMButtonDown)
        {
            button = MouseClickButton.Middle;
            return true;
        }

        button = MouseClickButton.Left;
        return false;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
