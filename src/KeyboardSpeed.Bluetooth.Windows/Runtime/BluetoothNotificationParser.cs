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
        if (packet.Length < 4 || packet[0] != 0x35)
        {
            return null;
        }

        if (deviceType == BluetoothDeviceType.Ems && packet[1] == 0x71)
        {
            return packet[2] switch
            {
                0x01 when packet.Length >= 9 => new BluetoothParsedStatus
                {
                    ChannelAElectrodeStatus = packet[3],
                    ChannelAEnabled = packet[4] == 0x01,
                    ChannelAStrength = (packet[5] << 8) | packet[6],
                    ChannelAMode = packet[7]
                },
                0x02 when packet.Length >= 9 => new BluetoothParsedStatus
                {
                    ChannelBElectrodeStatus = packet[3],
                    ChannelBEnabled = packet[4] == 0x01,
                    ChannelBStrength = (packet[5] << 8) | packet[6],
                    ChannelBMode = packet[7]
                },
                0x03 when packet.Length >= 5 => new BluetoothParsedStatus
                {
                    MotorState = packet[3]
                },
                0x04 when packet.Length >= 5 => new BluetoothParsedStatus
                {
                    BatteryLevel = packet[3]
                },
                0x05 when packet.Length >= 6 => new BluetoothParsedStatus
                {
                    StepCount = (packet[3] << 8) | packet[4]
                },
                0x55 when packet.Length >= 5 => new BluetoothParsedStatus
                {
                    ErrorCode = packet[3]
                },
                _ => null
            };
        }

        if (deviceType == BluetoothDeviceType.Toy && packet[1] == 0x13 && packet.Length >= 5)
        {
            return packet[2] == 0x01
                ? new BluetoothParsedStatus
                {
                    BatteryLevel = packet[3]
                }
                : null;
        }

        return null;
    }
}
