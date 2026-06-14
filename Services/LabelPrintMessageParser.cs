namespace LabelPrinter.Services;

public readonly record struct LabelPrintMessage(string EplData, string? PrinterAlias);

public static class LabelPrintMessageParser
{
    private const string Prefix = "LabelPrint";

    /// <summary>
    /// Parses server messages: "LabelPrint {epl}" or "LabelPrint|alias|{epl}".
    /// </summary>
    public static bool TryParse(string message, out LabelPrintMessage result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var text = message.Trim();
        if (!text.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var payload = text.Length > Prefix.Length ? text[Prefix.Length..].TrimStart() : "";
        if (payload.StartsWith('|'))
        {
            var segments = payload.Split('|', 3, StringSplitOptions.None);
            if (segments.Length >= 3)
            {
                result = new LabelPrintMessage(segments[2], segments[1]);
                return !string.IsNullOrWhiteSpace(result.EplData);
            }
        }

        result = new LabelPrintMessage(payload, null);
        return !string.IsNullOrWhiteSpace(result.EplData);
    }
}
