namespace LabelPrinter;

/// <summary>
/// Best-effort append-only logger to logs/labelprinter.log. Never throws — logging
/// must not be able to take the process down, so all I/O errors are swallowed. Used
/// both by the tray's live log and by the process-level crash handlers in Program.
/// </summary>
public static class FileLog
{
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            lock (Gate)
            {
                File.AppendAllText(
                    Path.Combine(logDir, "labelprinter.log"),
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging is best-effort; never let a logging failure escape.
        }
    }
}
