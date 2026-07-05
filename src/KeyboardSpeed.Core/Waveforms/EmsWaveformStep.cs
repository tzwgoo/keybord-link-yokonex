namespace KeyboardSpeed.Core.Waveforms;

public sealed record EmsWaveformStep
{
    // YOKONEX 设备通道强度真实量程是 0-180，预览、拖拽和发包都按这个上限处理。
    public const int MaxStrength = 180;

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

    public static int ClampStrength(int strength)
    {
        return Math.Clamp(strength, 0, MaxStrength);
    }
}
