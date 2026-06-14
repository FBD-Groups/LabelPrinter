using LabelPrinter.Printing;
using LabelPrinter.Services;

namespace LabelPrinter;

public sealed class PrintHostService : IDisposable
{
    private AppConfig _config = null!;
    private PrintModel? _printModel;
    private RestPrintListener? _restListener;
    private WebSocketPrintListener? _webSocketListener;

    public event Action<string>? LogMessage;

    public bool IsWebSocketConnected => _webSocketListener?.IsConnected == true;

    public void Start(AppConfig config)
    {
        Stop();
        _config = config;
        _printModel = new PrintModel(_config);
        void Log(string msg) => LogMessage?.Invoke(msg);

        if (_config.EnableRestEndpoint)
        {
            _restListener = new RestPrintListener(_config, _printModel, Log);
            _restListener.Start();
        }

        if (_config.EnableWebSocket)
        {
            _webSocketListener = new WebSocketPrintListener(_config, _printModel, Log);
            _webSocketListener.Start();
        }

        LogMessage?.Invoke($"Running. Printer={(_config.UseLptPrinter ? _config.LptPort : _config.PrinterName)}");
    }

    public void Restart(AppConfig config) => Start(config);

    public void Stop()
    {
        if (_webSocketListener != null)
            _webSocketListener.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _webSocketListener = null;
        _restListener?.Dispose();
        _restListener = null;
        _printModel = null;
    }

    public void Dispose() => Stop();
}
