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
            Waveforms = BuiltinWaveforms.CreateDefaults().ToList()
        };
    }
}
