using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Core.Rules;

public sealed class SpeedRuleEngine
{
    public RuleMatchResult Match(TypingSpeedSnapshot snapshot, IEnumerable<SpeedRangeRule> rules)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rules);

        var matchedRule = rules
            .Where(rule => rule.Enabled)
            .FirstOrDefault(rule => IsInRange(snapshot, rule));

        return new RuleMatchResult(matchedRule);
    }

    private static bool IsInRange(TypingSpeedSnapshot snapshot, SpeedRangeRule rule)
    {
        var value = rule.MetricType == SpeedMetricType.Wpm
            ? snapshot.RealtimeWpm
            : snapshot.RealtimeKpm;

        return value >= rule.MinValue && value <= rule.MaxValue;
    }
}
