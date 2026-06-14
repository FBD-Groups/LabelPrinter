namespace LabelPrinter.Printing;

public sealed class PrintModel
{
    private readonly AppConfig _config;

    public PrintModel(AppConfig config) => _config = config;

    /// <summary>
    /// Prints one or more EPL label command blocks. Server may send multiple labels separated by blank lines.
    /// </summary>
    public void PrintBarcode(string eplData, string? printerAlias = null)
    {
        if (string.IsNullOrWhiteSpace(eplData))
            throw new ArgumentException("EPL data is empty.", nameof(eplData));

        foreach (var block in SplitEplJobs(eplData))
        {
            if (_config.UseLptPrinter)
                LptPrinter.Print(_config.LptPort, block);
            else
            {
                var printer = _config.ResolvePrinterName(printerAlias);
                RawPrinterHelper.SendStringToPrinter(printer, block);
            }
        }
    }

    private static IEnumerable<string> SplitEplJobs(string eplData)
    {
        var normalized = eplData.Replace("\r\n", "\n").Trim();
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
