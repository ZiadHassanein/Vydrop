using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;
public sealed partial class MainViewModel
{
    private readonly Dictionary<string, (BrowserRequest Request, BrowserReceipt Receipt)> browserReceipts = new();
    public IRelayCommand QueuePauseCommand { get; private set; }
    public IRelayCommand<Guid> RefreshQueueSourceCommand { get; private set; }
    public IReadOnlyList<KeyValuePair<int, string>> ConnectionOptions => new KeyValuePair<int, string>[] { new(1, T("1 connection (default)")), new(2, T("2 connections")), new(4, T("4 connections")), new(8, T("8 connections")) };
    public IReadOnlyList<KeyValuePair<int, string>> RetryOptions => new KeyValuePair<int, string>[] { new(1, T("No retries")), new(2, T("Retry once")), new(3, T("Retry twice")), new(4, T("Retry 3 times")), new(5, T("Retry 4 times")) };
    public int DownloadConnections { get => state.Connections is 2 or 4 or 8 ? state.Connections : 1; set { if (value is not (1 or 2 or 4 or 8)) return; state.Connections = value; Save(); OnPropertyChanged(); } }
    private int lastSpeedLimitKiB = 1024;
    private bool customSpeedLimit;
    private static readonly int[] SpeedPresets = { 512, 1024, 2048, 5120, 10240 };
    public IReadOnlyList<KeyValuePair<int, string>> SpeedLimitOptions => new KeyValuePair<int, string>[]
    {
        new(512, "0.5 MiB/s"), new(1024, "1 MiB/s"), new(2048, "2 MiB/s"), new(5120, "5 MiB/s"), new(10240, "10 MiB/s"), new(-1, T("Custom"))
    };
    public int SelectedSpeedPreset
    {
        get => customSpeedLimit || !SpeedPresets.Contains(LimitedSpeedKiB) ? -1 : LimitedSpeedKiB;
        set
        {
            if (value == -1) customSpeedLimit = true;
            else if (SpeedPresets.Contains(value)) { customSpeedLimit = false; SpeedLimitKiB = value; }
            else return;
            OnPropertyChanged(); OnPropertyChanged(nameof(IsCustomSpeedLimit)); OnPropertyChanged(nameof(LimitedSpeedKiB));
        }
    }
    public bool IsCustomSpeedLimit => IsSpeedLimited && SelectedSpeedPreset == -1;
    public int SpeedLimitKiB
    {
        get => state.SpeedLimitKiB;
        set
        {
            int next = Math.Clamp(value, 0, 1024 * 1024);
            if (next == state.SpeedLimitKiB) return;
            if (state.SpeedLimitKiB > 0) lastSpeedLimitKiB = state.SpeedLimitKiB;
            state.SpeedLimitKiB = next; if (next > 0) lastSpeedLimitKiB = next;
            Save(); OnPropertyChanged(); OnPropertyChanged(nameof(IsSpeedLimited)); OnPropertyChanged(nameof(LimitedSpeedKiB)); OnPropertyChanged(nameof(SpeedLimitHint));
            OnPropertyChanged(nameof(SelectedSpeedPreset)); OnPropertyChanged(nameof(IsCustomSpeedLimit));
        }
    }
    public bool IsSpeedLimited { get => SpeedLimitKiB > 0; set { if (value != IsSpeedLimited) SpeedLimitKiB = value ? lastSpeedLimitKiB : 0; } }
    public int LimitedSpeedKiB
    {
        get => IsSpeedLimited ? SpeedLimitKiB : lastSpeedLimitKiB;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1); ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1024 * 1024);
            customSpeedLimit = true; SpeedLimitKiB = value;
            OnPropertyChanged(nameof(SelectedSpeedPreset)); OnPropertyChanged(nameof(IsCustomSpeedLimit));
        }
    }
    public int QueueAttempts { get => state.QueueAttempts; set { state.QueueAttempts = Math.Clamp(value, 1, 5); Save(); OnPropertyChanged(); } }
    public bool DirectFileAcceleration { get => state.DirectFileAcceleration; set { state.DirectFileAcceleration = value; Save(); OnPropertyChanged(); } }
    public string PauseQueueLabel => T("Pause queue");
    public string RefreshSourceLabel => T("Refresh source");
    public string DownloadLimitsLabel => T("Download speed");
    public string AdvancedDownloadLabel => T("Advanced download options");
    public string SpeedLimitValidationHint => T("Enter a whole number from 1 to 1,048,576.");
    public string DownloadLimitsHint => T("Applies to new downloads only.");
    public string ConnectionsLabel => T("Download connections");
    public string ConnectionsHint => T("Connections for the same download, when the source supports them.");
    public string SpeedLimitLabel => T("Limit download speed");
    public string SpeedValueLabel => T("Maximum speed");
    public string SpeedLimitHint => T(IsSpeedLimited ? "Choose a maximum speed." : "Uses your available speed without a limit.");
    public string AttemptsLabel => T("Automatic retries");
    public string AttemptsHint => T("Retry a download if a temporary network error causes it to fail.");
    public string RangeAccelerationLabel => T("Use extra connections for MP4 file links");
    public string RangeAccelerationHint => T("Direct file links only. Choose 2, 4 or 8 connections above.");
    private void InitializeQueueReliability()
    {
        // Retire old schedules without changing the saved jobs or their order.
        if (Queue.StartAt.HasValue || Queue.StopAt.HasValue)
        {
            try { Queue.Schedule(null, null); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            { QueueMessage = T("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); }
        }
        QueuePauseCommand = new RelayCommand(() => { automaticBrowserJobs.Clear(); Queue.Pause(); }, () => Queue.IsRunning);
        RefreshQueueSourceCommand = new RelayCommand<Guid>(id =>
        {
            try
            {
                string page = Queue.RequestSourceRefresh(id);
                if (page == null) throw new ArgumentException();
                actions.OpenPublicPage(page);
                QueueMessage = T("Play the same video and send it from the extension again. Then resume this job.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { QueueMessage = T("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); }
            catch { QueueMessage = T("The original page is unavailable. Add the public video link again."); }
            OnPropertyChanged(nameof(QueueMessage));
        });
    }
    public BrowserReceipt ReceiveBrowserRequest(BrowserRequest request)
    {
        if (request.Type == "ping") return new(true, "connected", Language: IsArabic ? "ar" : "en");
        if (browserClosing) return new(false, "busy");
        if (request.RequestId != null && browserReceipts.TryGetValue(request.RequestId, out var previous)) return previous.Request == request ? previous.Receipt : new(false, "invalid");
        try
        {
            var targets = downloads.ResolveTargets(request.Url);
            bool playlist = request.WholePlaylist || targets.VideoUrl == null && targets.PlaylistUrl != null;
            string link = downloads.Normalize(request.Url, playlist);
            BrowserReceipt receipt;
            if (request.Type == "download") receipt = AcceptBrowserDownload(request, link, playlist);
            else if (request.SourcePage != null && Queue.RefreshSource(request.SourcePage, link, request.MediaId, playlist)) receipt = new(true, "refreshed");
            else if (!IsIdle || request.Type == "enqueue")
            {
                var existing = Queue.Entries.FirstOrDefault(e => e.Url == link && (e.Options.PlaylistRequested || e.Options.Playlist != null) == playlist && e.State is QueueState.Pending or QueueState.Paused or QueueState.Interrupted);
                if (existing == null)
                {
                    var options = OutputRequest(); options.Url = link; options.Selection = null; options.QualityKey = null; options.StableOutputName = null;
                    options.PlaylistRequested = playlist;
                    options.Playlist = null;
                    options.SourcePage = request.SourcePage; options.MediaId = request.MediaId;
                    options.ThumbnailUrl = options.Uploader = null; options.Duration = 0;
                    ApplyBrowserOutput(options, request.Output);
                    Queue.Add(options, new Uri(link).Host);
                }
                ShowNotice("Added to queue", "Open the queue to review and start this download.", false, "library");
                receipt = new(true, "queued");
            }
            else { Url = link; if (playlist) WholePlaylist = true; pendingBrowserOutput = request.Output; incomingSourcePage = request.SourcePage; incomingMediaId = request.MediaId; ShowAddDownload(); if (checkedUrl == link) ApplyPendingBrowserOutput(); receipt = new(true, "opened"); }
            if (request.RequestId != null)
            {
                if (browserReceipts.Count >= 128) browserReceipts.Remove(browserReceipts.Keys.First());
                browserReceipts[request.RequestId] = (request, receipt);
            }
            return receipt;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            ShowNotice("Queue needs attention", "Queue changes could not be saved. Keep Vydrop open and check free space and folder access.", true, "folder");
            return new(false, "storage");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        { return new(false, "busy"); }
    }
    private BrowserOutputChoice pendingBrowserOutput;
    private static void ApplyBrowserOutput(DownloadOptions options, BrowserOutputChoice choice)
    {
        if (choice == null) return;
        options.Audio = choice.Kind == "audio";
        options.AudioBitrate = 0;
        options.Height = choice.Height;
        if (options.Audio) options.AudioEncoding = Enum.Parse<AudioEncoding>(choice.Format, true);
        else options.VideoContainer = Enum.Parse<VideoContainer>(choice.Format, true);
    }
    private void ApplyPendingBrowserOutput()
    {
        var choice = pendingBrowserOutput;
        if (choice == null) return;
        pendingBrowserOutput = null;
        Audio = choice.Kind == "audio";
        if (Audio) { state.AudioBitrate = 0; SelectedEncoding = Encodings.First(e => OutputProfiles.AudioExtension(e.Encoding) == choice.Format); }
        else
        {
            var format = VideoFormats.FirstOrDefault(f => OutputProfiles.VideoExtension(f.Container) == choice.Format);
            if (format != null) SelectedVideoFormat = format;
            else { ShowNotice("Format changed", "The available formats changed. Review the quality before downloading.", false, "folder"); return; }
        }
        RefreshQualities();
        if (!Audio && choice.Height > 0)
        {
            var quality = Qualities.FirstOrDefault(q => q.Height == choice.Height);
            if (quality != null) SelectedQuality = quality;
            else ShowNotice("Format changed", "The available formats changed. Review the quality before downloading.", false, "folder");
        }
    }
    private string incomingSourcePage, incomingMediaId;
}
