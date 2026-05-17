namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public sealed record BluetoothParsedStatus
{
    public int? BatteryLevel { get; init; }

    public int? ChannelAElectrodeStatus { get; init; }

    public bool? ChannelAEnabled { get; init; }

    public int? ChannelAStrength { get; init; }

    public int? ChannelAMode { get; init; }

    public int? ChannelBElectrodeStatus { get; init; }

    public bool? ChannelBEnabled { get; init; }

    public int? ChannelBStrength { get; init; }

    public int? ChannelBMode { get; init; }

    public int? MotorState { get; init; }

    public int? StepCount { get; init; }

    public int? ErrorCode { get; init; }
}
