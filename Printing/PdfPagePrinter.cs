using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Data.Pdf;
using Windows.Storage.Streams;

namespace LabelPrinter.Printing;

/// <summary>
/// Renders PDF pages to bitmaps via Windows.Data.Pdf, then prints through the
/// printer's GDI driver (same path Edge uses). Label printers do not understand
/// raw PDF bytes; dumping them with WritePrinter is a silent no-op on most models.
/// </summary>
public static class PdfPagePrinter
{
    public static void Print(string printerName, byte[] pdfBytes)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            throw new ArgumentException("PDF data is empty.", nameof(pdfBytes));

        var (images, pageSizePoints) = RenderPages(pdfBytes);
        try
        {
            if (images.Count == 0)
                throw new InvalidOperationException("PDF has no pages.");

            using var doc = new PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;
            if (!doc.PrinterSettings.IsValid)
                throw new InvalidOperationException($"Printer '{printerName}' is not available.");
            doc.DocumentName = "ControlCode Label PDF";

            // Without this, PageBounds comes from whatever page size the driver currently
            // has configured (e.g. a leftover default), not the PDF's actual size — the
            // image then gets scaled/centered against the WRONG page and part of the label
            // ends up beyond where the physical stock is cut. PaperSize is in hundredths
            // of an inch; PDF points are 1/72 inch.
            doc.DefaultPageSettings.PaperSize = new PaperSize(
                "Label",
                (int)Math.Round(pageSizePoints.Width / 72 * 100),
                (int)Math.Round(pageSizePoints.Height / 72 * 100));
            doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            var pageIndex = 0;
            doc.PrintPage += (_, e) =>
            {
                var img = images[pageIndex];
                var bounds = e.PageBounds;
                var scale = Math.Min((float)bounds.Width / img.Width, (float)bounds.Height / img.Height);
                var w = img.Width * scale;
                var h = img.Height * scale;
                var x = bounds.Left + (bounds.Width - w) / 2f;
                var y = bounds.Top + (bounds.Height - h) / 2f;
                e.Graphics!.DrawImage(img, x, y, w, h);
                pageIndex++;
                e.HasMorePages = pageIndex < images.Count;
            };

            doc.Print();
        }
        finally
        {
            foreach (var img in images)
                img.Dispose();
        }
    }

    private static (List<Image> images, SizeF firstPageSizePoints) RenderPages(byte[] pdfBytes) =>
        RenderPagesAsync(pdfBytes).ConfigureAwait(false).GetAwaiter().GetResult();

    private static async Task<(List<Image>, SizeF)> RenderPagesAsync(byte[] pdfBytes)
    {
        using var mem = new InMemoryRandomAccessStream();
        await mem.WriteAsync(pdfBytes.AsBuffer()).AsTask().ConfigureAwait(false);
        mem.Seek(0);

        var pdf = await PdfDocument.LoadFromStreamAsync(mem).AsTask().ConfigureAwait(false);
        var list = new List<Image>((int)pdf.PageCount);
        var firstPageSize = SizeF.Empty;

        for (uint i = 0; i < pdf.PageCount; i++)
        {
            using var page = pdf.GetPage(i);
            if (i == 0)
                firstPageSize = new SizeF((float)page.Size.Width, (float)page.Size.Height);
            using var outStream = new InMemoryRandomAccessStream();

            // ~2× PDF point size ≈ 144 dpi equivalent — sharp enough for thermal labels.
            var options = new PdfPageRenderOptions
            {
                DestinationWidth = Math.Max(1u, (uint)Math.Round(page.Size.Width * 2)),
                DestinationHeight = Math.Max(1u, (uint)Math.Round(page.Size.Height * 2))
            };
            await page.RenderToStreamAsync(outStream, options).AsTask().ConfigureAwait(false);
            outStream.Seek(0);

            using var netStream = outStream.AsStreamForRead();
            // Clone into a MemoryStream-backed Bitmap so the WinRT stream can close.
            using var ms = new MemoryStream();
            await netStream.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;
            using var temp = Image.FromStream(ms);
            list.Add(new Bitmap(temp));
        }

        return (list, firstPageSize);
    }
}
