namespace LabelPrinter;

static class Program
{
    private const string MutexName = "Global\\ControlCodeLabelPrinterTray";

    [STAThread]
    static void Main()
    {
        // Load just for the persisted language: this sets L.Current so the "already
        // running" dialog below matches the user's chosen language even before the tray
        // context (which reloads the full config) exists.
        AppConfig.Load();

        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                L.T("app.alreadyRunning"),
                L.T("tray.balloon.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
