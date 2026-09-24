using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace SceneTest;

/// <summary>
///     Backs --log-file: redirects this process's stdout/stderr to a file. On Windows this repoints the
///     OS-level std handles (via SetStdHandle) before .NET's Console lazily binds to them, so both managed
///     Console.WriteLine calls and native writes (e.g. the Vulkan validation layer's default debug
///     messenger, which prints via the C runtime, not through .NET) land in the same file. Elsewhere it
///     only redirects the managed side.
/// </summary>
internal static class LogFileRedirector
{
    public static string? GetLogFilePath(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] is "--log-file")
                return args[i + 1];

        return null;
    }

    public static void RedirectTo(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            RedirectWindows(path);
            return;
        }

        var writer = new StreamWriter(path, false) { AutoFlush = true };
        Console.SetOut(writer);
        Console.SetError(writer);
    }

    private static void RedirectWindows(string path)
    {
        const uint genericWrite = 0x40000000;
        const uint fileShareRead = 0x1;
        const uint createAlways = 2;
        const uint fileAttributeNormal = 0x80;

        var handle = CreateFileW(path, genericWrite, fileShareRead, IntPtr.Zero, createAlways, fileAttributeNormal,
            IntPtr.Zero);
        if (handle == new IntPtr(-1))
            throw new IOException($"Failed to open log file '{path}' (GetLastError={Marshal.GetLastWin32Error()})");

        SetStdHandle(StdOutputHandle, handle);
        SetStdHandle(StdErrorHandle, handle);

        var writer = new StreamWriter(new FileStream(new SafeFileHandle(handle, false), FileAccess.Write))
        {
            AutoFlush = true
        };
        Console.SetOut(writer);
        Console.SetError(writer);
    }

    private const int StdOutputHandle = -11;
    private const int StdErrorHandle = -12;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);
}
