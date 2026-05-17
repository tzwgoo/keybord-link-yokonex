using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Tests.Typing;

public sealed class TypingSpeedCalculatorTests
{
    [Fact]
    public void Calculator_ShouldUseRecentTenSecondWindowForRealtimeKpm()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
        var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");

        calculator.RecordKeystroke(now.AddSeconds(-9));
        calculator.RecordKeystroke(now.AddSeconds(-4));

        var snapshot = calculator.CreateSnapshot(now);

        Assert.Equal(12d, snapshot.RealtimeKpm, 6);
        Assert.Equal(2.4d, snapshot.RealtimeWpm, 6);
    }

    [Fact]
    public void Calculator_ShouldDropExpiredSamplesFromRealtimeWindow()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
        var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");

        calculator.RecordKeystroke(now.AddSeconds(-15));
        calculator.RecordKeystroke(now.AddSeconds(-3));

        var snapshot = calculator.CreateSnapshot(now);

        Assert.Equal(6d, snapshot.RealtimeKpm, 6);
        Assert.Equal(4d, snapshot.TrendKpm, 6);
    }

    [Fact]
    public void Calculator_ShouldReturnZeroWhenThereAreNoSamples()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());

        var snapshot = calculator.CreateSnapshot(DateTimeOffset.Parse("2026-05-18T10:00:10+08:00"));

        Assert.Equal(0d, snapshot.RealtimeKpm);
        Assert.Equal(0d, snapshot.RealtimeWpm);
        Assert.Equal(0d, snapshot.TrendKpm);
        Assert.Equal(0d, snapshot.TrendWpm);
    }
}
