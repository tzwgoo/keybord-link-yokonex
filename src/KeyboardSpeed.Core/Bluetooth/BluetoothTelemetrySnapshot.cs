namespace KeyboardSpeed.Core.Bluetooth;

public sealed record BluetoothTelemetrySnapshot
{
    public static BluetoothTelemetrySnapshot Empty { get; } = new();

    public IReadOnlyList<BluetoothTelemetrySample> Samples { get; init; } = Array.Empty<BluetoothTelemetrySample>();
}
