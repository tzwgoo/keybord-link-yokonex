using System.Text;

namespace KeyboardSpeed.Core.Diagnostics;

public static class AppDiagnostics
{
    private static readonly Lock SyncRoot = new();

    public static string LogFilePath => Path.Combine(
        AppContext.BaseDirectory,
        "logs",
        "debug.log");

    public static void WriteInfo(string source, string message)
    {
        WriteLine("INFO", source, message);
    }

    public static void WriteException(string source, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        WriteLine("ERROR", source, $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception}");
    }

    private static void WriteLine(string level, string source, string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] [{source}] {message}{Environment.NewLine}";
            lock (SyncRoot)
            {
                File.AppendAllText(LogFilePath, line, new UTF8Encoding(false));
            }
        }
        catch
        {
            // Diagnostics must never crash the app.
        }
    }
}
