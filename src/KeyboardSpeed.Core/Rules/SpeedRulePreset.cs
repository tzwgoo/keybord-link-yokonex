namespace KeyboardSpeed.Core.Rules;

public sealed record SpeedRulePreset(
    string Id,
    string Name,
    string Description,
    double MinValue,
    double MaxValue,
    int CooldownMs,
    string WaveformId,
    bool Enabled,
    bool StopOnExit);
