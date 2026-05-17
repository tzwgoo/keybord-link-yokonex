using System.Runtime.InteropServices;

namespace KeyboardSpeed.Input.Windows.Interop;

internal static partial class NativeMethods
{
    public const int WhKeyboardLl = 13;
    public const int WmKeyDown = 0x0100;
    public const int WmSysKeyDown = 0x0104;

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial nint SetWindowsHookExW(int idHook, LowLevelKeyboardProc lpfn, nint hmod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(nint hhk);

    [LibraryImport("user32.dll")]
    public static partial nint CallNextHookEx(nint hhk, int nCode, nuint wParam, nint lParam);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint GetModuleHandleW(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    public struct KbdLlHookStruct
    {
        public uint VkCode;
        public uint ScanCode;
        public uint Flags;
        public uint Time;
        public nuint DwExtraInfo;
    }

    public delegate nint LowLevelKeyboardProc(int nCode, nuint wParam, nint lParam);
}
