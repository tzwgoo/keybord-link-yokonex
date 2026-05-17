using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Core.Rules;

public sealed class SpeedRuleCoordinator
{
    private readonly SpeedRuleEngine _engine;
    private string? _currentRuleId;
    private DateTimeOffset? _lastDispatchAt;
    private SpeedRangeRule? _currentRule;

    public SpeedRuleCoordinator(SpeedRuleEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    public RuleEvaluationResult Evaluate(
        TypingSpeedSnapshot snapshot,
        IReadOnlyList<SpeedRangeRule> rules,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(rules);

        var activeRule = _engine.Match(snapshot, rules).ActiveRule;
        if (activeRule is null)
        {
            var shouldStop = _currentRule?.StopOnExit == true;
            _currentRuleId = null;
            _currentRule = null;
            return new RuleEvaluationResult(null, false, shouldStop, null);
        }

        if (!string.Equals(_currentRuleId, activeRule.Id, StringComparison.Ordinal))
        {
            _currentRuleId = activeRule.Id;
            _currentRule = activeRule;
            _lastDispatchAt = now;
            return new RuleEvaluationResult(activeRule, activeRule.TriggerOnEnter, false, activeRule.WaveformId);
        }

        _currentRule = activeRule;
        if (!activeRule.RepeatWithinRange)
        {
            return new RuleEvaluationResult(activeRule, false, false, null);
        }

        var cooldown = TimeSpan.FromMilliseconds(Math.Max(0, activeRule.CooldownMs));
        var canDispatch = !_lastDispatchAt.HasValue || now - _lastDispatchAt.Value >= cooldown;
        if (!canDispatch)
        {
            return new RuleEvaluationResult(activeRule, false, false, null);
        }

        _lastDispatchAt = now;
        return new RuleEvaluationResult(activeRule, true, false, activeRule.WaveformId);
    }
}
