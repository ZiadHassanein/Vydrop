namespace YouTubeDownloader;

public enum WorkspacePage { None, QueueComposer, BrowserIntegration, Subtitles, Playlist }

public sealed partial class MainViewModel
{
    private WorkspacePage workspacePage;
    public WorkspacePage ActiveWorkspace
    {
        get => workspacePage;
        internal set
        {
            if (!SetProperty(ref workspacePage, value)) return;
            OnPropertyChanged(nameof(HasWorkspacePage));
            OnPropertyChanged(nameof(IsBrowserIntegrationPage));
            RefreshLibrary();
        }
    }
    public bool HasWorkspacePage => ActiveWorkspace != WorkspacePage.None;
    public bool IsBrowserIntegrationPage => ActiveWorkspace == WorkspacePage.BrowserIntegration;
    public string BackToQueueLabel => T(IsQueuePage ? "Back to queue" : "Back to Library");

    internal void OpenWorkspace(WorkspacePage page)
    {
        SettingsVisible = false; HistoryVisible = false; AddDownloadVisible = false;
        ActiveWorkspace = page;
    }
}
