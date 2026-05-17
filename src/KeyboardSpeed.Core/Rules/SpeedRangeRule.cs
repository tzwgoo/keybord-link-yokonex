namespace KeyboardSpeed.Core.Rules;

public sealed record SpeedRangeRule(
    string Id,
    string Name,
    SpeedMetricType MetricType,
    double MinValue,
    double MaxValue,
    string WaveformId,
    int CooldownMs,
    bool Enabled,
    bool TriggerOnEnter,
    bool RepeatWithinRange,
    bool StopOnExit);
