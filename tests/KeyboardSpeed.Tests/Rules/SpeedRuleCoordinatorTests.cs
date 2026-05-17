using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Tests.Rules;

public sealed class SpeedRuleCoordinatorTests
{
    [Fact]
    public void Coordinator_ShouldNotRetriggerWithinCooldown()
    {
        var coordinator = new SpeedRuleCoordinator(new SpeedRuleEngine());
        var rule = new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, true, true);
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");

        var first = coordinator.Evaluate(snapshot, [rule], now);
        var second = coordinator.Evaluate(snapshot, [rule], now.AddMilliseconds(300));

        Assert.True(first.ShouldDispatch);
        Assert.False(second.ShouldDispatch);
    }

    [Fact]
    public void Coordinator_ShouldReturnStopActionWhenLeavingMatchedRange()
    {
        var coordinator = new SpeedRuleCoordinator(new SpeedRuleEngine());
        var rule = new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true);
        var matchedSnapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        var lowSnapshot = new TypingSpeedSnapshot(40, 8, 50, 10, 1);
        var now = DateTimeOffset.Parse("2026-05-18T10:00:10+08:00");

        var first = coordinator.Evaluate(matchedSnapshot, [rule], now);
        var second = coordinator.Evaluate(lowSnapshot, [rule], now.AddSeconds(2));

        Assert.True(first.ShouldDispatch);
        Assert.True(second.ShouldStop);
    }
}
