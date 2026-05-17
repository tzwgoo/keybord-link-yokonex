using KeyboardSpeed.Core.Rules;

namespace KeyboardSpeed.Tests.Rules;

public sealed class BuiltinSpeedRulePresetsTests
{
    [Fact]
    public void BuiltinSpeedRulePresets_ShouldExposeTypingRangeRecommendations()
    {
        var presets = BuiltinSpeedRulePresets.CreateDefaults();

        Assert.True(presets.Count >= 3);
        var preset = Assert.Single(presets, item => item.Id == "mid-rhythm");
        Assert.Equal(120d, preset.MinValue);
        Assert.Equal("heartbeat", preset.WaveformId);
    }
}
