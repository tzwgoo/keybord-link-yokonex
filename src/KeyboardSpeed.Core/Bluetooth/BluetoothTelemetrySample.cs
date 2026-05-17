namespace KeyboardSpeed.Core.Bluetooth;

public sealed record BluetoothTelemetrySample(
    DateTimeOffset TimestampUtc,
    int? BatteryLevel,
    int? ChannelAStrength,
    int? ChannelBStrength,
    bool? ChannelAEnabled,
    bool? ChannelBEnabled);
