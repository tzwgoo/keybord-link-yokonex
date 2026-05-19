using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Core.Configuration;

public sealed record AppSettings
{
    public const int DefaultIdleTriggerTimeoutMs = 2000;
    public const int DefaultRuleRepeatCooldownMs = 600;
    public const string DefaultIdleReminderWaveformId = "idle-jolt";

    public WaveformTriggerMode TriggerMode { get; init; } = WaveformTriggerMode.SpeedRules;

    public string KeypressWaveformId { get; init; } = "soft-pulse";

    public bool IdleTriggerEnabled { get; init; }

    public int IdleTriggerTimeoutMs { get; init; } = DefaultIdleTriggerTimeoutMs;

    public string IdleWaveformId { get; init; } = DefaultIdleReminderWaveformId;

    public List<SpeedRangeRule> SpeedRules { get; init; } = [];

    public List<EmsWaveformDefinition> Waveforms { get; init; } = [];

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            TriggerMode = WaveformTriggerMode.SpeedRules,
            KeypressWaveformId = "soft-pulse",
            IdleTriggerEnabled = false,
            IdleTriggerTimeoutMs = DefaultIdleTriggerTimeoutMs,
            IdleWaveformId = DefaultIdleReminderWaveformId,
            SpeedRules =
            [
                new SpeedRangeRule("low", "低速区", SpeedMetricType.Kpm, 0, 119.99, "soft-pulse", DefaultRuleRepeatCooldownMs, true, true, true, true),
                new SpeedRangeRule("mid", "中速区", SpeedMetricType.Kpm, 120, 220, "heartbeat", DefaultRuleRepeatCooldownMs, true, true, true, true)
            ],
            Waveforms = BuiltinWaveforms.CreateDefaults().ToList()
        };
    }
}
