using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using LabelPrinter.Printing;

namespace LabelPrinter.Services;

/// <summary>
/// Stands in for a real C-Lodop install so an existing caller (MZL's `lodop_print.js`,
/// which loads `http://localhost:8000/CLodopfuncs.js` — with `18000` as its own built-in
/// fallback — and then calls `LODOP.ADD_PRINT_PDF(...); LODOP.PRINT();`) can print PDFs
/// through LabelPrinter with zero changes on the caller's side.
///
/// Only the tiny slice of the real C-Lodop JS API that caller actually uses is
/// implemented: getCLodop/SET_LICENSES/ADD_PRINT_PDF/SET_PRINTER_INDEX/PRINT. This does
/// NOT replicate C-Lodop's real wire protocol (WebSocket / iframe-post) — the JS this
/// class serves is entirely our own, so it can call back to our own /lodop_print endpoint
/// however we like; the caller only ever sees the LODOP.* JS surface.
///
/// Ports 8000 and 18000 are hardcoded in the caller's `lodop_print.js` (not configurable
/// there), so they are fixed here too — not exposed as a user-editable setting. A real
/// C-Lodop install must be uninstalled first so these ports are free.
/// </summary>
public sealed class LodopCompatListener : IDisposable
{
    private const int PrimaryPort = 8000;
    private const int FallbackPort = 18000;

    // Real shipping-label PDFs are small; this just stops a pathological/misbehaving
    // response from ballooning memory. Matches RestPrintListener's body cap.
    private const long MaxPdfBytes = 10L * 1024 * 1024;
    private const long MaxRequestBodyBytes = 8 * 1024;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

    private readonly LodopCompatConfig _config;
    private readonly PrintModel _printModel;
    private readonly Action<string> _log;
    private readonly List<Port> _ports = new();

    /// <summary>Ports this instance actually managed to bind (empty if both were taken).</summary>
    public IReadOnlyList<int> BoundPorts => _ports.Select(p => p.Number).ToList();

    public LodopCompatListener(LodopCompatConfig config, PrintModel printModel, Action<string> log)
    {
        _config = config;
        _printModel = printModel;
        _log = log;
    }

    public void Start()
    {
        Stop();
        // Two independent HttpListener instances, not one with two Prefixes: a single
        // HttpListener.Start() fails entirely if ANY of its prefixes can't bind, so
        // sharing one instance would mean a busy 8000 also kills 18000. Binding
        // separately means whichever port is free still works.
        StartOne(PrimaryPort);
        StartOne(FallbackPort);

        if (_ports.Count == 0)
            _log("Lodop-compat: failed to bind both 8000 and 18000 — feature is unavailable.");
    }

    // A just-stopped listener (ours from a moment ago — Save & Apply / Reconnect calls
    // Stop() then immediately Start() again — or a previous run of this same process)
    // can leave the OS a moment behind: HttpListener.Close() returns before the port is
    // necessarily free to rebind. Observed in practice: an immediate restart failed to
    // bind BOTH 8000 and 18000 even though nothing else was using them a second later.
    // Retry briefly instead of giving up on the first attempt.
    private const int MaxBindAttempts = 4;
    private static readonly TimeSpan BindRetryDelay = TimeSpan.FromMilliseconds(300);

    private void StartOne(int port)
    {
        // A failed Start() leaves that HttpListener instance unusable (it throws
        // ObjectDisposedException on a subsequent Start(), confirmed empirically) — each
        // retry attempt needs a brand new instance, not another Start() on the same one.
        HttpListener? listener = null;
        for (var attempt = 1; attempt <= MaxBindAttempts; attempt++)
        {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                listener.Start();
                break;
            }
            catch (HttpListenerException ex) when (attempt < MaxBindAttempts)
            {
                _log($"Lodop-compat: {port} busy on attempt {attempt}/{MaxBindAttempts} ({ex.Message}), retrying...");
                listener = null;
                Thread.Sleep(BindRetryDelay);
            }
            catch (HttpListenerException ex)
            {
                _log($"Lodop-compat: failed to listen on {port}: {ex.Message}");
                return;
            }
        }

