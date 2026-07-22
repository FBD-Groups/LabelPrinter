using System.Globalization;
using System.Text;

namespace LabelPrinter.Printing;

/// <summary>
/// Builds a small sample label for the "test" buttons. Sizes are "WxH" in inches;
/// dot counts assume 203 dpi (8 dots/mm) printers.
/// </summary>
public static class SampleLabelGenerator
{
    private const int DotsPerInch = 203;
    private const int PointsPerInch = 72;

    public static string Generate(LabelPrintType type, string size)
    {
        var (widthDots, heightDots) = ToUnits(size, DotsPerInch);

        return type switch
        {
            LabelPrintType.Zpl =>
                $"^XA\n^PW{widthDots}\n^LL{heightDots}\n^FO50,50^A0N,40,40^FDTEST {size}^FS\n^XZ\n",
            LabelPrintType.Text =>
                $"TEST {size}\nLabel Printer Service\n\f",
            LabelPrintType.Pdf =>
                Convert.ToBase64String(GeneratePdfBytes(size)),
            _ => // Epl
                $"N\nq{widthDots}\nQ{heightDots},24\nA50,50,0,4,1,1,N,\"TEST {size}\"\nP1\n"
        };
    }

    /// <summary>
    /// Hand-built one-page PDF (no external library). Valid enough to open in any PDF
    /// viewer; used as the PDF test-button sample (Base64'd) and as REST payload input.
    /// </summary>
    public static byte[] GeneratePdfBytes(string size)
    {
        var (widthPt, heightPt) = ToUnits(size, PointsPerInch);
        var content =
            $"BT /F1 18 Tf 36 {heightPt - 52} Td ({EscapePdfText($"TEST {size}")}) Tj ET\n" +
            $"BT /F1 12 Tf 36 {heightPt - 72} Td ({EscapePdfText("Label Printer Service")}) Tj ET\n";
        var contentBytes = Encoding.ASCII.GetBytes(content);

        string[] objects =
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthPt} {heightPt}] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(Encoding.ASCII.GetBytes(s));

        Write("%PDF-1.4\n");
        var offsets = new long[6];
        for (var i = 0; i < objects.Length; i++)
        {
            offsets[i + 1] = ms.Position;
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        offsets[5] = ms.Position;
        Write($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
        ms.Write(contentBytes);
        Write("endstream\nendobj\n");

        var xrefStart = ms.Position;
        Write("xref\n0 6\n0000000000 65535 f \n");
        for (var i = 1; i <= 5; i++)
            Write($"{offsets[i]:D10} 00000 n \n");
        Write($"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefStart}\n%%EOF");

        return ms.ToArray();
    }

    private static string EscapePdfText(string text) =>
        text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static (int width, int height) ToUnits(string size, int unitsPerInch)
    {
        var parts = size.ToLowerInvariant().Split('x');
        if (parts.Length != 2
            || !double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var w)
            || !double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var h))
        {
            // Fall back to 4x6 if the size string is malformed.
            w = 4;
            h = 6;
        }

        return ((int)Math.Round(w * unitsPerInch), (int)Math.Round(h * unitsPerInch));
    }
}
