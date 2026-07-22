namespace LabelPrinter;

public enum AppLanguage
{
    Zh,
    En
}

/// <summary>
/// Tiny in-memory string table for the tray/settings UI. No .resx/satellite assemblies —
/// switching languages just updates <see cref="Current"/> and re-reads control .Text from
/// <see cref="T"/>, so it takes effect immediately without restarting the app.
/// </summary>
public static class L
{
    public static AppLanguage Current { get; private set; } = AppLanguage.Zh;

    public static event Action? LanguageChanged;

    public static void SetLanguage(AppLanguage language)
    {
        if (Current == language)
            return;
        Current = language;
        LanguageChanged?.Invoke();
    }

    public static AppLanguage Parse(string? code) =>
        string.Equals(code, "en", StringComparison.OrdinalIgnoreCase) ? AppLanguage.En : AppLanguage.Zh;

    public static string Code(AppLanguage language) => language == AppLanguage.En ? "en" : "zh";

    private static readonly Dictionary<string, (string Zh, string En)> Map = new()
    {
        ["title"] = ("ControlCode Label Printer - 设置", "ControlCode Label Printer - Settings"),
        ["host"] = ("本机地址", "Local address"),
        ["websocket"] = ("WebSocket:", "WebSocket:"),
        ["enable"] = ("启用", "Enable"),
        ["col.default"] = ("默认", "Default"),
        ["col.size"] = ("尺寸", "Size"),
        ["col.url"] = ("调用链接", "Call URL"),
        ["col.printer"] = ("打印机", "Printer"),
        ["col.type"] = ("类型", "Type"),
        ["col.port"] = ("端口", "Port"),
        ["col.enabled"] = ("启用", "Enabled"),
        ["col.test"] = ("", ""),
        ["btn.test"] = ("测试", "Test"),
        ["btn.testing"] = ("打印中...", "Printing..."),
        ["type.text"] = ("文本", "Text"),
        ["chk.runAtStartup"] = ("开机自启", "Start with Windows"),
        ["chk.allowLan"] = ("允许局域网访问 (需管理员)", "Allow LAN access (admin required)"),
        ["btn.save"] = ("保存并应用", "Save && Apply"),
        ["language"] = ("Language", "Language"),
        ["log.label"] = ("Log:", "Log:"),

        ["tray.text"] = ("Label Printer", "Label Printer"),
        ["tray.ws.on"] = ("已连接", "connected"),
        ["tray.ws.off"] = ("未连接", "disconnected"),
        ["tray.ws.disabled"] = ("off", "off"),
        ["tray.menu.settings"] = ("设置...", "Settings..."),
        ["tray.menu.reconnect"] = ("重新连接", "Reconnect"),
        ["tray.menu.exit"] = ("退出", "Exit"),
        ["tray.balloon.title"] = ("ControlCode Label Printer", "ControlCode Label Printer"),
        ["tray.balloon.started"] = ("已在系统托盘运行。双击图标可打开设置。", "Running in the system tray. Double-click the icon to open settings."),
        ["tray.balloon.reconnected"] = ("已重新连接。", "Reconnected."),

        ["app.alreadyRunning"] = ("Label Printer 已在运行，请查看系统托盘（任务栏右下角 ^）。", "Label Printer is already running — check the system tray (bottom-right corner ^)."),
    };

    public static string T(string key) =>
        Map.TryGetValue(key, out var value) ? (Current == AppLanguage.En ? value.En : value.Zh) : key;
}
