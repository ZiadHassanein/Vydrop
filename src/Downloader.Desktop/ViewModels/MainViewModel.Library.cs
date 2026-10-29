using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private string librarySearch = "", queueSearch = "", libraryFilter = "all";
    private QueueItemView selectedLibraryItem;
    private bool addDownloadVisible;
    private bool historyVisible;
    private RecentDownloadsViewModel history;
    public RecentDownloadsViewModel History { get => history; private set => SetProperty(ref history, value); }
    public bool HistoryVisible { get => historyVisible; private set { if (SetProperty(ref historyVisible, value)) RefreshLibrary(); } }
    public bool IsAddDownloadPage => AddDownloadVisible && !SettingsVisible || ActiveWorkspace is WorkspacePage.Subtitles or WorkspacePage.Playlist;
    public bool LibraryVisible => !HasWorkspacePage && !AddDownloadVisible && !SettingsVisible && !HistoryVisible;
    private void ShowHistory()
    {
        ActiveWorkspace = WorkspacePage.None;
        AddDownloadVisible = false; SettingsVisible = false;
        RefreshHistory(); HistoryVisible = true;
        state.UnseenDownloads = 0; RefreshRecentState(); Save();
    }
    private void RefreshHistory() => History = new RecentDownloadsViewModel(state.Recent, T, path => actions.OpenMedia(path, true));
    private readonly CancellationTokenSource libraryLifetime = new();
    private readonly SemaphoreSlim artworkSlots = new(3);
    public ICollectionView LibraryItems { get; private set; }
    public QueueItemView SelectedLibraryItem { get => selectedLibraryItem; set { if (SetProperty(ref selectedLibraryItem, value)) { OnPropertyChanged(nameof(HasLibrarySelection)); LibraryActionCommand?.NotifyCanExecuteChanged(); LibraryCancelCommand?.NotifyCanExecuteChanged(); OpenLibraryFolderCommand?.NotifyCanExecuteChanged(); } } }
    public bool HasLibrarySelection => SelectedLibraryItem != null;
    public bool LibraryEmpty => LibraryItems?.IsEmpty != false;
    public bool HasLibrarySearch => !string.IsNullOrWhiteSpace(LibrarySearch);
    public string LibrarySearch
    {
        get => IsQueuePage ? queueSearch : librarySearch;
        set
        {
            bool changed = IsQueuePage ? SetProperty(ref queueSearch, value ?? "", nameof(LibrarySearch)) : SetProperty(ref librarySearch, value ?? "", nameof(LibrarySearch));
            if (changed) RefreshLibrary();
        }
    }
    public bool AddDownloadVisible { get => addDownloadVisible; private set { if (SetProperty(ref addDownloadVisible, value)) RefreshLibrary(); } }
    public bool IsLibraryPage => IsSavedLibrary && LibraryVisible;
    public bool IsQueuePage => libraryFilter is "queue" or "active" or "pending" or "paused";
    public bool IsSavedLibrary => !IsQueuePage;
    public bool IsLibraryQueue => IsQueuePage && LibraryVisible || ActiveWorkspace == WorkspacePage.QueueComposer;
    public bool IsScopeAll => libraryFilter is "all" or "queue";
    public bool IsLibraryActive => libraryFilter == "active";
    public bool IsLibraryPaused => libraryFilter == "paused";
    public bool IsLibraryPending => libraryFilter == "pending";
    public bool IsLibraryVideo => libraryFilter == "video" && LibraryVisible;
    public bool IsLibraryAudio => libraryFilter == "audio" && LibraryVisible;
    public int LibraryCount => QueueItems.Count(e => e.IsComplete);
    public int WaitingCount => QueueItems.Count(e => !e.IsComplete);
    public int ActiveCount => QueueItems.Count(e => e.IsActive);
    public int PausedCount => QueueItems.Count(e => e.Entry.State is QueueState.Paused or QueueState.Interrupted);
    public int PendingCount => QueueItems.Count(e => e.Entry.State == QueueState.Pending);
    public int VideoCount => QueueItems.Count(e => e.IsComplete && !e.IsAudio);
    public int AudioCount => QueueItems.Count(e => e.IsComplete && e.IsAudio);
    public string DetailsLabel => T("Details");
    public string LibraryHeading => T(IsQueuePage ? "Download queue" : libraryFilter == "video" ? "Videos" : libraryFilter == "audio" ? "Audio" : "Library");
    public bool IsSettingsPage => SettingsVisible || IsBrowserIntegrationPage;
    public string LibraryLabel => T("Library");
    public string HistoryNavigationLabel => T("History");
    public string VideosLabel => T("Videos");
    public string AudioLibraryLabel => T("Audio");
    public string AddDownloadLabel => T("Add download");
    public string LibrarySearchLabel => T(IsQueuePage ? "Search queued downloads" : "Search completed downloads");
    public string AllFilterLabel => T("All");
    public string ActiveFilterLabel => T("Downloading");
    public string PausedFilterLabel => T("Paused");
    public string WaitingFilterLabel => T("Waiting");
    public string QueueNavigationLabel => T("Queue");
    public string EmptyLibraryHeading => T(HasLibrarySearch ? "No matching downloads" : IsQueuePage ? IsScopeAll ? "Your queue is clear" : "No downloads with this status" : libraryFilter == "video" ? "No completed videos" : libraryFilter == "audio" ? "No completed audio" : "Your library is empty");
    public string EmptyLibraryDetail => T(HasLibrarySearch ? "Try a different title or website." : IsQueuePage ? IsScopeAll ? "Add a download to start. Completed files appear in Library." : "Downloads with this status will appear here." : "Completed downloads appear here. Start a download to build your library.");
    public string SelectDownloadLabel => T("Select a download to see its details.");
    public string SavedLocallyLabel => T("Saved locally");
    public string SaveFolderLabel => T("Save folder");
    public string OpenSaveFolderLabel => T("Open save folder");
    public string TransferSpeedLabel => T("Speed");
    public string TimeRemainingLabel => T("Time left");
    public string LibraryFooterLabel
    {
        get
        {
            if (!IsQueuePage)
            {
                int completed = LibraryItems?.Cast<QueueItemView>().Count() ?? 0;
                return completed > 0 ? F("{0} completed", completed) : "";
            }
            int waiting = PendingCount;
            return ActiveCount > 0 && waiting > 0 ? F("{0} downloading · {1} waiting", ActiveCount, waiting) :
                ActiveCount > 0 ? F("{0} downloading", ActiveCount) : waiting > 0 ? F("{0} waiting", waiting) : WaitingCount > 0 ? F("{0} unfinished", WaitingCount) : "";
        }
    }
    public IRelayCommand<string> LibraryFilterCommand { get; private set; }
    public IRelayCommand CloseAddDownloadCommand { get; private set; }
    public IAsyncRelayCommand<Guid> LibraryActionCommand { get; private set; }
    public IRelayCommand<Guid> LibraryCancelCommand { get; private set; }
    public IRelayCommand OpenLibraryFolderCommand { get; private set; }

    private void InitializeLibrary()
    {
        LibraryItems = new ListCollectionView(QueueItems);
        LibraryItems.Filter = item => item is QueueItemView row && LibraryMatches(row);
        LibraryFilterCommand = new RelayCommand<string>(filter =>
        {
            bool previousScope = IsQueuePage;
            ActiveWorkspace = WorkspacePage.None;
            AddDownloadVisible = false; SettingsVisible = false; HistoryVisible = false;
            libraryFilter = filter is "queue" or "active" or "pending" or "paused" or "video" or "audio" ? filter : "all";
            if (previousScope != IsQueuePage) OnPropertyChanged(nameof(LibrarySearch));
            RefreshLibrary();
        });
        CloseAddDownloadCommand = new RelayCommand(() => AddDownloadVisible = false);
        LibraryActionCommand = new AsyncRelayCommand<Guid>(async id =>
        {
            var row = QueueItems.FirstOrDefault(item => item.Id == id);
            if (row == null) return;
            SelectedLibraryItem = row;
            if (row.IsActive) { automaticBrowserJobs.Clear(); Queue.Pause(); return; }
            if (row.IsComplete)
            {
                try { actions.OpenMedia(row.OutputFile, false); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { ShowNotice("File is not available", "The file may have been moved or deleted. Check its save folder.", true, "library"); RefreshLibrary(); }
                return;
            }
            if (IsBusy) return;
            if (row.Entry.State != QueueState.Pending && !Queue.Retry(id)) return;
            await Queue.RunJobAsync(id, async (request, progress, token) =>
            {
                var result = await downloads.DownloadQueueItemAsync(request, progress, Queue.CapturePreparedOptions, token);
                actions.Dispatch(() => RememberFiles(result));
                return result;
            });
            RefreshLibrary();
        }, id => QueueItems.FirstOrDefault(item => item.Id == id) is { } row &&
            (row.IsActive || row.IsComplete && !row.MissingFile || IsIdle && row.Entry.State is QueueState.Pending or QueueState.Paused or QueueState.Interrupted or QueueState.Failed or QueueState.Cancelled), AsyncRelayCommandOptions.AllowConcurrentExecutions);
        LibraryCancelCommand = new RelayCommand<Guid>(id =>
        {
            if (QueueItems.FirstOrDefault(item => item.Id == id)?.IsActive == true) CancelCommand.Execute(null);
        }, id => QueueItems.FirstOrDefault(item => item.Id == id)?.IsActive == true);
        OpenLibraryFolderCommand = new RelayCommand(() =>
        {
            try { OpenDownloadLocation(SelectedLibraryItem); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Runtime.InteropServices.ExternalException) { ShowNotice("Folder unavailable", "This download folder is no longer available.", true, "library"); }
        }, () => SelectedLibraryItem?.CanOpenFolder == true);
        RefreshLibrary();
    }
    private void OpenDownloadLocation(QueueItemView item)
    {
        if (item.HasLocalFile) actions.OpenMedia(item.OutputFile, true);
        else actions.OpenFolder(item.FolderToOpen);
    }
    private bool LibraryMatches(QueueItemView row)
    {
        bool stateMatches = libraryFilter switch
        {
            "queue" => !row.IsComplete, "active" => row.IsActive,
            "pending" => row.Entry.State == QueueState.Pending,
            "paused" => row.Entry.State is QueueState.Paused or QueueState.Interrupted,
            "video" => row.IsComplete && !row.IsAudio, "audio" => row.IsComplete && row.IsAudio, _ => row.IsComplete
        };
        return stateMatches && (string.IsNullOrWhiteSpace(LibrarySearch) || (row.Title + " " + row.SourceName + " " + row.Url).Contains(LibrarySearch.Trim(), StringComparison.CurrentCultureIgnoreCase));
    }
    private void RefreshLibrary()
    {
        if (LibraryItems == null) return;
        // Progress, language and palette updates do not replace the list's membership.
        // Avoid resetting a virtualized ItemsControl while it is generating its rows.
        if (!LibraryItems.Cast<QueueItemView>().SequenceEqual(QueueItems.Where(LibraryMatches))) LibraryItems.Refresh();
        foreach (var row in QueueItems) { row.Refresh(T, Queue.IsRunning); EnsureLibraryArtwork(row); }
        if (SelectedLibraryItem == null || !LibraryItems.Contains(SelectedLibraryItem)) SelectedLibraryItem = LibraryItems.Cast<QueueItemView>().FirstOrDefault();
        foreach (string name in new[] { nameof(IsAddDownloadPage), nameof(BackToQueueLabel), nameof(LibraryEmpty), nameof(HasLibrarySearch), nameof(IsQueuePage), nameof(IsSavedLibrary), nameof(IsScopeAll), nameof(IsLibraryPage), nameof(IsLibraryQueue), nameof(IsLibraryActive), nameof(IsLibraryPending), nameof(IsLibraryPaused), nameof(IsLibraryVideo), nameof(IsLibraryAudio), nameof(LibraryCount), nameof(WaitingCount), nameof(ActiveCount), nameof(PendingCount), nameof(PausedCount), nameof(VideoCount), nameof(AudioCount), nameof(AllFilterLabel), nameof(ActiveFilterLabel), nameof(PausedFilterLabel), nameof(WaitingFilterLabel), nameof(LibraryHeading), nameof(IsSettingsPage), nameof(LibrarySearchLabel), nameof(QueueNavigationLabel), nameof(EmptyLibraryHeading), nameof(EmptyLibraryDetail), nameof(LibraryFooterLabel) }) OnPropertyChanged(name);
        LibraryActionCommand?.NotifyCanExecuteChanged(); LibraryCancelCommand?.NotifyCanExecuteChanged(); OpenLibraryFolderCommand?.NotifyCanExecuteChanged();
    }
    private void EnsureLibraryArtwork(QueueItemView row)
    {
        string address = row.Entry.Options.ThumbnailUrl;
        if (string.IsNullOrWhiteSpace(address) || address == row.ArtworkAddress || libraryLifetime.IsCancellationRequested) return;
        row.ArtworkAddress = address;
        _ = LoadLibraryArtworkAsync(row, address, libraryLifetime.Token);
    }
    private async Task LoadLibraryArtworkAsync(QueueItemView row, string address, CancellationToken token)
    {
        bool entered = false;
        try
        {
            await artworkSlots.WaitAsync(token); entered = true;
            var image = await thumbnailLoader.LoadAsync(address, token);
            if (!token.IsCancellationRequested) actions.Dispatch(() => { if (row.ArtworkAddress == address) { row.Artwork = image; row.Refresh(T, Queue.IsRunning); } });
        }
        catch (Exception ex) when (ex is OperationCanceledException or System.Net.Http.HttpRequestException or IOException or NotSupportedException or ArgumentException or FormatException) { }
        finally { if (entered) artworkSlots.Release(); }
    }
    private void ShowAddDownload()
    {
        ActiveWorkspace = WorkspacePage.None;
        SettingsVisible = false; HistoryVisible = false;
        AddDownloadVisible = true;
    }
}
