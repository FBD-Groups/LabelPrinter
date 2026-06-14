namespace LabelPrinter;

static class Program
{
    private const string MutexName = "Global\\ControlCodeLabelPrinterTray";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Label Printer 已在运行，请查看系统托盘（任务栏右下角 ^）。",
                "ControlCode Label Printer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
