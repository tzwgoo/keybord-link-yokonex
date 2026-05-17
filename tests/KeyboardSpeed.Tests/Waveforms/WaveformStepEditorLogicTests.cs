using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class WaveformStepEditorLogicTests
{
    [Fact]
    public void InsertStepAfter_ShouldAppendEditableCopyAfterIndex()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20, AMode = 1, BMode = 1, MotorState = 0 }
        };

        var updated = WaveformStepEditorLogic.InsertStepAfter(steps, 0);

        Assert.Equal(2, updated.Count);
        Assert.Equal(100, updated[1].DurationMs);
        Assert.Equal(10, updated[1].AStrength);
    }

    [Fact]
    public void MoveStep_ShouldSwapAdjacentSteps()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 100, AStrength = 10, BStrength = 20 },
            new EmsWaveformStep { DurationMs = 200, AStrength = 30, BStrength = 40 }
        };

        var updated = WaveformStepEditorLogic.MoveStep(steps, 1, moveUp: true);

        Assert.Equal(200, updated[0].DurationMs);
        Assert.Equal(100, updated[1].DurationMs);
    }

    [Fact]
    public void DeleteStep_ShouldKeepAtLeastOneStep()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 120, AStrength = 12, BStrength = 18 }
        };

        var updated = WaveformStepEditorLogic.DeleteStep(steps, 0);

        var remaining = Assert.Single(updated);
        Assert.Equal(100, remaining.DurationMs);
    }

    [Fact]
    public void UpdateStep_ShouldReplaceTargetStep()
    {
        var steps = new[]
        {
            new EmsWaveformStep { DurationMs = 120, AStrength = 12, BStrength = 18 }
        };

        var updated = WaveformStepEditorLogic.UpdateStep(steps, 0, new EmsWaveformStep
        {
            DurationMs = 160,
            AStrength = 24,
            AMode = 2,
            BStrength = 28,
            BMode = 3,
            MotorState = 1
        });

        Assert.Equal(160, updated[0].DurationMs);
        Assert.Equal(24, updated[0].AStrength);
        Assert.Equal(1, updated[0].MotorState);
    }
}
