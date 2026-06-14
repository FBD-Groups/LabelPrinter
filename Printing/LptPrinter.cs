using System.Text;

namespace LabelPrinter.Printing;

/// <summary>
/// Writes EPL directly to a parallel port (e.g. LPT1).
/// </summary>
public static class LptPrinter
{
    public static void Print(string portName, string eplData)
    {
        if (string.IsNullOrWhiteSpace(portName))
            throw new ArgumentException("LPT port name is required.", nameof(portName));

        var path = portName.StartsWith(@"\\.\", StringComparison.Ordinal)
            ? portName
            : $@"\\.\{portName.Trim()}";

        var bytes = Encoding.ASCII.GetBytes(eplData);
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