        if (listener is null)
            return;

        var cts = new CancellationTokenSource();
        var task = Task.Run(() => ListenAsync(listener, port, cts.Token));
        _ports.Add(new Port(port, listener, cts, task));
        _log($"Lodop-compat: listening on http://localhost:{port} -> {_config.PrinterName}");
    }

    public void Stop()
    {
        foreach (var p in _ports)
        {
            p.Cts.Cancel();
            if (p.Listener.IsListening)
                p.Listener.Stop();
            try
            {
                p.Task.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // ignore on shutdown
            }

            p.Listener.Close();
            p.Cts.Dispose();
        }

        _ports.Clear();
    }

    private async Task ListenAsync(HttpListener listener, int port, CancellationToken token)
    {
        while (!token.IsCancellationRequested && listener.IsListening)
        {
            try
            {
                var ctx = await listener.GetContextAsync().WaitAsync(token).ConfigureAwait(false);
                _ = Task.Run(() => HandleRequest(ctx, port), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"Lodop-compat [{port}] listener error: {ex.Message}");
            }
        }
    }

    private void HandleRequest(HttpListenerContext ctx, int port)
    {
        try
        {
            if (ctx.Request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                WriteCors(ctx.Response);
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
                return;
            }

            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
                && (path == "/" || path.Equals("/CLodopfuncs.js", StringComparison.OrdinalIgnoreCase)))
            {
                WriteText(ctx, 200, "application/javascript; charset=utf-8", BuildClodopFuncsJs(port));
                return;
            }

            if (ctx.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
                && path.Equals("/_test_sample.pdf", StringComparison.OrdinalIgnoreCase))
            {
                WriteBytes(ctx, 200, "application/pdf", GetSamplePdfBytes());
                return;
            }

            if (ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
                && path.Equals("/lodop_print", StringComparison.OrdinalIgnoreCase))
            {
                HandlePrint(ctx, port);
                return;
            }

            WriteText(ctx, 404, "text/plain; charset=utf-8", "Not Found");
        }
        catch (Exception ex)
        {
            _log($"Lodop-compat [{port}] request failed: {ex.Message}");
            try
            {
                WriteText(ctx, 500, "text/plain; charset=utf-8", ex.Message);
            }
            catch
            {
                // response already closed/client gone; nothing more to do
            }
        }
    }

    private void HandlePrint(HttpListenerContext ctx, int port)
    {
        if (!PrintModel.TryBeginJob())
        {
            _log($"Lodop-compat [{port}]: busy — rejected (503).");
            WriteText(ctx, 503, "text/plain; charset=utf-8", "Printer busy, retry shortly.");
            return;
        }

        try
        {
            if (string.IsNullOrWhiteSpace(_config.PrinterName))
            {
                _log($"Lodop-compat [{port}]: no printer configured.");
                WriteText(ctx, 500, "text/plain; charset=utf-8", "No printer configured for Lodop compatibility.");
                return;
            }

            if (!TryReadRequestBody(ctx, MaxRequestBodyBytes, out var body))
            {
                WriteText(ctx, 413, "text/plain; charset=utf-8", "Request body too large.");
                return;
            }

            string? pdfUrl;
            try
            {
                using var doc = JsonDocument.Parse(body);
                pdfUrl = doc.RootElement.GetProperty("pdfUrl").GetString();
            }
            catch (Exception ex)
            {
                WriteText(ctx, 400, "text/plain; charset=utf-8", $"Invalid request body: {ex.Message}");
                return;
            }

            if (string.IsNullOrWhiteSpace(pdfUrl))
            {
                WriteText(ctx, 400, "text/plain; charset=utf-8", "pdfUrl is required.");
                return;
            }

            byte[] pdfBytes;
            try
            {
                pdfBytes = FetchPdfBytes(pdfUrl);
            }
            catch (Exception ex)
            {
                _log($"Lodop-compat [{port}]: failed to fetch '{pdfUrl}': {ex.Message}");
                WriteText(ctx, 502, "text/plain; charset=utf-8", $"Failed to fetch PDF: {ex.Message}");
                return;
            }

            _printModel.PrintTo(Convert.ToBase64String(pdfBytes), _config.PrinterName, LabelPrintType.Pdf);
            _log($"Lodop-compat [{port}]: printed '{pdfUrl}' to {_config.PrinterName}.");
            WriteText(ctx, 200, "text/plain; charset=utf-8", "OK");
        }
        catch (Exception ex)
        {
            _log($"Lodop-compat [{port}]: print failed: {ex.Message}");
            WriteText(ctx, 500, "text/plain; charset=utf-8", ex.Message);
        }
        finally
        {
            PrintModel.EndJob();
        }
    }

    private static byte[] FetchPdfBytes(string pdfUrl)
    {
        using var response = Http.GetAsync(pdfUrl, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream();
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > MaxPdfBytes)
                throw new InvalidOperationException($"PDF exceeds {MaxPdfBytes / (1024 * 1024)} MB limit.");
            ms.Write(buffer, 0, read);
        }

        return ms.ToArray();
    }

    private static bool TryReadRequestBody(HttpListenerContext ctx, long maxBytes, out string body)
    {
        body = "";
        if (ctx.Request.ContentLength64 > maxBytes)
            return false;

        using var ms = new MemoryStream();
        var buffer = new byte[4096];
        long total = 0;
        int read;
        while ((read = ctx.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
            if (total > maxBytes)
                return false;
            ms.Write(buffer, 0, read);
        }

        body = (ctx.Request.ContentEncoding ?? Encoding.UTF8).GetString(ms.ToArray());
        return true;
    }

    private static byte[] GetSamplePdfBytes()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LabelPrinter.sample-label.pdf")
            ?? throw new InvalidOperationException("Embedded sample-label.pdf resource is missing.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // The caller page is on a different origin than localhost:8000/18000, so every
    // response needs CORS headers, and preflight OPTIONS requests must succeed.
    private static void WriteCors(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
    }

    private static void WriteText(HttpListenerContext ctx, int status, string contentType, string text) =>
        WriteBytes(ctx, status, contentType, Encoding.UTF8.GetBytes(text));

    private static void WriteBytes(HttpListenerContext ctx, int status, string contentType, byte[] bytes)
    {
        try
        {
            WriteCors(ctx.Response);
            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = bytes.Length;
            ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            ctx.Response.Close();
        }
        catch
        {
            // Client disconnected or listener stopped mid-response; runs on a background
            // Task, so swallow rather than let it escape.
        }
    }

    /// <summary>
    /// The minimal C-Lodop-compatible JS surface. `port` is whichever of 8000/18000 this
    /// particular request actually came in on, baked into the absolute URL the script
    /// posts back to — a relative URL would resolve against the CALLER PAGE's origin
    /// (wrong host entirely), not this script's origin, since the script executes in the
    /// caller page's document.
    /// </summary>
    public static string BuildClodopFuncsJs(int port) => $$"""
        function getCLodop(){ return CLODOP; }
        var CLODOP = {
          VERSION: "6.6.4.2",
          CVERSION: "6.6.4.2",
          SET_LICENSES: function(){},
          ADD_PRINT_PDF: function(top,left,width,height,pdfUrl){ this._pdfUrl = pdfUrl; },
          SET_PRINTER_INDEX: function(index){ /* ignored: LabelPrinter's Lodop-compat row targets a single fixed printer */ },
          PRINT: function(){
            var self = this;
            fetch('http://localhost:{{port}}/lodop_print', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ pdfUrl: self._pdfUrl })
            });
          }
        };
        """;

    public void Dispose() => Stop();

    private sealed record Port(int Number, HttpListener Listener, CancellationTokenSource Cts, Task Task);
}
