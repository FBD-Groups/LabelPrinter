using Microsoft.Win32;

namespace LabelPrinter;

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "ControlCodeLabelPrinter";

    public static void Apply(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Cannot open Run registry key.");

        if (enabled)
        {
            var exe = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "LabelPrinter.exe");
            key.SetValue(ValueName, $"\"{exe}\"");
        }
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
