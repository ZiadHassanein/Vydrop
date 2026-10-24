using System.Runtime.InteropServices;

namespace YouTubeDownloader;

public static class AppPaths
{
    // These legacy directories retain existing preferences, history and verified media tools.
    public static string UserData
    {
        get
        {
#if VIFREE_PREVIEW
            // Preview lifecycle tests must never borrow the user's existing preview queue.
            string isolated = Environment.GetEnvironmentVariable("VYDROP_PREVIEW_DATA_ROOT");
            if (!string.IsNullOrWhiteSpace(isolated))
            {
                if (!Path.IsPathFullyQualified(isolated) || isolated.StartsWith(@"\\", StringComparison.Ordinal)) throw new ArgumentException("Choose a folder on this computer.");
                return Path.GetFullPath(isolated);
            }
#endif
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ReleaseChannel.IsPreview ? "ViFreePreview" : "YouTubeDownloader");
        }
    }

    public static string Downloads
    {
        get
        {
            var id = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            IntPtr pointer = IntPtr.Zero;
            try
            {
                if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out pointer) == 0)
                    return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pointer);
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    public static string InitialFolder(string saved, bool migrateLegacy)
    {
        if (string.IsNullOrWhiteSpace(saved))
            return Downloads;
        if (migrateLegacy)
            foreach (string parent in new[]
            {
                Downloads,
                Path.Combine(Path.GetPathRoot(Downloads), "Downloads")
            })
                if (string.Equals(saved.Trim().TrimEnd('\\', '/'), Path.Combine(parent, "YouTube"), StringComparison.OrdinalIgnoreCase))
                    return parent;
        return saved;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHGetKnownFolderPath(ref Guid id, uint flags, IntPtr token, out IntPtr path);
}
