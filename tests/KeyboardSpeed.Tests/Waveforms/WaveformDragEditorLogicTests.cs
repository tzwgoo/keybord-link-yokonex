using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class WaveformDragEditorLogicTests
{
    [Fact]
    public void BuildHandles_ShouldCreateStrengthAndDurationHandles()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 },
            new EmsWaveformStep { DurationMs = 200, AStrength = 30, BStrength = 40 }
        };

        var handles = WaveformDragEditorLogic.BuildHandles(steps, 320, 210);

        Assert.Equal(5, handles.Count);
        Assert.Equal(2, handles.Count(item => item.Kind == WaveformDragHandleKind.ChannelA));
        Assert.Equal(2, handles.Count(item => item.Kind == WaveformDragHandleKind.ChannelB));
        Assert.Single(handles, item => item.Kind == WaveformDragHandleKind.Duration);
    }

    [Fact]
    public void UpdateStrength_ShouldMapVerticalDragIntoChannelStrength()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 }
        };

        var updated = WaveformDragEditorLogic.UpdateStrength(
            steps,
            0,
            WaveformDragHandleKind.ChannelA,
            y: 12,
            height: 210);

        Assert.Equal(EmsWaveformStep.MaxStrength, updated[0].AStrength);
        Assert.Equal(20, updated[0].BStrength);
    }

    [Fact]
    public void UpdateDurationFromDelta_ShouldIncreaseStepDurationWhenDraggedRight()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 },
            new EmsWaveformStep { DurationMs = 100, AStrength = 30, BStrength = 40 }
        };

        var updated = WaveformDragEditorLogic.UpdateDurationFromDelta(steps, 0, deltaX: 30, width: 320);

        Assert.True(updated[0].DurationMs > 100);
        Assert.Equal(100, updated[1].DurationMs);
    }
}
