using System.IO;

namespace MultiBox.Setup;

/// <summary>Setup logging: file under %LocalAppData%\MultiBox\Setup\Logs plus an in-memory tail for the UI.</summary>
public static class SetupLog
{
    private static readonly object Gate = new();
    private static readonly string LogDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MultiBox", "Setup", "Logs");
    private static readonly string LogPath =
        Path.Combine(LogDir, $"setup-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    public static string LogFilePath => LogPath;

    public static event Action<string>? LineAdded;

    public static void Info(string message) => Write("INFO ", message);
    public static void Warn(string message) => Write("WARN ", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Error(string message, Exception ex)
    {
        Write("ERROR", $"{message}: {ex.Message}");
        if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            Write("     ", ex.StackTrace);
    }

    private static void Write(string level, string message)
    {
        string line = $"{DateTime.Now:HH:mm:ss.fff}  {level}  {message}";
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never take the installer down.
            }
        }
        LineAdded?.Invoke(message);
    }
}
