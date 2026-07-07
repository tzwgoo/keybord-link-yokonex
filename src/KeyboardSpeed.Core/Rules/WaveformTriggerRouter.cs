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

        if (mode != WaveformTriggerMode.SpeedRules)
        {
            return new RuleEvaluationResult(null, false, false, null);
        }

        return _speedRuleCoordinator.Evaluate(snapshot, rules, now);
    }

    public RuleEvaluationResult EvaluateKeystroke(
        WaveformTriggerMode mode,
        string? keypressWaveformId,
        string? specificKeyWaveformId)
    {
        if (mode == WaveformTriggerMode.AnyKeypress && !string.IsNullOrWhiteSpace(keypressWaveformId))
        {
            return new RuleEvaluationResult(null, true, false, keypressWaveformId);
        }

        if (mode == WaveformTriggerMode.SpecificKeypress &&
            !string.IsNullOrWhiteSpace(specificKeyWaveformId))
        {
            return new RuleEvaluationResult(null, true, false, specificKeyWaveformId);
        }

        if (mode != WaveformTriggerMode.AnyKeypress && mode != WaveformTriggerMode.SpecificKeypress)
        {
            return new RuleEvaluationResult(null, false, false, null);
        }

        return new RuleEvaluationResult(null, false, false, null);
    }

    public RuleEvaluationResult EvaluateMouseClick(
        WaveformTriggerMode mode,
        string? mouseClickWaveformId)
    {
        if (mode == WaveformTriggerMode.MouseClick && !string.IsNullOrWhiteSpace(mouseClickWaveformId))
        {
            return new RuleEvaluationResult(null, true, false, mouseClickWaveformId);
        }

        return new RuleEvaluationResult(null, false, false, null);
    }
}
