namespace LabelPrinter;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly PrintHostService _host = new();
    private AppConfig _config;
    private SettingsForm? _settingsForm;

    public TrayApplicationContext()
    {
        _config = AppConfig.Load();

        _host.LogMessage += OnLogMessage;
        _host.Start(_config);
        StartupRegistration.Apply(_config.RunAtStartup);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ControlCode Label Printer",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        UpdateTrayText();
        _trayIcon.ShowBalloonTip(
            3000,
            "ControlCode Label Printer",
            "已在系统托盘运行。双击图标可打开设置。",
            ToolTipIcon.Info);

        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (_, _) => UpdateTrayText();
        timer.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("设置...", null, (_, _) => ShowSettings());
        menu.Items.Add("重新连接", null, (_, _) => Reconnect());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());
        return menu;
    }

    private void ShowSettings()
    {
        if (_settingsForm == null || _settingsForm.IsDisposed)
        {
            _settingsForm = new SettingsForm(_config, _host);
            _settingsForm.ConfigSaved += OnConfigSaved;
            _settingsForm.FormClosed += (_, _) => _settingsForm = null;
        }

        _settingsForm.Show();
        _settingsForm.BringToFront();
        _settingsForm.WindowState = FormWindowState.Normal;
        _settingsForm.Activate();
    }

    private void OnConfigSaved(AppConfig config)
    {
        _config = config;
        config.Save();
        StartupRegistration.Apply(config.RunAtStartup);
        _host.Restart(_config);
        UpdateTrayText();
    }

    private void Reconnect()
    {
        _host.Restart(_config);
        _trayIcon.ShowBalloonTip(2000, "Label Printer", "已重新连接。", ToolTipIcon.Info);
    }

    private void OnLogMessage(string message)
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            File.AppendAllText(
                Path.Combine(logDir, "labelprinter.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore log file errors
        }
    }

    private void UpdateTrayText()
    {
        var ws = !_config.EnableWebSocket
            ? "WS:off"
            : _host.IsWebSocketConnected ? "WS:已连接" : "WS:未连接";
        _trayIcon.Text = $"Label Printer | {ws}";
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _host.Dispose();
        base.ExitThreadCore();
    }
}
