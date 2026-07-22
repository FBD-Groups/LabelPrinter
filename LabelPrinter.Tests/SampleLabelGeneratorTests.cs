using LabelPrinter.Printing;
using Xunit;

namespace LabelPrinter.Tests;

public class SampleLabelGeneratorTests
{
    [Fact]
    public void Epl_4x2_has_width_and_height_dots_at_203dpi()
    {
        var epl = SampleLabelGenerator.Generate(LabelPrintType.Epl, "4x2");

        Assert.Contains("q812", epl);        // 4in * 203
        Assert.Contains("Q406,24", epl);     // 2in * 203
        Assert.Contains("\"TEST 4x2\"", epl);
        Assert.Contains("P1", epl);
    }

    [Fact]
    public void Zpl_4x6_has_print_width_and_label_length()
    {
        var zpl = SampleLabelGenerator.Generate(LabelPrintType.Zpl, "4x6");

        Assert.Contains("^PW812", zpl);      // 4in
        Assert.Contains("^LL1218", zpl);     // 6in
        Assert.Contains("^FDTEST 4x6^FS", zpl);
        Assert.StartsWith("^XA", zpl);
        Assert.Contains("^XZ", zpl);
    }

    [Fact]
    public void Text_contains_size_and_form_feed()
    {
        var text = SampleLabelGenerator.Generate(LabelPrintType.Text, "4x3");

        Assert.Contains("TEST 4x3", text);
        Assert.EndsWith("\f", text);
    }

    [Fact]
    public void Pdf_generates_base64_of_a_valid_pdf_containing_size()
    {
        var base64 = SampleLabelGenerator.Generate(LabelPrintType.Pdf, "4x6");
        var bytes = Convert.FromBase64String(base64);
        var text = System.Text.Encoding.ASCII.GetString(bytes);

        Assert.StartsWith("%PDF-1.4", text);
        Assert.Contains("/MediaBox [0 0 288 432]", text); // 4in x 6in at 72pt/in
        Assert.Contains("(TEST 4x6)", text);
        Assert.EndsWith("%%EOF", text);
    }
}
