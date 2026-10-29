using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace YouTubeDownloader;

internal static class BrowserForegroundPermission
{
    internal static Action<SafePipeHandle> Grant { get; set; } = GrantToServer;

    internal static void ForRequest(SafePipeHandle pipe, string type)
    {
        // Only a direct browser action may foreground the receiving desktop.
        // Background health checks and format inspection must remain passive.
        if (type is "open-link" or "enqueue" or "download") Grant(pipe);
    }

    private static void GrantToServer(SafePipeHandle pipe)
    {
        if (!pipe.IsInvalid && !pipe.IsClosed && GetNamedPipeServerProcessId(pipe, out uint pid) && pid != 0)
            _ = AllowSetForegroundWindow(pid);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);
}
