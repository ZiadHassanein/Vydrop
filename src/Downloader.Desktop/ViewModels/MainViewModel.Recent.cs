namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    public int RecentDownloadCount => state.UnseenDownloads;
    public string HistoryAccessibleName => RecentDownloadCount == 0 ? HistoryLabel : F("Recent downloads, {0} new files", RecentDownloadCount);
    private void RefreshRecentState()
    {
        OnPropertyChanged(nameof(RecentDownloadCount));
        OnPropertyChanged(nameof(HistoryAccessibleName));
    }
}
