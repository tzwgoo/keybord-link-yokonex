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
        if (snapshot.ActiveSampleCount <= 0)
        {
            return false;
        }

        var normalizedRule = SpeedRuleMetricNormalizer.NormalizeToCharactersPerMinute(rule);
        var value = snapshot.RealtimeKpm;

        return value >= normalizedRule.MinValue && value <= normalizedRule.MaxValue;
    }
}
