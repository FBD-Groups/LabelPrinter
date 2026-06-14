using System.Net;
using System.Text;
using System.Text.Json;
using LabelPrinter.Printing;

namespace LabelPrinter.Services;

/// <summary>
/// Local HTTP endpoint: POST /LabelPrint with raw EPL body or JSON { "epl": "...", "alias": "..." }.
/// </summary>
public sealed class RestPrintListener : IDisposable
{
    private readonly AppConfig _config;
    private readonly PrintModel _printModel;
    private readonly Action<string> _log;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _task;

    public RestPrintListener(AppConfig config, PrintModel printModel, Action<string> log)
    {
        _config = config;
        _printModel = printModel;
        _log = log;
    }

    public void Start()
    {
        if (!_config.EnableRestEndpoint)
            return;

        Stop();
        _listener = new HttpListener();
        _listener.Prefixes.Add(_config.RestListenPrefix);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _task = Task.Run(() => ListenAsync(_cts.Token));
        _log($"REST listening on {_config.RestListenPrefix}LabelPrint");
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_listener?.IsListening == true)
            _listener.Stop();
        try
        {
            _task?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // ignore
        }

        _listener?.Close();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
        _task = null;
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener is { IsListening: true })
        {
            HttpListenerContext? ctx = null;
            try
            {
                ctx = await _listener.GetContextAsync().WaitAsync(token).ConfigureAwait(false);
                _ = Task.Run(() => HandleRequestAsync(ctx), token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log($"REST listener error: {ex.Message}");
            }
        }
    }

    private void HandleRequestAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath?.TrimEnd('/') ?? "";
            if (!ctx.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase)
                || !path.EndsWith("/LabelPrint", StringComparison.OrdinalIgnoreCase))
            {
                WriteResponse(ctx, 404, "Not Found");
                return;
            }

            using var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
            var body = reader.ReadToEnd();
            string epl;
            string? alias = null;

            if (ctx.Request.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                var doc = JsonDocument.Parse(body);
                epl = doc.RootElement.GetProperty("epl").GetString() ?? "";
                if (doc.RootElement.TryGetProperty("alias", out var aliasEl))
                    alias = aliasEl.GetString();
            }
            else
            {
                epl = body;
            }

            if (string.IsNullOrWhiteSpace(epl))
            {
                WriteResponse(ctx, 400, "EPL body is required.");
                return;
            }

            _printModel.PrintBarcode(epl, alias);
            _log("REST LabelPrint job completed.");
            WriteResponse(ctx, 200, "OK");
        }
        catch (Exception ex)
        {
            _log($"REST print failed: {ex.Message}");
            WriteResponse(ctx, 500, ex.Message);
        }
    }

    private static void WriteResponse(HttpListenerContext ctx, int status, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.Close();
    }

    public void Dispose() => Stop();
}
