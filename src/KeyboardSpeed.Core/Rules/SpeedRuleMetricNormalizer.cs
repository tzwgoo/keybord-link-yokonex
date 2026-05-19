namespace KeyboardSpeed.Core.Rules;

public static class SpeedRuleMetricNormalizer
{
    public static IReadOnlyList<SpeedRangeRule> NormalizeToCharactersPerMinute(IEnumerable<SpeedRangeRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        return rules.Select(NormalizeToCharactersPerMinute).ToList();
    }

    public static SpeedRangeRule NormalizeToCharactersPerMinute(SpeedRangeRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        if (rule.MetricType != SpeedMetricType.Wpm)
        {
            return rule;
        }

        return rule with
        {
            MetricType = SpeedMetricType.Kpm,
            MinValue = rule.MinValue * 5d,
            MaxValue = rule.MaxValue * 5d
        };
    }
}
