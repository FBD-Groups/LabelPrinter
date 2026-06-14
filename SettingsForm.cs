using System.Drawing.Printing;
using LabelPrinter.Printing;

namespace LabelPrinter;

public partial class SettingsForm : Form
{
    private AppConfig _config;
    private readonly PrintHostService _host;

    public event Action<AppConfig>? ConfigSaved;

    public SettingsForm(AppConfig config, PrintHostService host)
    {
        _config = config;
        _host = host;
        InitializeComponent();
        LoadUi();
        _host.LogMessage += AppendLog;
    }

    private void LoadUi()
    {
        foreach (string name in PrinterSettings.InstalledPrinters)
            cboPrinter.Items.Add(name);

        if (!string.IsNullOrWhiteSpace(_config.PrinterName))
        {
            var idx = cboPrinter.Items.IndexOf(_config.PrinterName);
            if (idx >= 0)
                cboPrinter.SelectedIndex = idx;
        }

        if (cboPrinter.SelectedIndex < 0 && cboPrinter.Items.Count > 0)
            cboPrinter.SelectedIndex = 0;

        txtWsUrl.Text = _config.LabelPrinterUrl;
        chkEnableWebSocket.Checked = _config.EnableWebSocket;
        chkEnableRest.Checked = _config.EnableRestEndpoint;
        chkUseLpt.Checked = _config.UseLptPrinter;
        txtLptPort.Text = _config.LptPort;
        chkRunAtStartup.Checked = _config.RunAtStartup;
        txtWsUrl.Enabled = chkEnableWebSocket.Checked;
        txtLptPort.Enabled = chkUseLpt.Checked;
        cboPrinter.Enabled = !chkUseLpt.Checked;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        ApplyUiToConfig();
        ConfigSaved?.Invoke(_config);
        AppendLog("Settings saved.");
        MessageBox.Show(this, "已保存并重新连接。", "Label Printer", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BtnTestEpl_Click(object? sender, EventArgs e)
    {
        ApplyUiToConfig();
        const string sampleEpl = """
            N
            D15
            Q200,20
            A20,20,0,4,1,1,N,"Test"
            P1
            """;

        try
        {
            new PrintModel(_config).PrintBarcode(sampleEpl);
            AppendLog("EPL test sent.");
        }
        catch (Exception ex)
        {
            AppendLog($"EPL test failed: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyUiToConfig()
    {
        if (cboPrinter.SelectedItem is string name)
            _config.PrinterName = name;
        _config.LabelPrinterUrl = txtWsUrl.Text.Trim();
        _config.EnableWebSocket = chkEnableWebSocket.Checked;
        _config.EnableRestEndpoint = chkEnableRest.Checked;
        _config.UseLptPrinter = chkUseLpt.Checked;
        _config.LptPort = string.IsNullOrWhiteSpace(txtLptPort.Text) ? "LPT1" : txtLptPort.Text.Trim();
        _config.RunAtStartup = chkRunAtStartup.Checked;
    }

    private void AppendLog(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(message));
            return;
        }

        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _host.LogMessage -= AppendLog;
        base.OnFormClosed(e);
    }
}
