using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class BuiltinWaveformsTests
{
    [Fact]
    public void BuiltinWaveforms_ShouldIncludeHeartbeatPreset()
    {
        var waveforms = BuiltinWaveforms.CreateDefaults();

        Assert.Contains(waveforms, waveform => waveform.Name == "Heartbeat");
    }
}
