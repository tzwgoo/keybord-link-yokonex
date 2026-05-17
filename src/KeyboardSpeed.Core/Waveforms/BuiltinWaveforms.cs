namespace KeyboardSpeed.Core.Waveforms;

public static class BuiltinWaveforms
{
    public static IReadOnlyList<EmsWaveformDefinition> CreateDefaults()
    {
        return
        [
            new EmsWaveformDefinition
            {
                Id = "soft-pulse",
                Name = "Soft Pulse",
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 180,
                        AStrength = 24,
                        BStrength = 20
                    }
                ]
            },
            new EmsWaveformDefinition
            {
                Id = "heartbeat",
                Name = "Heartbeat",
                LoopCount = 2,
                Steps =
                [
                    new EmsWaveformStep
                    {
                        DurationMs = 120,
                        AStrength = 36,
                        BStrength = 32
                    },
                    new EmsWaveformStep
                    {
                        DurationMs = 90,
                        AStrength = 18,
                        BStrength = 16
                    }
                ]
            }
        ];
    }
}
