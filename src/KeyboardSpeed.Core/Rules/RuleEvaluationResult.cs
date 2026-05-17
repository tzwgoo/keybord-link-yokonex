namespace KeyboardSpeed.Core.Rules;

public sealed record RuleEvaluationResult(
    SpeedRangeRule? ActiveRule,
    bool ShouldDispatch,
    bool ShouldStop,
    string? WaveformId);
