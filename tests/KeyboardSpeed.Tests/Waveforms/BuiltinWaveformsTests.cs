using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class BuiltinWaveformsTests
{
    [Fact]
    public void BuiltinWaveforms_ShouldExposeExpandedPresetLibrary()
    {
        var waveforms = BuiltinWaveforms.CreateDefaults();

        Assert.True(waveforms.Count >= 7);
        Assert.Contains(waveforms, waveform => waveform.Name == "心跳节奏");
        Assert.Contains(waveforms, waveform => waveform.Id == "alternating-sweep");
        Assert.Contains(waveforms, waveform => waveform.Id == "sprint-burst");
        Assert.Contains(waveforms, waveform => waveform.Id == "stair-ramp");
        Assert.Contains(waveforms, waveform => waveform.Id == "wave-cascade");
        Assert.Contains(waveforms, waveform => waveform.Id == "double-knock");
        Assert.Contains(waveforms, waveform => waveform.Id == "idle-jolt");
    }

    [Fact]
    public void BuiltinWaveforms_ShouldMatchWaveformNamesWithRhythmShape()
    {
        var waveforms = BuiltinWaveforms.CreateDefaults().ToDictionary(item => item.Id);

        var heartbeat = waveforms["heartbeat"];
        Assert.True(heartbeat.Steps.Count >= 3);
        Assert.True(heartbeat.Steps[0].AStrength > heartbeat.Steps[^1].AStrength);
        Assert.True(heartbeat.Steps[1].AStrength > heartbeat.Steps[^1].AStrength);

        var alternatingSweep = waveforms["alternating-sweep"];
        Assert.True(alternatingSweep.Steps[0].AStrength > alternatingSweep.Steps[0].BStrength);
        Assert.True(alternatingSweep.Steps[1].BStrength > alternatingSweep.Steps[1].AStrength);

        var stairRamp = waveforms["stair-ramp"];
        Assert.True(stairRamp.Steps[0].AStrength < stairRamp.Steps[1].AStrength);
        Assert.True(stairRamp.Steps[1].AStrength < stairRamp.Steps[2].AStrength);

        var doubleKnock = waveforms["double-knock"];
        Assert.True(doubleKnock.Steps.Count >= 3);
        Assert.True(doubleKnock.Steps[0].AStrength > doubleKnock.Steps[1].AStrength);
        Assert.True(doubleKnock.Steps[2].AStrength > doubleKnock.Steps[1].AStrength);

        var centerLock = waveforms["center-lock"];
        Assert.All(centerLock.Steps, step => Assert.Equal(step.AStrength, step.BStrength));

        var idleJolt = waveforms["idle-jolt"];
        Assert.True(idleJolt.Steps.Count >= 4);
        Assert.All(idleJolt.Steps.Where(step => step.AStrength > 0), step => Assert.True(step.AStrength >= 50));
        Assert.All(idleJolt.Steps.Where(step => step.BStrength > 0), step => Assert.True(step.BStrength >= 50));
        Assert.All(idleJolt.Steps, step => Assert.True(step.DurationMs <= 110));
    }
}
