namespace LabelPrinter;

/// <summary>
/// Best-effort append-only logger to logs/labelprinter-yyyy-MM-dd.log — one file per
/// calendar day, so a long-running tray process doesn't grow a single ever-larger log
/// file. Never throws — logging must not be able to take the process down, so all I/O
/// errors are swallowed. Used both by the tray's live log and by the process-level crash
/// handlers in Program. Old day-files are left in place (not auto-deleted); prune
/// manually if disk space becomes a concern.
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
            var path = Path.Combine(logDir, $"labelprinter-{DateTime.Now:yyyy-MM-dd}.log");
            lock (Gate)
            {
                File.AppendAllText(
                    path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging is best-effort; never let a logging failure escape.
        }
    }
}
