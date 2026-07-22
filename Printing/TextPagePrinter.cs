using System.Drawing;
using System.Drawing.Printing;

namespace LabelPrinter.Printing;

/// <summary>
/// Renders plain text into printed pages via the printer's GDI driver, so it works on
/// any Windows printer (label, laser, Microsoft Print to PDF, ...). Used for the Text
/// print type. A form feed (\f) starts a new page; long pages overflow onto more sheets.
/// </summary>
public static class TextPagePrinter
{
    public static void Print(string printerName, string text)
    {
        using var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = printerName;
        if (!doc.PrinterSettings.IsValid)
            throw new InvalidOperationException($"Printer '{printerName}' is not available.");
        doc.DocumentName = "Label Printer Service";

        var pages = text
            .Replace("\r\n", "\n")
            .Split('\f')
            .Select(block => block.Split('\n'))
            .Where(lines => lines.Any(l => l.Trim().Length > 0))
            .ToList();

        if (pages.Count == 0)
            throw new InvalidOperationException("No printable text.");

        using var font = new Font("Consolas", 10f);
        var pageIndex = 0;
        Queue<string>? current = null;

        doc.PrintPage += (_, e) =>
        {
            current ??= new Queue<string>(pages[pageIndex]);
            var bounds = e.MarginBounds;
            var lineHeight = font.GetHeight(e.Graphics!);
            float y = bounds.Top;

            while (current.Count > 0 && y + lineHeight <= bounds.Bottom)
            {
                e.Graphics!.DrawString(current.Dequeue(), font, Brushes.Black, bounds.Left, y);
                y += lineHeight;
            }

            if (current.Count > 0)
            {
                e.HasMorePages = true; // this page block spills onto another sheet
                return;
            }

            pageIndex++;
            current = null;
            e.HasMorePages = pageIndex < pages.Count;
        };

        doc.Print();
    }
}
