using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private string librarySearch = "", libraryFilter = "all";
    private QueueItemView selectedLibraryItem;
    private bool addDownloadVisible;
    private bool historyVisible;
    private RecentDownloadsViewModel history;
    public RecentDownloadsViewModel History { get => history; private set => SetProperty(ref history, value); }
    public bool HistoryVisible { get => historyVisible; private set { if (SetProperty(ref historyVisible, value)) RefreshLibrary(); } }
    public bool IsAddDownloadPage => AddDownloadVisible && !SettingsVisible;
    public bool LibraryVisible => !AddDownloadVisible && !SettingsVisible && !HistoryVisible;
    private void ShowHistory()
    {
        AddDownloadVisible = false; SettingsVisible = false;
        RefreshHistory(); HistoryVisible = true;
        state.UnseenDownloads = 0; RefreshRecentBadge(); Save();
    }
    private void RefreshHistory() => History = new RecentDownloadsViewModel(state.Recent, T, path => actions.OpenMedia(path, true));
    private readonly CancellationTokenSource libraryLifetime = new();
    private readonly SemaphoreSlim artworkSlots = new(3);
    public ICollectionView LibraryItems { get; private set; }
    public QueueItemView SelectedLibraryItem { get => selectedLibraryItem; set { if (SetProperty(ref selectedLibraryItem, value)) { OnPropertyChanged(nameof(HasLibrarySelection)); LibraryActionCommand?.NotifyCanExecuteChanged(); LibraryCancelCommand?.NotifyCanExecuteChanged(); OpenLibraryFolderCommand?.NotifyCanExecuteChanged(); } } }
    public bool HasLibrarySelection => SelectedLibraryItem != null;
    public bool LibraryEmpty => LibraryItems?.IsEmpty != false;
    public bool HasLibrarySearch => !string.IsNullOrWhiteSpace(LibrarySearch);
    public string LibrarySearch { get => librarySearch; set { if (SetProperty(ref librarySearch, value ?? "")) RefreshLibrary(); } }
    public bool AddDownloadVisible { get => addDownloadVisible; private set { if (SetProperty(ref addDownloadVisible, value)) RefreshLibrary(); } }
    public bool IsLibraryAll => libraryFilter == "all" && LibraryVisible;
    public bool IsLibraryQueue => libraryFilter == "queue" && LibraryVisible;
    public bool IsLibraryActive => libraryFilter == "active";
    public bool IsLibraryPaused => libraryFilter == "paused";
    public bool IsLibraryComplete => libraryFilter == "complete";
    public bool IsLibraryVideo => libraryFilter == "video" && LibraryVisible;
    public bool IsLibraryAudio => libraryFilter == "audio" && LibraryVisible;
    public int LibraryCount => QueueItems.Count;
    public int WaitingCount => QueueItems.Count(e => !e.IsComplete);
    public int ActiveCount => QueueItems.Count(e => e.IsActive);
    public int PausedCount => QueueItems.Count(e => e.Entry.State is QueueState.Paused or QueueState.Interrupted);
    public int CompletedCount => QueueItems.Count(e => e.IsComplete);
    public int VideoCount => QueueItems.Count(e => !e.IsAudio);
    public int AudioCount => QueueItems.Count(e => e.IsAudio);
    public string DetailsLabel => T("Details");
    public string LibraryHeading => T("Your downloads");
    public string LibraryLabel => T("Library");
    public string HistoryNavigationLabel => T("History");
    public string MediaLabel => T("Media");
    public string VideosLabel => T("Videos");
    public string AudioLibraryLabel => T("Audio");
    public string AddDownloadLabel => T("Add download");
    public string LibrarySearchLabel => T("Search by title or source");
    public string AllFilterLabel => T("All");
    public string ActiveFilterLabel => T("Downloading");
    public string PausedFilterLabel => T("Paused");
    public string CompletedFilterLabel => T("Completed");
    public string QueueNavigationLabel => T("Queue");
    public string EmptyLibraryHeading => T(HasLibrarySearch ? "No matching downloads" : libraryFilter == "all" ? "Make room for your next favorite" : "Nothing here yet");
    public string EmptyLibraryDetail => T(HasLibrarySearch ? "Try a different title or website." : "Add a video, audio track or playlist. Your downloads stay saved here.");
    public string SelectDownloadLabel => T("Select a download to see its details.");
    public string SavedLocallyLabel => T("Saved locally");
    public string SaveFolderLabel => T("Save folder");
    public string OpenSaveFolderLabel => T("Open save folder");
    public string TransferSpeedLabel => T("Speed");
    public string TimeRemainingLabel => T("Time left");
    public string ManageQueueLabel => T("Manage queue");
    public string LibraryFooterLabel
    {
        get
        {
            int waiting = QueueItems.Count(e => e.Entry.State == QueueState.Pending);
            return ActiveCount > 0 && waiting > 0 ? F("{0} downloading · {1} waiting", ActiveCount, waiting) :
                ActiveCount > 0 ? F("{0} downloading", ActiveCount) : waiting > 0 ? F("{0} waiting", waiting) : "";
        }
    }
    public IRelayCommand<string> LibraryFilterCommand { get; private set; }
    public IRelayCommand CloseAddDownloadCommand { get; private set; }
    public IAsyncRelayCommand<Guid> LibraryActionCommand { get; private set; }
    public string MoreActionsLabel => T("More actions");
    public IRelayCommand<Guid> LibraryCancelCommand { get; private set; }
    public IRelayCommand OpenLibraryFolderCommand { get; private set; }

    private void InitializeLibrary()
    {
        LibraryItems = new ListCollectionView(QueueItems);
        LibraryItems.Filter = item => item is QueueItemView row && LibraryMatches(row);
        LibraryFilterCommand = new RelayCommand<string>(filter =>
        {
            AddDownloadVisible = false; SettingsVisible = false; HistoryVisible = false;
            libraryFilter = filter is "queue" or "active" or "paused" or "complete" or "video" or "audio" ? filter : "all";
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
            try { actions.OpenFolder(SelectedLibraryItem.SaveFolder); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { ShowNotice("Folder unavailable", "This download folder is no longer available.", true, "library"); }
        }, () => SelectedLibraryItem != null && Directory.Exists(SelectedLibraryItem.SaveFolder));
        RefreshLibrary();
    }
    private bool LibraryMatches(QueueItemView row)
    {
        bool stateMatches = libraryFilter switch
        {
            "queue" => !row.IsComplete, "active" => row.IsActive,
            "paused" => row.Entry.State is QueueState.Paused or QueueState.Interrupted,
            "complete" => row.IsComplete, "video" => !row.IsAudio, "audio" => row.IsAudio, _ => true
        };
        return stateMatches && (string.IsNullOrWhiteSpace(librarySearch) || (row.Title + " " + row.SourceName + " " + row.Url).Contains(librarySearch.Trim(), StringComparison.CurrentCultureIgnoreCase));
    }
    private void RefreshLibrary()
    {
        if (LibraryItems == null) return;
        LibraryItems.Refresh();
        foreach (var row in QueueItems) { row.Refresh(T, Queue.IsRunning); EnsureLibraryArtwork(row); }
        if (SelectedLibraryItem == null || !QueueItems.Contains(SelectedLibraryItem)) SelectedLibraryItem = LibraryItems.Cast<QueueItemView>().FirstOrDefault();
        foreach (string name in new[] { nameof(IsAddDownloadPage), nameof(LibraryItems), nameof(LibraryEmpty), nameof(HasLibrarySearch), nameof(IsLibraryAll), nameof(IsLibraryQueue), nameof(IsLibraryActive), nameof(IsLibraryPaused), nameof(IsLibraryComplete), nameof(IsLibraryVideo), nameof(IsLibraryAudio), nameof(LibraryCount), nameof(WaitingCount), nameof(ActiveCount), nameof(PausedCount), nameof(CompletedCount), nameof(VideoCount), nameof(AudioCount), nameof(AllFilterLabel), nameof(ActiveFilterLabel), nameof(PausedFilterLabel), nameof(CompletedFilterLabel), nameof(QueueNavigationLabel), nameof(EmptyLibraryHeading), nameof(EmptyLibraryDetail), nameof(LibraryFooterLabel) }) OnPropertyChanged(name);
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
        SettingsVisible = false; HistoryVisible = false;
        AddDownloadVisible = true;
    }
}
