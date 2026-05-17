namespace KeyboardSpeed.Core.Typing;

public sealed record TypingSpeedSnapshot(
    double RealtimeKpm,
    double RealtimeWpm,
    double TrendKpm,
    double TrendWpm,
    int ActiveSampleCount);
