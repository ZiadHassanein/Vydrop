using System.Windows;
namespace YouTubeDownloader;
public sealed partial class DesktopActions
{
    public void ShowOutputOptions(MainViewModel model)
    {
        if (owner is MainWindow main)
            main.ShowWorkspace(WorkspacePage.Subtitles, new SubtitleOptionsView { DataContext = model });
    }
    public void ShowQueue(MainViewModel model)
    {
        if (owner is not MainWindow main || model.ActiveWorkspace == WorkspacePage.QueueComposer) return;
        model.ClearQueueMessage();
        model.LibraryFilterCommand.Execute("queue");
        main.ShowWorkspace(WorkspacePage.QueueComposer, new QueueComposerView { DataContext = model });
    }
    public async Task<HashSet<string>> SelectPlaylist(PlaylistInfo playlist, HashSet<string> selected, Func<string, string> t)
    {
        if (owner is not MainWindow main || owner.DataContext is not MainViewModel model) return null;
        var view = new PlaylistSelectionView(playlist, selected, t) { DataContext = model };
        main.ShowWorkspace(WorkspacePage.Playlist, view);
        var result = await view.Selection;
        if (main.WorkspaceView == view) model.NewDownloadCommand.Execute(null);
        return result;
    }
    public void ShowBrowserIntegration(Func<string, string> t)
        => ShowBrowserIntegration(t, new BrowserSetupActions());
    internal void ShowBrowserIntegration(Func<string, string> t, IBrowserSetupActions actions, SetupBrowser? installerBrowser = null)
    {
        if (owner is not MainWindow main || owner.DataContext is not MainViewModel model) return;
        if (main.WorkspaceView is BrowserIntegrationView existing)
        {
            if (installerBrowser is { } selected) ((BrowserSetupViewModel)existing.DataContext).BeginInstallerSetup(selected);
            return;
        }
        var setup = new BrowserSetupViewModel(t, actions, model.IsArabic);
        var view = new BrowserIntegrationView { DataContext = setup };
        if (installerBrowser is { } browser)
        {
            RoutedEventHandler start = null;
            start = (_, _) => { view.Loaded -= start; setup.BeginInstallerSetup(browser); };
            view.Loaded += start;
        }
        main.ShowWorkspace(WorkspacePage.BrowserIntegration, view);
    }
}
