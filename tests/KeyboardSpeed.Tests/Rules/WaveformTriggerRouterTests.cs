using KeyboardSpeed.Core.Rules;
using KeyboardSpeed.Core.Typing;

namespace KeyboardSpeed.Tests.Rules;

public sealed class WaveformTriggerRouterTests
{
    [Fact]
    public void EvaluateSnapshot_ShouldSuppressSpeedRuleMatchingWhenAnyKeypressModeIsActive()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        IReadOnlyList<SpeedRangeRule> rules =
        [
            new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true)
        ];

        var result = router.EvaluateSnapshot(
            snapshot,
            rules,
            WaveformTriggerMode.AnyKeypress,
            DateTimeOffset.Parse("2026-05-18T10:00:10+08:00"));

        Assert.Null(result.ActiveRule);
        Assert.False(result.ShouldDispatch);
        Assert.False(result.ShouldStop);
        Assert.Null(result.WaveformId);
    }

    [Fact]
    public void EvaluateSnapshot_ShouldSuppressSpeedRuleMatchingWhenSpecificKeypressModeIsActive()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));
        var snapshot = new TypingSpeedSnapshot(180, 36, 150, 30, 3);
        IReadOnlyList<SpeedRangeRule> rules =
        [
            new SpeedRangeRule("mid", "中速", SpeedMetricType.Kpm, 120, 220, "heartbeat", 1500, true, true, false, true)
        ];

        var result = router.EvaluateSnapshot(
            snapshot,
            rules,
            WaveformTriggerMode.SpecificKeypress,
            DateTimeOffset.Parse("2026-05-18T10:00:10+08:00"));

        Assert.Null(result.ActiveRule);
        Assert.False(result.ShouldDispatch);
        Assert.False(result.ShouldStop);
        Assert.Null(result.WaveformId);
    }

    [Fact]
    public void EvaluateKeystroke_ShouldDispatchConfiguredWaveformInAnyKeypressMode()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateKeystroke(
            WaveformTriggerMode.AnyKeypress,
            keypressWaveformId: "soft-pulse",
            specificKeyWaveformId: null);

        Assert.True(result.ShouldDispatch);
        Assert.Equal("soft-pulse", result.WaveformId);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void EvaluateKeystroke_ShouldDispatchConfiguredWaveformWhenSpecificKeyMatches()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateKeystroke(
            WaveformTriggerMode.SpecificKeypress,
            keypressWaveformId: "soft-pulse",
            specificKeyWaveformId: "wave-cascade");

        Assert.True(result.ShouldDispatch);
        Assert.Equal("wave-cascade", result.WaveformId);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void EvaluateKeystroke_ShouldNotDispatchWhenSpecificKeyDoesNotMatch()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateKeystroke(
            WaveformTriggerMode.SpecificKeypress,
            keypressWaveformId: "soft-pulse",
            specificKeyWaveformId: null);

        Assert.False(result.ShouldDispatch);
        Assert.Null(result.WaveformId);
    }

    [Fact]
    public void EvaluateKeystroke_ShouldNotDispatchWhenSpeedRuleModeIsActive()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateKeystroke(
            WaveformTriggerMode.SpeedRules,
            keypressWaveformId: "soft-pulse",
            specificKeyWaveformId: "wave-cascade");

        Assert.False(result.ShouldDispatch);
        Assert.Null(result.WaveformId);
    }

    [Fact]
    public void EvaluateMouseClick_ShouldDispatchConfiguredWaveformInMouseClickMode()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateMouseClick(
            WaveformTriggerMode.MouseClick,
            mouseClickWaveformId: "soft-pulse");

        Assert.True(result.ShouldDispatch);
        Assert.Equal("soft-pulse", result.WaveformId);
        Assert.False(result.ShouldStop);
    }

    [Fact]
    public void EvaluateMouseClick_ShouldNotDispatchWhenOtherModeIsActive()
    {
        var router = new WaveformTriggerRouter(new SpeedRuleCoordinator(new SpeedRuleEngine()));

        var result = router.EvaluateMouseClick(
            WaveformTriggerMode.AnyKeypress,
            mouseClickWaveformId: "soft-pulse");

        Assert.False(result.ShouldDispatch);
        Assert.Null(result.WaveformId);
    }
}
