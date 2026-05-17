namespace KeyboardSpeed.Core.Waveforms;

public sealed record EmsWaveformStep
{
    public int DurationMs { get; init; } = 100;

    public int AStrength { get; init; }

    public int AMode { get; init; } = 1;

    public int AFrequency { get; init; }

    public int APulseWidth { get; init; }

    public int BStrength { get; init; }

    public int BMode { get; init; } = 1;

    public int BFrequency { get; init; }

    public int BPulseWidth { get; init; }

    public int MotorState { get; init; }
}
