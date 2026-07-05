using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyboardSpeed.Input.Windows.Interop;

namespace KeyboardSpeed.Input.Windows;

public sealed class GlobalKeyboardHook : IDisposable
{
    private readonly NativeMethods.LowLevelKeyboardProc _hookCallback;
    private nint _hookHandle;
    private bool _disposed;

    public GlobalKeyboardHook()
    {
        _hookCallback = HandleHookCallback;
    }

    public event EventHandler<KeystrokeCapturedEventArgs>? KeyChanged;

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
        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WhKeyboardLl,
            _hookCallback,
            moduleHandle,
            0);

        if (_hookHandle == nint.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "安装全局键盘钩子失败。");
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
            throw new Win32Exception(Marshal.GetLastWin32Error(), "卸载全局键盘钩子失败。");
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
        if (nCode >= 0 &&
            (wParam == NativeMethods.WmKeyDown ||
             wParam == NativeMethods.WmSysKeyDown ||
             wParam == NativeMethods.WmKeyUp ||
             wParam == NativeMethods.WmSysKeyUp))
        {
            var hookData = Marshal.PtrToStructure<NativeMethods.KbdLlHookStruct>(lParam);
            var virtualKey = unchecked((int)hookData.VkCode);
            var action = wParam == NativeMethods.WmKeyUp || wParam == NativeMethods.WmSysKeyUp
                ? KeystrokeAction.Up
                : KeystrokeAction.Down;
            KeyChanged?.Invoke(
                this,
                new KeystrokeCapturedEventArgs(
                    DateTimeOffset.UtcNow,
                    virtualKey,
                    KeyboardInputClassifier.ShouldCount(virtualKey),
                    action));
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
