namespace KeyboardSpeed.Input.Windows;

public static class KeyboardInputClassifier
{
    public static bool ShouldCount(int virtualKey)
    {
        return virtualKey switch
        {
            >= 0x30 and <= 0x39 => true,
            >= 0x41 and <= 0x5A => true,
            >= 0x60 and <= 0x69 => true,
            >= 0xBA and <= 0xC0 => true,
            >= 0xDB and <= 0xDE => true,
            0x08 or 0x0D or 0x20 or 0x6A or 0x6B or 0x6D or 0x6E or 0x6F => true,
            _ => false
        };
    }
}
