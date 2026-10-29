using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace YouTubeDownloader;

public sealed partial class DesktopActions
{
    private readonly Window owner;
    private readonly Action<string> openFolder;
    private readonly Action<string, bool> openMedia;
    public DesktopActions(Window owner)
    {
        this.owner = owner;
    }

    internal DesktopActions(Window owner, Action<string> openFolder, Action<string, bool> openMedia) : this(owner)
        => (this.openFolder, this.openMedia) = (openFolder, openMedia);

    public void Dispatch(Action action)
    {
        if (owner.Dispatcher.HasShutdownStarted)
            return;
        if (owner.Dispatcher.CheckAccess())
            action();
        else
            owner.Dispatcher.BeginInvoke(action);
    }

    public string ClipboardText()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    public string BrowseFolder(string current)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a save folder",
            InitialDirectory = Directory.Exists(current) ? current : AppPaths.Downloads
        };
        return dialog.ShowDialog(owner) == true ? dialog.FolderName : null;
    }

    public void OpenFolder(string folder)
    {
        string path = Path.GetFullPath(folder);
        if (openFolder != null) { openFolder(path); return; }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }
    public void OpenPublicPage(string url) => Process.Start(new ProcessStartInfo(MediaLink.PublicUrl(url)) { UseShellExecute = true });
    public void OpenMedia(string file, bool reveal)
    {
        if (!File.Exists(file) || !OutputProfiles.IsMediaExtension(Path.GetExtension(file)))
            throw new IOException("File is not available.");
        if (openMedia != null) { openMedia(file, reveal); return; }
        if (!reveal)
        {
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
            return;
        }

        uint flags;
        IntPtr item;
        int result = SHParseDisplayName(Path.GetFullPath(file), IntPtr.Zero, out item, 0, out flags);
        if (result < 0)
            Marshal.ThrowExceptionForHR(result);
        try
        {
            result = SHOpenFolderAndSelectItems(item, 0, IntPtr.Zero, 0);
            if (result < 0)
                Marshal.ThrowExceptionForHR(result);
        }
        finally
        {
            Marshal.FreeCoTaskMem(item);
        }
    }

    public void OpenSocial(string kind)
    {
        string url = kind switch
        {
            "github" => "https://github.com/ZiadHassanein",
            "linkedin" => "https://www.linkedin.com/in/ziad-hassanein/",
            "email" => "mailto:ziadmuhammed501@gmail.com",
            _ => null
        };
        if (url != null)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public void OpenReleaseDownload() => Process.Start(new ProcessStartInfo(AppUpdates.Repository + "/releases") { UseShellExecute = true });
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHParseDisplayName(string path, IntPtr bindContext, out IntPtr item, uint attributes, out uint returnedAttributes);
    [DllImport("shell32.dll")]
    private static extern int SHOpenFolderAndSelectItems(IntPtr item, uint count, IntPtr children, uint flags);
}
