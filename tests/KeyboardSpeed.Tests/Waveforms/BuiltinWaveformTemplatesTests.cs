using KeyboardSpeed.Core.Waveforms;

namespace KeyboardSpeed.Tests.Waveforms;

public sealed class BuiltinWaveformTemplatesTests
{
    [Fact]
    public void BuiltinWaveformTemplates_ShouldExposeReadyToUseScripts()
    {
        var templates = BuiltinWaveformTemplates.CreateDefaults();

        Assert.True(templates.Count >= 4);
        Assert.Contains(templates, template => template.Id == "heartbeat-template");
        Assert.Contains(templates, template => template.Script.Contains("120,36,1,32,1,0", StringComparison.Ordinal));
    }
}
