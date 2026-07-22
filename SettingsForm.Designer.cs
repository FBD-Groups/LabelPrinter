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
        lblHost = new Label();
        lblWsUrl = new Label();
        txtWsUrl = new TextBox();
        chkEnableWebSocket = new CheckBox();
        lblLanguage = new Label();
        cboLanguage = new ComboBox();
        tlpFormats = new TableLayoutPanel();
        chkRunAtStartup = new CheckBox();
        chkAllowLan = new CheckBox();
        btnSave = new Button();
        lblLog = new Label();
        txtLog = new TextBox();
        SuspendLayout();
        //
        // lblHost
        //
        lblHost.AutoSize = true;
        lblHost.Location = new Point(16, 16);
        lblHost.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblHost.Text = "本机地址: ...";
        //
        // lblWsUrl
        //
        lblWsUrl.AutoSize = true;
        lblWsUrl.Location = new Point(16, 46);
        lblWsUrl.Text = "WebSocket:";
        //
        // txtWsUrl
        //
        txtWsUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtWsUrl.Location = new Point(170, 42);
        txtWsUrl.Size = new Size(620, 23);
        //
        // chkEnableWebSocket
        //
        chkEnableWebSocket.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        chkEnableWebSocket.AutoSize = true;
        chkEnableWebSocket.Location = new Point(110, 44);
        chkEnableWebSocket.Text = "启用";
        chkEnableWebSocket.CheckedChanged += (_, _) => txtWsUrl.Enabled = chkEnableWebSocket.Checked;
        //
        // lblLanguage
        //
        lblLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(720, 16);
        lblLanguage.Text = "Language";
        //
        // cboLanguage
        //
        cboLanguage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cboLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cboLanguage.Location = new Point(800, 13);
        cboLanguage.Size = new Size(92, 23);
        cboLanguage.Items.AddRange(new object[] { "中文", "English" });
        //
        // tlpFormats
        //
        tlpFormats.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        tlpFormats.Location = new Point(16, 76);
        tlpFormats.Size = new Size(876, 140);
        tlpFormats.ColumnCount = 9;
        tlpFormats.RowCount = 1;
        tlpFormats.AutoSize = false;
        tlpFormats.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));    // default radio (fits "Default" in English)
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));    // size
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));   // call URL
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155F));   // printer
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62F));    // type
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));   // port
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));    // enabled
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));   // test
        tlpFormats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));    // spacer (absorbs extra width)
        //
        // chkRunAtStartup
        //
        chkRunAtStartup.AutoSize = true;
        chkRunAtStartup.Location = new Point(16, 260);
        chkRunAtStartup.Text = "开机自启";
        //
        // chkAllowLan — own row so English text never covers Save
        //
        chkAllowLan.AutoSize = true;
        chkAllowLan.Location = new Point(16, 286);
        chkAllowLan.Text = "允许局域网访问 (需管理员)";
        //
        // btnSave
        //
        btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSave.AutoSize = true;
        btnSave.Location = new Point(760, 254);
        btnSave.MinimumSize = new Size(120, 28);
        btnSave.Padding = new Padding(8, 0, 8, 0);
        btnSave.Text = "保存并应用";
        btnSave.Click += BtnSave_Click;
        //
        // lblLog
        //
        lblLog.AutoSize = true;
        lblLog.Location = new Point(16, 318);
        lblLog.Text = "Log:";
        //
        // txtLog
        //
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9F);
        txtLog.Location = new Point(16, 338);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.Size = new Size(876, 140);
        //
        // SettingsForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(908, 506);
        Controls.Add(txtLog);
        Controls.Add(lblLog);
        Controls.Add(btnSave);
        Controls.Add(chkAllowLan);
        Controls.Add(chkRunAtStartup);
        Controls.Add(tlpFormats);
        Controls.Add(cboLanguage);
        Controls.Add(lblLanguage);
        Controls.Add(chkEnableWebSocket);
        Controls.Add(txtWsUrl);
        Controls.Add(lblWsUrl);
        Controls.Add(lblHost);
        MinimumSize = new Size(900, 530);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Label Printer Service - 设置";
        FormClosing += SettingsForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    private Label lblHost;
    private Label lblWsUrl;
    private TextBox txtWsUrl;
    private CheckBox chkEnableWebSocket;
    private Label lblLanguage;
    private ComboBox cboLanguage;
    private TableLayoutPanel tlpFormats;
    private CheckBox chkRunAtStartup;
    private CheckBox chkAllowLan;
    private Button btnSave;
    private Label lblLog;
    private TextBox txtLog;
}
