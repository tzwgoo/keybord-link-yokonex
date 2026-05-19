using KeyboardSpeed.Core.Rules;

namespace KeyboardSpeed.Tests.Rules;

public sealed class SpeedRuleMetricNormalizerTests
{
    [Fact]
    public void NormalizeToCharactersPerMinute_ShouldConvertWpmThresholds()
    {
        var rules = new[]
        {
            new SpeedRangeRule("wpm", "旧规则", SpeedMetricType.Wpm, 20, 40, "heartbeat", 1500, true, true, false, true)
        };

        var normalized = SpeedRuleMetricNormalizer.NormalizeToCharactersPerMinute(rules);

        var rule = Assert.Single(normalized);
        Assert.Equal(SpeedMetricType.Kpm, rule.MetricType);
        Assert.Equal(100d, rule.MinValue);
        Assert.Equal(200d, rule.MaxValue);
    }

    [Fact]
    public void NormalizeToCharactersPerMinute_ShouldKeepKpmThresholdsUnchanged()
    {
        var rules = new[]
        {
            new SpeedRangeRule("kpm", "新规则", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true)
        };

        var normalized = SpeedRuleMetricNormalizer.NormalizeToCharactersPerMinute(rules);

        var rule = Assert.Single(normalized);
        Assert.Equal(SpeedMetricType.Kpm, rule.MetricType);
        Assert.Equal(120d, rule.MinValue);
        Assert.Equal(220d, rule.MaxValue);
    }
}
