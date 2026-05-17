using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public sealed class BluetoothNotificationParser
{
    public bool TryReadBatteryLevel(BluetoothDeviceType deviceType, byte[] packet, out int? batteryLevel)
    {
        batteryLevel = null;
        if (packet.Length < 4 || packet[0] != 0x35)
        {
            return false;
        }

        switch (deviceType)
        {
            case BluetoothDeviceType.Toy when packet.Length >= 5 && packet[1] == 0x13 && packet[2] == 0x01:
                batteryLevel = packet[3];
                return true;
            case BluetoothDeviceType.Ems when packet.Length >= 5 && packet[1] == 0x71 && packet[2] == 0x04:
                batteryLevel = packet[3];
                return true;
            default:
                return false;
        }
    }

    public BluetoothParsedStatus? ParseStatus(BluetoothDeviceType deviceType, byte[] packet)
    {
        return TryReadBatteryLevel(deviceType, packet, out var batteryLevel)
            ? new BluetoothParsedStatus { BatteryLevel = batteryLevel }
            : null;
    }
}
