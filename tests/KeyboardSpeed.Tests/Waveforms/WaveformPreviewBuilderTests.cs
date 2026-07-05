using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class WaveformPreviewBuilderTests
{
    [Fact]
    public void Build_ShouldCreateAscendingTimelinePoints()
    {
        var waveform = new EmsWaveformDefinition
        {
            Steps =
            [
                new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 },
                new EmsWaveformStep { DurationMs = 200, AStrength = 30, BStrength = 40 }
            ]
        };

        var preview = WaveformPreviewBuilder.Build(waveform);

        Assert.Equal(4, preview.Points.Count);
        Assert.Equal(0, preview.Points[0].TimeMs);
        Assert.Equal(100, preview.Points[1].TimeMs);
        Assert.Equal(100, preview.Points[2].TimeMs);
        Assert.Equal(300, preview.Points[3].TimeMs);
        Assert.Equal(300, preview.TotalDurationMs);
    }

    [Fact]
    public void Build_ShouldRepeatTimelineByLoopCount()
    {
        var waveform = new EmsWaveformDefinition
        {
            LoopCount = 2,
            Steps =
            [
                new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 },
                new EmsWaveformStep { DurationMs = 200, AStrength = 30, BStrength = 40 }
            ]
        };

        var preview = WaveformPreviewBuilder.Build(waveform);

        Assert.Equal(8, preview.Points.Count);
        Assert.Equal(0, preview.Points[0].TimeMs);
        Assert.Equal(300, preview.Points[3].TimeMs);
        Assert.Equal(300, preview.Points[4].TimeMs);
        Assert.Equal(600, preview.Points[7].TimeMs);
        Assert.Equal(600, preview.TotalDurationMs);
    }
}
