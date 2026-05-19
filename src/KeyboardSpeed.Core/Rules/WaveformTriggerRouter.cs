using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Core.Rules;

public sealed class WaveformTriggerRouter
{
    private readonly SpeedRuleCoordinator _speedRuleCoordinator;

    public WaveformTriggerRouter(SpeedRuleCoordinator speedRuleCoordinator)
    {
        _speedRuleCoordinator = speedRuleCoordinator ?? throw new ArgumentNullException(nameof(speedRuleCoordinator));
    }

    public RuleEvaluationResult EvaluateSnapshot(
        TypingSpeedSnapshot snapshot,
        IReadOnlyList<SpeedRangeRule> rules,
        WaveformTriggerMode mode,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rules);

        if (mode == WaveformTriggerMode.AnyKeypress)
        {
            return new RuleEvaluationResult(null, false, false, null);
        }

        return _speedRuleCoordinator.Evaluate(snapshot, rules, now);
    }

    public RuleEvaluationResult EvaluateKeystroke(WaveformTriggerMode mode, string? waveformId)
    {
        if (mode != WaveformTriggerMode.AnyKeypress || string.IsNullOrWhiteSpace(waveformId))
        {
            return new RuleEvaluationResult(null, false, false, null);
        }

        return new RuleEvaluationResult(null, true, false, waveformId);
    }
}
