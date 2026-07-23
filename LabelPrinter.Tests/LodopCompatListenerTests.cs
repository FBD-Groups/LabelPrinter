using System.Text.RegularExpressions;
using LabelPrinter.Services;
using Xunit;

namespace LabelPrinter.Tests;

public class LodopCompatListenerTests
{
    [Theory]
    [InlineData(8000)]
    [InlineData(18000)]
    public void BuildClodopFuncsJs_exposes_the_functions_getLodop_hard_requires(int port)
    {
        var js = LodopCompatListener.BuildClodopFuncsJs(port);

        Assert.Contains("function getCLodop()", js);
        Assert.Contains("SET_LICENSES:", js); // getLodop() calls this unconditionally — must exist or it throws
        Assert.Contains("ADD_PRINT_PDF:", js);
        Assert.Contains("SET_PRINTER_INDEX:", js);
        Assert.Contains("PRINT:", js);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(18000)]
    public void BuildClodopFuncsJs_posts_to_an_absolute_url_on_the_port_the_request_hit(int port)
    {
        var js = LodopCompatListener.BuildClodopFuncsJs(port);

        // Must be absolute (the script runs in the CALLER page's origin, so a relative
        // path would resolve against that page, not this service) and must match
        // whichever port (8000 or 18000) actually served this script — not hardcoded.
        var match = Regex.Match(js, @"fetch\('(http://localhost:(\d+)/lodop_print)'");
        Assert.True(match.Success, "Expected an absolute http://localhost:<port>/lodop_print fetch URL.");
        Assert.Equal(port.ToString(), match.Groups[2].Value);
    }
}
