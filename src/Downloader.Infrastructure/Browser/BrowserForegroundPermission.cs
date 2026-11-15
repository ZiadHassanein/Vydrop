using System.Runtime.InteropServices;
using System.Diagnostics;
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
        if (pipe.IsInvalid || pipe.IsClosed || !GetNamedPipeServerProcessId(pipe, out uint pid) || pid == 0) return;
        _ = AllowSetForegroundWindow(pid);
        try
        {
            using var process = Process.GetProcessById(checked((int)pid));
            IntPtr window = process.MainWindowHandle;
            // Act in the browser host while it still owns the explicit user gesture.
            // Delegating alone can expire before the desktop has handled the request.
            PresentWindow(window, window != IntPtr.Zero && IsIconic(window),
                handle => { _ = ShowWindow(handle, 9); }, handle => { _ = SetForegroundWindow(handle); });
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception or OverflowException)
        {
            // A closing window or Windows focus refusal must not fail a download.
        }
    }

    internal static void PresentWindow(IntPtr window, bool minimized, Action<IntPtr> restore, Action<IntPtr> activate)
    {
        if (window == IntPtr.Zero) return;
        if (minimized) restore(window);
        activate(window);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(SafePipeHandle pipe, out uint serverProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);
}
