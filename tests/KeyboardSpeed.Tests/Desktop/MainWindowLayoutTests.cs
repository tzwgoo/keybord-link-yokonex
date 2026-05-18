namespace KeyboardSpeed.Tests.Desktop;

public sealed class MainWindowLayoutTests
{
    [Fact]
    public void TabItemTemplate_ShouldRenderHeaderInsteadOfContent()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var xamlPath = Path.Combine(repoRoot, "src", "KeyboardSpeed.Desktop", "MainWindow.xaml");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("ContentSource=\"Header\"", xaml);
    }
}
