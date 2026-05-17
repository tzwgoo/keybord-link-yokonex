namespace KeyboardSpeed.Core.Typing;

public sealed record TypingSpeedOptions
{
    public TimeSpan RealtimeWindow { get; init; } = TimeSpan.FromSeconds(10);

    public TimeSpan TrendWindow { get; init; } = TimeSpan.FromSeconds(30);
}
