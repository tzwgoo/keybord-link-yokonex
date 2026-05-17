namespace KeyboardSpeed.Bluetooth.Windows.Protocol;

public static class EmsPacketChecksum
{
    public static byte Compute(IEnumerable<byte> bytes)
    {
        var sum = 0;
        foreach (var value in bytes)
        {
            sum = (sum + value) & 0xFF;
        }

        return (byte)sum;
    }
}
