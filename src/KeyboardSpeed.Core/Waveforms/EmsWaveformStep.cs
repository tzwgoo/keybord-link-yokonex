namespace KeyboardSpeed.Core.Waveforms;

public sealed record EmsWaveformStep
{
    public int DurationMs { get; init; } = 100;

    public int AStrength { get; init; }

    public int AMode { get; init; } = 1;

    public int BStrength { get; init; }

    public int BMode { get; init; } = 1;

    public int MotorState { get; init; }
}
