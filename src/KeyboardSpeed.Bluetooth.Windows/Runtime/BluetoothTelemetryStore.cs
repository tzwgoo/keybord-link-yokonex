using KeyboardSpeed.Core.Bluetooth;

namespace KeyboardSpeed.Bluetooth.Windows.Runtime;

public sealed class BluetoothTelemetryStore
{
    private readonly TimeSpan _window;
    private readonly List<BluetoothTelemetrySample> _samples = [];

    public BluetoothTelemetryStore(TimeSpan? window = null)
    {
        _window = window ?? TimeSpan.FromSeconds(15);
    }

    public void RecordStatus(BluetoothConnectionStatus status, DateTimeOffset? timestampUtc = null)
    {
        if (!status.IsConnected || status.Device is null)
        {
            _samples.Clear();
            return;
        }

        var now = timestampUtc ?? DateTimeOffset.UtcNow;
        _samples.Add(new BluetoothTelemetrySample(
            now,
            status.BatteryLevel,
            status.ChannelAStrength,
            status.ChannelBStrength,
            status.ChannelAEnabled,
            status.ChannelBEnabled));

        Trim(now);
    }

    public BluetoothTelemetrySnapshot GetSnapshot(DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        Trim(now);

        return new BluetoothTelemetrySnapshot
        {
            Samples = _samples.OrderBy(static sample => sample.TimestampUtc).ToArray()
        };
    }

    private void Trim(DateTimeOffset now)
    {
        var threshold = now - _window;
        _samples.RemoveAll(sample => sample.TimestampUtc < threshold);
    }
}
