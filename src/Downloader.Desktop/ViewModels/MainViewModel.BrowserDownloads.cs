namespace YouTubeDownloader;

public sealed partial class MainViewModel
{
    private readonly HashSet<Guid> automaticBrowserJobs = new();
    private Task browserDownloadTask = Task.CompletedTask;
    private bool browserDrainActive, browserClosing;
    private long observedQueueStop;

    private void ObserveQueueStop()
    {
        if (observedQueueStop == Queue.StopGeneration) return;
        observedQueueStop = Queue.StopGeneration;
        automaticBrowserJobs.Clear();
        ClearBrowserQueueNotice();
    }

    private void ClearBrowserQueueNotice()
    {
        if (noticeAnchor == "library" && noticeHeading == "Added to queue") NoticeVisible = false;
    }

    private BrowserReceipt AcceptBrowserDownload(BrowserRequest request, string link)
    {
        if (browserClosing) return new(false, "busy");
        if (request.Output == null) return new(false, "invalid");
        ObserveQueueStop();
        var options = OutputRequest();
        options.Url = link; options.Selection = null; options.QualityKey = null;
        options.Captions = null; options.AudioSource = null; options.StableOutputName = null;
        options.ThumbnailUrl = options.Uploader = null; options.Duration = 0;
        options.SourcePage = request.SourcePage; options.MediaId = request.MediaId;
        ApplyBrowserOutput(options, request.Output);
        var existing = Queue.Entries.FirstOrDefault(e => e.Url == link && e.State == QueueState.Pending &&
            e.Options.Folder == options.Folder && OutputProfiles.Key(e.Options) == OutputProfiles.Key(options));
        var awaiting = request.SourcePage == null ? null : Queue.Entries.FirstOrDefault(e =>
            e.State == QueueState.AwaitingSource && e.Options.SourcePage == request.SourcePage && e.Options.MediaId == request.MediaId);
        QueueEntry job;
        if (awaiting != null && Queue.RefreshSource(request.SourcePage, link, request.MediaId))
        {
            var refreshed = awaiting.Options; ApplyBrowserOutput(refreshed, request.Output);
            Queue.CapturePreparedOptions(refreshed); Queue.Retry(awaiting.Id); job = awaiting;
        }
        else job = existing ?? Queue.Add(options, new Uri(link).Host);
        bool canStart = IsIdle;
        automaticBrowserJobs.Add(job.Id);
        LibraryFilterCommand.Execute("queue");
        LibrarySearch = "";
        SelectedLibraryItem = QueueItems.FirstOrDefault(row => row.Id == job.Id);
        TryStartBrowserDownloads();
        if (!canStart) ShowNotice("Added to queue", "The download will start when the current operation finishes.", false, "library");
        return new(true, canStart ? "started" : "queued");
    }

    private void TryStartBrowserDownloads()
    {
        ObserveQueueStop();
        if (browserClosing || browserDrainActive || !IsIdle || LibraryActionCommand == null) return;
        automaticBrowserJobs.RemoveWhere(id => !Queue.Entries.Any(e => e.Id == id && e.State == QueueState.Pending));
        var next = Queue.Entries.FirstOrDefault(e => automaticBrowserJobs.Contains(e.Id));
        if (next == null) return;
        automaticBrowserJobs.Remove(next.Id);
        ClearBrowserQueueNotice();
        browserDrainActive = true;
        browserDownloadTask = RunBrowserDownloadAsync(next.Id);
    }

    private async Task RunBrowserDownloadAsync(Guid id)
    {
        try { await LibraryActionCommand.ExecuteAsync(id); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
        { ShowNotice("Download failed", DownloadErrorPresentation.Describe(error), true, "library"); }
        finally { browserDrainActive = false; TryStartBrowserDownloads(); }
    }
}
