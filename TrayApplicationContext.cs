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
        TryApplyStartup(_config.RunAtStartup);

        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "Label Printer Service",
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
        _trayIcon.DoubleClick += (_, _) => ShowSettings();

        UpdateTrayText();
        _trayIcon.ShowBalloonTip(
            3000,
            L.T("tray.balloon.title"),
            L.T("tray.balloon.started"),
            ToolTipIcon.Info);

        L.LanguageChanged += () =>
        {
            _trayIcon.ContextMenuStrip = BuildMenu();
            UpdateTrayText();
        };

        var timer = new System.Windows.Forms.Timer { Interval = 2000 };
        timer.Tick += (_, _) => UpdateTrayText();
        timer.Start();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(L.T("tray.menu.settings"), null, (_, _) => ShowSettings());
        menu.Items.Add(L.T("tray.menu.reconnect"), null, (_, _) => Reconnect());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L.T("tray.menu.exit"), null, (_, _) => ExitThread());
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
        try
        {
            _config = config;
            config.Save();
            TryApplyStartup(config.RunAtStartup);
            _host.Restart(_config);
            UpdateTrayText();
        }
        catch (Exception ex)
        {
            // Saving / re-listening failed (disk, port permissions, etc.). Report it
            // instead of letting the exception tear the process down.
            OnLogMessage($"Apply settings failed: {ex.Message}");
            MessageBox.Show(ex.Message, L.T("tray.balloon.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // Writing the HKCU Run key can fail (policy, locked hive). Startup registration is a
    // convenience, never a reason to crash — log and carry on.
    private void TryApplyStartup(bool enabled)
    {
        try
        {
            StartupRegistration.Apply(enabled);
        }
        catch (Exception ex)
        {
            OnLogMessage($"Startup registration failed: {ex.Message}");
        }
    }

    private void Reconnect()
    {
        _host.Restart(_config);
        _trayIcon.ShowBalloonTip(2000, L.T("tray.text"), L.T("tray.balloon.reconnected"), ToolTipIcon.Info);
    }

    private void OnLogMessage(string message) => FileLog.Write(message);

    private void UpdateTrayText()
    {
        var ws = !_config.EnableWebSocket
            ? L.T("tray.ws.disabled")
            : _host.IsWebSocketConnected ? L.T("tray.ws.on") : L.T("tray.ws.off");
        _trayIcon.Text = $"{L.T("tray.text")} | WS:{ws}";
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _host.Dispose();
        base.ExitThreadCore();
    }
}
