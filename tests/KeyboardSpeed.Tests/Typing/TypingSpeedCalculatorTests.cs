using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Tests.Typing;

public sealed class TypingSpeedCalculatorTests
{
    [Fact]
    public void Calculator_ShouldUseTotalCharactersAndElapsedMinutesForRealtimeCpm()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
        var now = DateTimeOffset.Parse("2026-05-18T10:01:00+08:00");

        calculator.RecordKeystroke(now.AddSeconds(-60));
        calculator.RecordKeystroke(now.AddSeconds(-30));
        calculator.RecordKeystroke(now.AddSeconds(-10));

        var snapshot = calculator.CreateSnapshot(now);

        Assert.Equal(3d, snapshot.RealtimeKpm, 6);
        Assert.Equal(0.6d, snapshot.RealtimeWpm, 6);
    }

    [Fact]
    public void Calculator_ShouldKeepTotalCpmButDropExpiredSamplesFromActiveWindow()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
        var now = DateTimeOffset.Parse("2026-05-18T10:01:00+08:00");

        calculator.RecordKeystroke(now.AddSeconds(-60));
        calculator.RecordKeystroke(now.AddSeconds(-3));

        var snapshot = calculator.CreateSnapshot(now);

        Assert.Equal(2d, snapshot.RealtimeKpm, 6);
        Assert.Equal(1, snapshot.ActiveSampleCount);
        Assert.Equal(2d, snapshot.TrendKpm, 6);
    }

    [Fact]
    public void Calculator_ShouldReturnZeroRealtimeCpmWhenElapsedTimeIsZero()
    {
        var calculator = new TypingSpeedCalculator(new TypingSpeedOptions());
        var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");

        calculator.RecordKeystroke(now);

        var snapshot = calculator.CreateSnapshot(now);

        Assert.Equal(0d, snapshot.RealtimeKpm);
        Assert.Equal(1, snapshot.ActiveSampleCount);
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
