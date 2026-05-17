using KeyboardSpeed.Core.Diagnostics;

namespace KeyboardSpeed.Tests.Diagnostics;

public sealed class AppDiagnosticsTests
{
    [Fact]
    public void LogFilePath_ShouldPointToProgramRootLogsDebugLog()
    {
        var expected = Path.Combine(AppContext.BaseDirectory, "logs", "debug.log");

        Assert.Equal(expected, AppDiagnostics.LogFilePath);
    }

    [Fact]
    public void WriteInfo_ShouldCreateDebugLogUnderProgramRoot()
    {
        var logFilePath = AppDiagnostics.LogFilePath;
        if (File.Exists(logFilePath))
        {
            File.Delete(logFilePath);
        }

        AppDiagnostics.WriteInfo("AppDiagnosticsTests", "hello debug log");

        Assert.True(File.Exists(logFilePath));
        var content = File.ReadAllText(logFilePath);
        Assert.Contains("hello debug log", content);
    }
}
