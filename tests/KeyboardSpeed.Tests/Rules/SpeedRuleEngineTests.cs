using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Tests.Rules;

public sealed class SpeedRuleEngineTests
{
    [Fact]
    public void Engine_ShouldMatchMiddleRuleForCurrentKpm()
    {
        var engine = new SpeedRuleEngine();
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        var rules = new[]
        {
            new SpeedRangeRule("low", "低速", SpeedMetricType.Kpm, 0, 119.99, "soft", 1500, true, true, false, true),
            new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true),
            new SpeedRangeRule("high", "高速", SpeedMetricType.Kpm, 220.01, 999, "burst", 1500, true, true, false, true)
        };

        var result = engine.Match(snapshot, rules);

        Assert.Equal("mid", result.ActiveRule?.Id);
    }

    [Fact]
    public void Engine_ShouldIgnoreDisabledRules()
    {
        var engine = new SpeedRuleEngine();
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        var rules = new[]
        {
            new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, false, true, false, true)
        };

        var result = engine.Match(snapshot, rules);

        Assert.Null(result.ActiveRule);
    }

    [Fact]
    public void Engine_ShouldSupportLegacyWpmRulesThroughCharactersPerMinuteNormalization()
    {
        var engine = new SpeedRuleEngine();
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        var rules = new[]
        {
            new SpeedRangeRule("legacy-mid", "旧中速", SpeedMetricType.Wpm, 20, 40, "heartbeat", 1500, true, true, false, true)
        };

        var result = engine.Match(snapshot, rules);

        Assert.Equal("legacy-mid", result.ActiveRule?.Id);
    }

    [Fact]
    public void Engine_ShouldNotMatchAnyRuleWhenActiveSampleCountIsZero()
    {
        var engine = new SpeedRuleEngine();
        var snapshot = new TypingSpeedSnapshot(0, 0, 0, 0, 0);
        var rules = new[]
        {
            new SpeedRangeRule("low", "低速", SpeedMetricType.Kpm, 0, 119.99, "soft", 600, true, true, true, true)
        };

        var result = engine.Match(snapshot, rules);

        Assert.Null(result.ActiveRule);
    }
}
