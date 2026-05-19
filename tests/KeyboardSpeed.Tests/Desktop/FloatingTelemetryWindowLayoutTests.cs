namespace KeyboardSpeed.Tests.Desktop;

public sealed class FloatingTelemetryWindowLayoutTests
{
    [Fact]
    public void FloatingTelemetryWindow_ShouldUseTopmostFramelessShell()
    {
        var xaml = ReadXaml();

        Assert.Contains("Topmost=\"True\"", xaml);
        Assert.Contains("WindowStyle=\"None\"", xaml);
        Assert.Contains("ResizeMode=\"NoResize\"", xaml);
        Assert.Contains("ShowInTaskbar=\"False\"", xaml);
    }

    [Fact]
    public void FloatingTelemetryWindow_ShouldContainRealtimeTelemetryLabels()
    {
        var xaml = ReadXaml();

        Assert.Contains("Text=\"实时波形\"", xaml);
        Assert.Contains("Text=\"实时强度\"", xaml);
        Assert.Contains("Text=\"A 强度\"", xaml);
        Assert.Contains("Text=\"B 强度\"", xaml);
        Assert.DoesNotContain("Text=\"当前规则\"", xaml);
        Assert.DoesNotContain("Text=\"当前波形\"", xaml);
    }

    [Fact]
    public void FloatingTelemetryWindow_ShouldUseRectangularStrengthBars()
    {
        var xaml = ReadXaml();

        Assert.Matches(
            "x:Name=\"ChannelABar\"[\\s\\S]*?Background=\"#4FD1C5\"[\\s\\S]*?CornerRadius=\"6\"",
            xaml);
        Assert.Matches(
            "x:Name=\"ChannelBBar\"[\\s\\S]*?Background=\"#F59E0B\"[\\s\\S]*?CornerRadius=\"6\"",
            xaml);
        Assert.DoesNotMatch(
            "x:Name=\"ChannelABar\"[\\s\\S]*?CornerRadius=\"999\"",
            xaml);
        Assert.DoesNotMatch(
            "x:Name=\"ChannelBBar\"[\\s\\S]*?CornerRadius=\"999\"",
            xaml);
    }

    [Fact]
    public void FloatingTelemetryWindow_ShouldReserveEnoughHeightForStrengthPanel()
    {
        var xaml = ReadXaml();

        Assert.Contains("Height=\"452\"", xaml);
        Assert.Contains("Height=\"96\"", xaml);
        Assert.Contains("MinHeight=\"104\"", xaml);
    }

    private static string ReadXaml()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(repoRoot, "src", "KeyboardSpeed.Desktop", "FloatingTelemetryWindow.xaml");

        return File.ReadAllText(xamlPath);
    }
}
