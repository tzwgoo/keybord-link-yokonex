using KeyboardSpeed.Core.Bluetooth;
using KeyboardSpeed.Core.Typing;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Desktop.Services;

public static class FloatingTelemetryPresenter
{
    public static FloatingTelemetryState BuildState(
        TypingSpeedSnapshot snapshot,
        BluetoothConnectionStatus status,
        string currentRuleName,
        EmsWaveformDefinition? waveform,
        string currentWaveformName)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(status);

        var isVisible = status.IsConnected;
        var deviceName = status.Device?.Name;

        return new FloatingTelemetryState
        {
            IsVisible = isVisible,
            DeviceName = string.IsNullOrWhiteSpace(deviceName) ? "未连接设备" : deviceName,
            ConnectionText = isVisible ? "设备已连接" : "--",
            CharactersPerMinuteText = snapshot.RealtimeKpm.ToString("0.0"),
            RuleName = string.IsNullOrWhiteSpace(currentRuleName) ? "未命中" : currentRuleName,
            WaveformName = string.IsNullOrWhiteSpace(currentWaveformName) ? "未触发" : currentWaveformName,
            ChannelAStrength = EmsWaveformStep.ClampStrength(status.ChannelAStrength ?? 0),
            ChannelBStrength = EmsWaveformStep.ClampStrength(status.ChannelBStrength ?? 0),
            Waveform = waveform
        };
    }
}

public sealed record FloatingTelemetryState
{
    public bool IsVisible { get; init; }

    public string DeviceName { get; init; } = "未连接设备";

    public string ConnectionText { get; init; } = "--";

    public string CharactersPerMinuteText { get; init; } = "0.0";

    public string RuleName { get; init; } = "未命中";

    public string WaveformName { get; init; } = "未触发";

    public int ChannelAStrength { get; init; }

    public int ChannelBStrength { get; init; }

    public EmsWaveformDefinition? Waveform { get; init; }
}
