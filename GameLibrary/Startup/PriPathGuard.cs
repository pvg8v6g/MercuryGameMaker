using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace GameLibrary.Startup;

/// <summary>
/// The Windows App SDK resource loader (MRM, GetDefaultPriFile) writes past its buffer when the app's .pri file path
/// is too long, and the app then dies at random on start with heap corruption (0xc0000374) and no message.
/// Seen crashing at 139 characters, never at 125 or less. This runs first thing in Main, before WinUI starts:
/// a long path is relaunched through its short 8.3 form, and if that can't help the user gets a clear message.
/// </summary>
public static class PriPathGuard
{
    #region Fields

    public const int MaxPriPathLength = 120;

    // set on the relaunched process so a short path that still doesn't fit can't relaunch forever
    private const string RelaunchVariable = "MERCURY_PRI_PATH_RELAUNCH";

    #endregion

    #region Methods

    // true when it is safe to start WinUI, false when this process must exit (it relaunched or showed the message)
    public static bool EnsureSafe(string[] args)
    {
        var exe = Environment.ProcessPath ?? string.Empty;
        var priPath = PriPath(exe);
        if (priPath.Length <= MaxPriPathLength) return true;

        if (Environment.GetEnvironmentVariable(RelaunchVariable) is null && ShortPath(exe) is { } shortExe && PriPath(shortExe).Length <= MaxPriPathLength)
        {
            var startInfo = new ProcessStartInfo(shortExe)
            {
                WorkingDirectory = Path.GetDirectoryName(shortExe),
                UseShellExecute = false
            };
            foreach (var arg in args) startInfo.ArgumentList.Add(arg);
            startInfo.Environment[RelaunchVariable] = "1";
            Process.Start(startInfo);
            return false;
        }

        _ = MessageBoxW(IntPtr.Zero,
            $"This program can't start safely from where it is installed.\n\n" +
            $"Its folder path is too long ({priPath.Length} characters, the limit is {MaxPriPathLength}):\n{Path.GetDirectoryName(exe)}\n\n" +
            "A bug in Microsoft's Windows App SDK makes WinUI apps crash at random when this path is too long.\n\n" +
            "Move the program (or the project folder it was built in) to a shorter folder, for example C:\\Games, and start it again.",
            $"{Path.GetFileNameWithoutExtension(exe)} - folder path too long", 0x10);
        return false;
    }

    // the loader looks for <exe name>.pri next to the exe
    private static string PriPath(string exe)
    {
        return Path.Combine(Path.GetDirectoryName(exe) ?? string.Empty, Path.GetFileNameWithoutExtension(exe) + ".pri");
    }

    // the 8.3 form of the path (C:\Users\NAME\DOCUME~1\...), null when the drive has 8.3 names turned off
    private static string? ShortPath(string path)
    {
        var buffer = new StringBuilder(1024);
        var length = GetShortPathNameW(path, buffer, buffer.Capacity);
        if (length == 0 || length >= buffer.Capacity) return null;

        var shortPath = buffer.ToString();
        return string.Equals(shortPath, path, StringComparison.OrdinalIgnoreCase) ? null : shortPath;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetShortPathNameW(string longPath, StringBuilder shortPath, int bufferLength);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);

    #endregion
}
