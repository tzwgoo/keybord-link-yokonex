namespace KeyboardSpeed.Core.Bluetooth;

public sealed record BluetoothConnectionStatus
{
    public bool IsConnected { get; init; }

    public bool IsBusy { get; init; }

    public int? BatteryLevel { get; init; }

    public string LastError { get; init; } = string.Empty;

    public BluetoothDeviceDescriptor? Device { get; init; }
}
