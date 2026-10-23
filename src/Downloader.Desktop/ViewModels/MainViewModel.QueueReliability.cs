using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.Windows.Threading;

namespace YouTubeDownloader;
public sealed partial class MainViewModel
{
    private DispatcherTimer queueClock;
    private readonly Dictionary<string, (BrowserRequest Request, BrowserReceipt Receipt)> browserReceipts = new();
    public IRelayCommand QueuePauseCommand { get; private set; }
    public IRelayCommand<Guid> MoveQueueUpCommand { get; private set; }
    public IRelayCommand<Guid> MoveQueueDownCommand { get; private set; }
    public IRelayCommand<Guid> RefreshQueueSourceCommand { get; private set; }
    public IRelayCommand ScheduleQueueCommand { get; private set; }
    public IRelayCommand ClearScheduleCommand { get; private set; }
    public IReadOnlyList<KeyValuePair<int, string>> ConnectionOptions => new KeyValuePair<int, string>[] { new(1, T("1 part (default)")), new(2, T("2 parts")), new(4, T("4 parts")) };
    public IReadOnlyList<KeyValuePair<int, string>> RetryOptions => new KeyValuePair<int, string>[] { new(1, T("No retries")), new(2, T("Retry once")), new(3, T("Retry twice")), new(4, T("Retry 3 times")), new(5, T("Retry 4 times")) };
    public int DownloadConnections { get => state.Connections is 2 or 4 ? state.Connections : 1; set { if (value is not (1 or 2 or 4)) return; state.Connections = value; Save(); OnPropertyChanged(); } }
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
    public DateTime? ScheduledDate { get; set; } = DateTime.Today;
    public string ScheduledTime { get; set; } = DateTime.Now.AddHours(1).ToString("HH:mm", CultureInfo.InvariantCulture);
    public string ScheduledStopTime { get; set; } = "";
    public string ScheduleSummary => Queue.StartAt.HasValue ? F("Scheduled for {0}", Queue.StartAt.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)) : T("No schedule. Vydrop must remain open for scheduled downloads.");
    public string PauseQueueLabel => T("Pause queue");
    public string MoveUpLabel => T("Move up");
    public string MoveDownLabel => T("Move down");
    public string RefreshSourceLabel => T("Refresh source");
    public string DownloadLimitsLabel => T("Download preferences");
    public string SpeedLimitValidationHint => T("Enter a whole number from 1 to 1,048,576.");
    public string DownloadLimitsHint => T("Used for new downloads. Downloads already in your queue keep their current settings.");
    public string ConnectionsLabel => T("Download parts");
    public string ConnectionsHint => T("Fetch parts of the same file at the same time, when supported.");
    public string SpeedLimitLabel => T("Limit download speed");
    public string SpeedValueLabel => T("Maximum speed");
    public string SpeedLimitHint => T(IsSpeedLimited ? "Choose a speed, or select Custom for an exact limit." : "Off: use your available bandwidth without a speed cap.");
    public string AttemptsLabel => T("Automatic retries");
    public string AttemptsHint => T("Retry a download if a temporary network error causes it to fail.");
    public string RangeAccelerationLabel => T("Use multiple parts for direct MP4 links");
    public string RangeAccelerationHint => T("Choose 2 or 4 parts above. Supported direct .mp4 file links use those parts; other downloads use their normal method.");
    public string ScheduleLabel => T("Schedule queue");
    public string ScheduleDateLabel => T("Start date");
    public string ScheduleStartLabel => T("Start time (HH:mm)");
    public string ScheduleStopLabel => T("Optional stop time (HH:mm)");
    public string ClearScheduleLabel => T("Clear schedule");
    private void InitializeQueueReliability()
    {
        QueuePauseCommand = new RelayCommand(() => { automaticBrowserJobs.Clear(); Queue.Pause(); }, () => Queue.IsRunning);
        MoveQueueUpCommand = new RelayCommand<Guid>(id => MoveQueueWork(id, -1), id => !Queue.IsRunning && QueueMoveSteps(id, -1) > 0);
        MoveQueueDownCommand = new RelayCommand<Guid>(id => MoveQueueWork(id, 1), id => !Queue.IsRunning && QueueMoveSteps(id, 1) > 0);
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
        ScheduleQueueCommand = new RelayCommand(() =>
        {
            try
            {
                if (!ScheduledDate.HasValue || !TimeOnly.TryParseExact(ScheduledTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)) throw new ArgumentException();
                DateTime first = ScheduledDate.Value.Date + start.ToTimeSpan();
                if (first <= DateTime.Now || TimeZoneInfo.Local.IsInvalidTime(first) || TimeZoneInfo.Local.IsAmbiguousTime(first)) throw new ArgumentException();
                DateTimeOffset? last = null;
                if (!string.IsNullOrWhiteSpace(ScheduledStopTime))
                {
                    if (!TimeOnly.TryParseExact(ScheduledStopTime, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)) throw new ArgumentException();
                    DateTime stop = first.Date + end.ToTimeSpan(); if (stop <= first) stop = stop.AddDays(1);
                    if (TimeZoneInfo.Local.IsInvalidTime(stop) || TimeZoneInfo.Local.IsAmbiguousTime(stop)) throw new ArgumentException();
                    last = new DateTimeOffset(stop);
                }
                Queue.Schedule(new DateTimeOffset(first), last); queueClock.Start(); QueueMessage = T("Schedule saved. Keep Vydrop open.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { QueueMessage = T("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); }
            catch { QueueMessage = T("Choose a future date and valid times in HH:mm format."); }
            OnPropertyChanged(nameof(QueueMessage)); OnPropertyChanged(nameof(ScheduleSummary));
        });
        ClearScheduleCommand = new RelayCommand(() => { try { Queue.Schedule(null, null); OnPropertyChanged(nameof(ScheduleSummary)); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { QueueMessage = T("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); OnPropertyChanged(nameof(QueueMessage)); } });
        queueClock = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, async (_, _) =>
        {
            try
            {
                Queue.CheckScheduledStop(DateTimeOffset.UtcNow);
                if (IsIdle && Queue.TakeScheduledStart(DateTimeOffset.UtcNow))
                {
                    OnPropertyChanged(nameof(ScheduleSummary));
                    if (QueueStartCommand.CanExecute(null)) await QueueStartCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { queueClock.Stop(); QueueMessage = T("Queue changes could not be saved. Keep Vydrop open and check free space and folder access."); OnPropertyChanged(nameof(QueueMessage)); }
        }, Dispatcher.CurrentDispatcher);
    }
    public BrowserReceipt ReceiveBrowserRequest(BrowserRequest request)
    {
        if (request.Type == "ping") return new(true, "connected");
        if (browserClosing) return new(false, "busy");
        if (request.RequestId != null && browserReceipts.TryGetValue(request.RequestId, out var previous)) return previous.Request == request ? previous.Receipt : new(false, "invalid");
        try
        {
            string link = downloads.Normalize(request.Url, false);
            BrowserReceipt receipt;
            if (request.Type == "download") receipt = AcceptBrowserDownload(request, link);
            else if (request.SourcePage != null && Queue.RefreshSource(request.SourcePage, link, request.MediaId)) receipt = new(true, "refreshed");
            else if (!IsIdle || request.Type == "enqueue")
            {
                var existing = Queue.Entries.FirstOrDefault(e => e.Url == link && e.State is QueueState.Pending or QueueState.Paused or QueueState.Interrupted);
                if (existing == null)
                {
                    var options = OutputRequest(); options.Url = link; options.Selection = null; options.QualityKey = null; options.StableOutputName = null;
                    options.SourcePage = request.SourcePage; options.MediaId = request.MediaId;
                    options.ThumbnailUrl = options.Uploader = null; options.Duration = 0;
                    ApplyBrowserOutput(options, request.Output);
                    Queue.Add(options, new Uri(link).Host);
                }
                ShowNotice("Added to queue", "Open the queue to review and start this download.", false, "library");
                receipt = new(true, "queued");
            }
            else { Url = link; pendingBrowserOutput = request.Output; incomingSourcePage = request.SourcePage; incomingMediaId = request.MediaId; ShowAddDownload(); if (checkedUrl == link) ApplyPendingBrowserOutput(); receipt = new(true, "opened"); }
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
    private int QueueMoveSteps(Guid id, int direction)
    {
        var jobs = Queue.Entries;
        int index = -1;
        for (int i = 0; i < jobs.Count; i++) if (jobs[i].Id == id) { index = i; break; }
        if (index < 0 || jobs[index].State == QueueState.Completed) return 0;
        int next = index + direction;
        while (next >= 0 && next < jobs.Count && jobs[next].State == QueueState.Completed) next += direction;
        return next >= 0 && next < jobs.Count ? Math.Abs(next - index) : 0;
    }
    private void MoveQueueWork(Guid id, int direction)
    {
        int steps = QueueMoveSteps(id, direction);
        for (int i = 0; i < steps; i++) if (!Queue.Move(id, direction)) break;
    }
    private string incomingSourcePage, incomingMediaId;
}
