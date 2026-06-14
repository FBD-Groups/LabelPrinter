#nullable disable
namespace LabelPrinter;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        lblPrinter = new Label();
        cboPrinter = new ComboBox();
        lblWsUrl = new Label();
        txtWsUrl = new TextBox();
        chkEnableWebSocket = new CheckBox();
        chkEnableRest = new CheckBox();
        chkUseLpt = new CheckBox();
        txtLptPort = new TextBox();
        chkRunAtStartup = new CheckBox();
        btnSave = new Button();
        btnTestEpl = new Button();
        txtLog = new TextBox();
        lblLog = new Label();
        SuspendLayout();
        // 
        // lblPrinter
        // 
        lblPrinter.AutoSize = true;
        lblPrinter.Location = new Point(16, 18);
        lblPrinter.Name = "lblPrinter";
        lblPrinter.Size = new Size(47, 15);
        lblPrinter.Text = "Printer:";
        // 
        // cboPrinter
        // 
        cboPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        cboPrinter.DropDownStyle = ComboBoxStyle.DropDownList;
        cboPrinter.Location = new Point(120, 14);
        cboPrinter.Size = new Size(392, 23);
        cboPrinter.SelectedIndexChanged += (_, _) => { if (cboPrinter.SelectedItem is string n) _config.PrinterName = n; };
        // 
        // lblWsUrl
        // 
        lblWsUrl.AutoSize = true;
        lblWsUrl.Location = new Point(16, 52);
        lblWsUrl.Name = "lblWsUrl";
        lblWsUrl.Size = new Size(95, 15);
        lblWsUrl.Text = "WebSocket URL:";
        // 
        // txtWsUrl
        // 
        txtWsUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtWsUrl.Location = new Point(120, 48);
        txtWsUrl.Size = new Size(392, 23);
        // 
        // chkEnableWebSocket
        // 
        chkEnableWebSocket.AutoSize = true;
        chkEnableWebSocket.Location = new Point(120, 82);
        chkEnableWebSocket.Text = "Enable WebSocket";
        chkEnableWebSocket.CheckedChanged += (_, _) => txtWsUrl.Enabled = chkEnableWebSocket.Checked;
        // 
        // chkEnableRest
        // 
        chkEnableRest.AutoSize = true;
        chkEnableRest.Location = new Point(280, 82);
        chkEnableRest.Text = "Enable REST";
        // 
        // chkUseLpt
        // 
        chkUseLpt.AutoSize = true;
        chkUseLpt.Location = new Point(120, 110);
        chkUseLpt.Text = "Use LPT port";
        chkUseLpt.CheckedChanged += (_, _) =>
        {
            txtLptPort.Enabled = chkUseLpt.Checked;
            cboPrinter.Enabled = !chkUseLpt.Checked;
        };
        // 
        // txtLptPort
        // 
        txtLptPort.Enabled = false;
        txtLptPort.Location = new Point(230, 108);
        txtLptPort.PlaceholderText = "LPT1";
        txtLptPort.Size = new Size(80, 23);
        // 
        // chkRunAtStartup
        // 
        chkRunAtStartup.AutoSize = true;
        chkRunAtStartup.Location = new Point(120, 138);
        chkRunAtStartup.Text = "开机自动启动（登录时）";
        // 
        // btnSave
        // 
        btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSave.Location = new Point(416, 168);
        btnSave.Size = new Size(96, 28);
        btnSave.Text = "保存并应用";
        btnSave.Click += BtnSave_Click;
        // 
        // btnTestEpl
        // 
        btnTestEpl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnTestEpl.Location = new Point(304, 168);
        btnTestEpl.Size = new Size(96, 28);
        btnTestEpl.Text = "Test EPL";
        btnTestEpl.Click += BtnTestEpl_Click;
        // 
        // lblLog
        // 
        lblLog.AutoSize = true;
        lblLog.Location = new Point(16, 208);
        lblLog.Text = "Log:";
        // 
        // txtLog
        // 
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.Location = new Point(16, 228);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(496, 160);
        // 
        // SettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(528, 406);
        Controls.Add(txtLog);
        Controls.Add(lblLog);
        Controls.Add(btnTestEpl);
        Controls.Add(btnSave);
        Controls.Add(chkRunAtStartup);
        Controls.Add(txtLptPort);
        Controls.Add(chkUseLpt);
        Controls.Add(chkEnableRest);
        Controls.Add(chkEnableWebSocket);
        Controls.Add(txtWsUrl);
        Controls.Add(lblWsUrl);
        Controls.Add(cboPrinter);
        Controls.Add(lblPrinter);
        MinimumSize = new Size(480, 380);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "ControlCode Label Printer - 设置";
        FormClosing += SettingsForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblPrinter;
    private ComboBox cboPrinter;
    private Label lblWsUrl;
    private TextBox txtWsUrl;
    private CheckBox chkEnableWebSocket;
    private CheckBox chkEnableRest;
    private CheckBox chkUseLpt;
    private TextBox txtLptPort;
    private CheckBox chkRunAtStartup;
    private Button btnSave;
    private Button btnTestEpl;
    private TextBox txtLog;
    private Label lblLog;
}
