namespace KeyboardSpeed.Tests.Desktop;

public sealed class DesktopProjectAssetTests
{
    [Fact]
    public void DesktopProject_ShouldEmbedYokonexLogoAsWpfResource()
    {
        var projectFile = ReadProjectFile();

        Assert.Contains("<Resource Include=\"Assets\\yokonex-logo.png\" />", projectFile);
    }

    private static string ReadProjectFile()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var projectPath = Path.Combine(repoRoot, "src", "KeyboardSpeed.Desktop", "KeyboardSpeed.Desktop.csproj");

        return File.ReadAllText(projectPath);
    }
}
