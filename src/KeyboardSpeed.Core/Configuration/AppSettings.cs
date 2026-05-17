using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Core.Configuration;

public sealed record AppSettings
{
    public List<SpeedRangeRule> SpeedRules { get; init; } = [];

    public List<EmsWaveformDefinition> Waveforms { get; init; } = [];

    public static AppSettings CreateDefault()
    {
        return new AppSettings
        {
            SpeedRules =
            [
                new SpeedRangeRule("low", "低速区", SpeedMetricType.Kpm, 0, 119.99, "soft-pulse", 1500, true, true, false, true),
                new SpeedRangeRule("mid", "中速区", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true)
            ],
            Waveforms = BuiltinWaveforms.CreateDefaults().ToList()
        };
    }
}
