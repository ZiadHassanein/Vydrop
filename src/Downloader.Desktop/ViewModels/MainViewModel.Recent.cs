namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private void RestoreRecentFiles()
    {
        // Older versions retained five paths; the durable queue may still know the others.
        // Keep known recent history first and only append missing completed outputs.
        var completed = Queue.Entries.Reverse().Where(entry => entry.State == QueueState.Completed)
            .OrderByDescending(entry => entry.CompletedAt);
        var files = completed.SelectMany(entry => (entry.Result?.Files ?? new())
            .Append(entry.Result?.FilePath).Distinct(StringComparer.OrdinalIgnoreCase).Reverse());
        state.Recent = SettingsRepository.NormalizeRecentFiles(state.Recent.Concat(files));
    }

    public int RecentDownloadCount => state.UnseenDownloads;
    public string HistoryAccessibleName => RecentDownloadCount == 0 ? HistoryLabel : F("Recent downloads, {0} new files", RecentDownloadCount);
    private void RefreshRecentState()
    {
        OnPropertyChanged(nameof(RecentDownloadCount));
        OnPropertyChanged(nameof(HistoryAccessibleName));
    }
}
