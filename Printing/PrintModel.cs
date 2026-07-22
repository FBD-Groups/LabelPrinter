namespace LabelPrinter.Printing;

public sealed class PrintModel
{
    // GDI (PrintDocument) and the winspool RAW/LPT paths are not thread-safe, and a
    // physical printer can only render one job at a time anyway. Several REST listeners
    // and the WebSocket listener share a single PrintModel and can call PrintTo from
    // different threads at once, so serialize every print through this process-wide gate.
    // Static (not per-instance) so serialization survives PrintModel being recreated on
    // Restart while an earlier job is still finishing.
    private static readonly object PrintGate = new();

    // Admission control: cap how many print requests may be in flight at once. Printing
    // is already serialized by PrintGate, so this does not add parallelism — it bounds
    // the QUEUE, so a flood of REST calls (or several large PDFs, each holding rendered
    // bitmaps / decoded bytes in memory) can't pile up unbounded background work and
    // exhaust memory. Shared process-wide across every listener.
    private const int MaxConcurrentJobs = 4;
    private static readonly SemaphoreSlim Admission = new(MaxConcurrentJobs, MaxConcurrentJobs);

    /// <summary>
    /// Reserves one of the limited print slots. Returns false if all slots stay taken for
    /// <paramref name="timeoutMs"/> (0 = don't wait — reject immediately). Every call that
    /// returns true MUST be paired with exactly one <see cref="EndJob"/> in a finally.
    /// </summary>
    public static bool TryBeginJob(int timeoutMs = 0) => Admission.Wait(timeoutMs);

    /// <summary>Releases a slot reserved by <see cref="TryBeginJob"/>.</summary>
    public static void EndJob() => Admission.Release();

    /// <summary>
    /// Prints label data to a specific target. The target is either a Windows printer
    /// name or a parallel port ("LPT1".."LPT3").
    ///
    /// EPL/ZPL are sent RAW (bytes forwarded verbatim; only a real label printer can
    /// interpret them), and multiple labels separated by blank lines become separate
    /// jobs. Text and PDF are rendered through the Windows GDI print path (like Edge /
    /// Notepad), so they work on ordinary drivers — including label printers that do
    /// not understand raw PDF bytes.
    ///
    /// Calls are serialized process-wide: concurrent callers block until the in-flight
    /// job finishes, rather than corrupting shared GDI/spooler state.
    /// </summary>
    public void PrintTo(string data, string printerName, LabelPrintType printType)
    {
        if (string.IsNullOrWhiteSpace(data))
            throw new ArgumentException("Print data is empty.", nameof(data));
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("No printer is configured for this label size.");

        var isLpt = printerName.TrimStart().StartsWith("LPT", StringComparison.OrdinalIgnoreCase);

        lock (PrintGate)
        {
            if (printType == LabelPrintType.Text)
            {
                // Render text through the printer's GDI driver so it prints on ANY printer
                // (PDF, laser, label). A raw LPT port has no driver, so write bytes directly.
                if (isLpt)
                    LptPrinter.Print(printerName, data.Replace("\r\n", "\n").Replace("\n", "\r\n"));
                else
                    TextPagePrinter.Print(printerName, data);
                return;
            }

            if (printType == LabelPrintType.Pdf)
            {
                // Base64 PDF → render pages → GDI print. Raw PDF dump fails on most label
                // printers (they only speak ZPL/EPL); Edge "Print" works because it renders.
                if (isLpt)
                    throw new InvalidOperationException("PDF printing requires a Windows printer; LPT raw ports are not supported.");

                var pdfBytes = Convert.FromBase64String(data);
                PdfPagePrinter.Print(printerName, pdfBytes);
                return;
            }

            foreach (var block in SplitJobs(data))
            {
                if (isLpt)
                    LptPrinter.Print(printerName, block);
                else
                    RawPrinterHelper.SendStringToPrinter(printerName, block);
            }
        }
    }

    private static IEnumerable<string> SplitJobs(string data)
    {
        var normalized = data.Replace("\r\n", "\n").Trim();
        if (string.IsNullOrEmpty(normalized))
            yield break;

        var parts = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            foreach (var part in parts)
                yield return part.TrimEnd() + "\n";
            yield break;
        }

        yield return normalized.EndsWith('\n') ? normalized : normalized + "\n";
    }
}
