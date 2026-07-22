using System.Net.WebSockets;
using System.Text;
using LabelPrinter.Printing;

namespace LabelPrinter.Services;

public sealed class WebSocketPrintListener : IAsyncDisposable
{
    private readonly AppConfig _config;
    private readonly PrintModel _printModel;
    private readonly Action<string> _log;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private bool _loggedRetryHint;

    public WebSocketPrintListener(AppConfig config, PrintModel printModel, Action<string> log)
    {
        _config = config;
        _printModel = printModel;
        _log = log;
    }

    public bool IsConnected { get; private set; }

    public void Start()
    {
        if (!_config.EnableWebSocket)
            return;

        Stop();
        _loggedRetryHint = false;
        _cts = new CancellationTokenSource();
        _listenTask = RunAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        try
        {
            _listenTask?.Wait(TimeSpan.FromSeconds(3));
        }
        catch
        {
            // ignore on shutdown
        }

        _cts?.Dispose();
        _cts = null;
        _listenTask = null;
        IsConnected = false;
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReceiveAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                if (!_loggedRetryHint)
                {
                    _log($"WebSocket error: {ex.Message}");
                    _loggedRetryHint = true;
                }
            }

            if (stoppingToken.IsCancellationRequested)
                break;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_config.ReconnectDelaySeconds), stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ConnectAndReceiveAsync(CancellationToken token)
    {
        using var socket = new ClientWebSocket();
        var uri = new Uri(_config.LabelPrinterUrl);

        if (!_loggedRetryHint)
            _log($"Connecting to {uri}...");

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        connectCts.CancelAfter(TimeSpan.FromSeconds(_config.WebSocketConnectTimeoutSeconds));
        await socket.ConnectAsync(uri, connectCts.Token).ConfigureAwait(false);

        IsConnected = true;
        _loggedRetryHint = false;
        _log("WebSocket connected.");

        var buffer = new byte[8192];
        var builder = new StringBuilder();

        while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            builder.Clear();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    IsConnected = false;
                    _log("WebSocket closed by server.");
                    return;
                }

                builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            var message = builder.ToString();
            if (LabelPrintMessageParser.TryParse(message, out var printMsg))
            {
                var format = _config.FindFormatByAlias(printMsg.PrinterAlias);
                if (format == null)
                {
                    _log($"No enabled label format matches alias '{printMsg.PrinterAlias ?? "(none)"}'. Skipped.");
                }
                else
                {
                    // Print off the receive loop: printing is synchronous and can block for
                    // seconds (slow printer, PDF rendering). Blocking here would stall the
                    // whole message stream. PrintModel serializes concurrent jobs itself.
                    var fmt = format;
                    var job = printMsg;
                    _ = Task.Run(() =>
                    {
                        // These are real jobs from the RMA server, so wait for a print slot
                        // (up to 60s) rather than dropping — but give up eventually so a
                        // wedged printer can't stall jobs forever.
                        if (!PrintModel.TryBeginJob(60_000))
                        {
                            _log($"Print job for {fmt.Size} dropped: printer busy 60s.");
                            return;
                        }
                        try
                        {
                            _log($"Received LabelPrint job for {fmt.Size}.");
                            _printModel.PrintTo(job.EplData, fmt.PrinterName, fmt.PrintType);
                            _log($"Print job sent to {fmt.PrinterName}.");
                        }
                        catch (Exception ex)
                        {
                            _log($"Print job for {fmt.Size} failed: {ex.Message}");
                        }
                        finally
                        {
                            PrintModel.EndJob();
                        }
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(message))
            {
                _log($"Ignored message: {Truncate(message, 80)}");
            }
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";

    public ValueTask DisposeAsync()
    {
        Stop();
        return ValueTask.CompletedTask;
    }
}
