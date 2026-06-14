using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace LabelPrinter;

public sealed class AppConfig
{
    public string LabelPrinterUrl { get; set; } = "ws://localhost:2012/websocket";
    public string PrinterName { get; set; } = "";
    public string PrinterAlias { get; set; } = "";
    public bool UseLptPrinter { get; set; }
    public string LptPort { get; set; } = "LPT1";
    public string RestListenPrefix { get; set; } = "http://localhost:8721/";
    public bool EnableRestEndpoint { get; set; } = true;
    public bool EnableWebSocket { get; set; } = true;
    public int ReconnectDelaySeconds { get; set; } = 5;
    public int WebSocketConnectTimeoutSeconds { get; set; } = 10;

    public static AppConfig Load()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        var root = builder.Build();
        var config = new AppConfig();
        root.GetSection("LabelPrinter").Bind(config);
        return config;
    }

    public string ResolvePrinterName(string? aliasFromMessage)
    {
        if (!string.IsNullOrWhiteSpace(aliasFromMessage)
            && !string.IsNullOrWhiteSpace(PrinterAlias)
            && string.Equals(aliasFromMessage, PrinterAlias, StringComparison.OrdinalIgnoreCase))
        {
            return PrinterName;
        }

        return PrinterName;
    }

    public void Save()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        var root = new Dictionary<string, object>
        {
            ["LabelPrinter"] = new Dictionary<string, object?>
            {
                ["LabelPrinterUrl"] = LabelPrinterUrl,
                ["PrinterName"] = PrinterName,
                ["PrinterAlias"] = PrinterAlias,
                ["UseLptPrinter"] = UseLptPrinter,
                ["LptPort"] = LptPort,
                ["RestListenPrefix"] = RestListenPrefix,
                ["EnableRestEndpoint"] = EnableRestEndpoint,
                ["EnableWebSocket"] = EnableWebSocket,
                ["ReconnectDelaySeconds"] = ReconnectDelaySeconds,
                ["WebSocketConnectTimeoutSeconds"] = WebSocketConnectTimeoutSeconds,
                ["RunAtStartup"] = RunAtStartup
            }
        };

        var json = JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public bool RunAtStartup { get; set; }
}
