#nullable enable
namespace YouTubeDownloader;

public interface IQueueStorage
{
    QueueSnapshot Load();
    void Save(QueueSnapshot snapshot);
}
public sealed class QueueSnapshot
{
    public int Version { get; set; } = 1;
    public List<StoredQueueJob> Jobs { get; set; } = new();
    public DateTimeOffset? StartAt { get; set; }
    public DateTimeOffset? StopAt { get; set; }
}
public sealed class StoredQueueJob
{
    public Guid Id { get; set; }
    public DownloadOptions Options { get; set; } = new();
    public string Title { get; set; } = "";
    public QueueState State { get; set; }
    public double Percent { get; set; }
    public string? Error { get; set; }
    public string[] Files { get; set; } = Array.Empty<string>();
}

public sealed partial class DownloadQueue
{
    private readonly IQueueStorage? storage;
    private bool pauseRequested;
    public string? StorageError { get; private set; }
    public DateTimeOffset? StartAt { get; private set; }
    public DateTimeOffset? StopAt { get; private set; }
    public DownloadQueue(IQueueStorage? storage = null)
    {
        this.storage = storage;
        if (storage == null) return;
        try
        {
            var saved = storage.Load();
            ValidateSnapshot(saved);
            StartAt = saved.StartAt;
            StopAt = saved.StopAt;
            foreach (var job in saved.Jobs)
            {
                var options = job.Options.Snapshot();
                options.Selection = null; options.QualityKey = null;
                var entry = new QueueEntry(WithJobIdentity(options, job.Id), job.Title) { Id = job.Id, State = job.State, Percent = Math.Clamp(job.Percent, 0, 100), Error = job.Error };
                if (entry.State is QueueState.Preparing or QueueState.Downloading) entry.State = QueueState.Interrupted;
                if (job.Files.Length > 0) entry.Result = new DownloadResult { FilePath = job.Files.Last(), Files = job.Files.ToList(), Completed = job.Files.Length };
                entries.Add(entry);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or System.Text.Json.JsonException)
        { entries.Clear(); StorageError = "The saved queue could not be read. Its original file was kept for recovery."; }
    }
    internal static void ValidateSnapshot(QueueSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Version != 1 || snapshot.Jobs == null || snapshot.Jobs.Count > Capacity) throw new InvalidDataException();
        var ids = new HashSet<Guid>();
        foreach (var job in snapshot.Jobs)
        {
            if (job == null || job.Options == null || job.Files == null || job.Files.Length > 1000 || job.Id == Guid.Empty || !ids.Add(job.Id) || !Enum.IsDefined(job.State) || !double.IsFinite(job.Percent) || string.IsNullOrWhiteSpace(job.Options.Url) || job.Options.Url.Length > 8192 || job.Title?.Length > 4096) throw new InvalidDataException();
            if (job.Options.Playlist is { } playlist && (playlist.Entries == null || playlist.Entries.Count > 10000 || playlist.Entries.Any(entry => entry == null))) throw new InvalidDataException();
        }
    }
    private static DownloadOptions WithJobIdentity(DownloadOptions value, Guid id) { value.JobId = id; return value; }
    private void Persist()
    {
        if (storage == null) return;
        storage.Save(new QueueSnapshot
        {
            StartAt = StartAt, StopAt = StopAt,
            Jobs = entries.Select(e => new StoredQueueJob
            {
                Id = e.Id, Options = e.Options, Title = e.Title, State = e.State, Percent = e.Percent, Error = e.Error,
                Files = (e.Result?.Files ?? new()).Append(e.Result?.FilePath).Where(f => !string.IsNullOrWhiteSpace(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()!
            }).ToList()
        });
        StorageError = null;
    }
    private void Signal(bool durable = true)
    {
        if (durable) lock (gate)
            try { Persist(); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
            { StorageError = "Queue changes could not be saved. Keep Vydrop open and check free space and folder access."; }
        Changed?.Invoke();
    }
    public void Pause() { lock (gate) pauseRequested = true; Stop(); }
    public bool Move(Guid id, int delta)
    {
        lock (gate)
        {
            int index = entries.FindIndex(e => e.Id == id), next = index + Math.Sign(delta);
            if (lifetime != null || index < 0 || next < 0 || next >= entries.Count) return false;
            (entries[index], entries[next]) = (entries[next], entries[index]);
        }
        Signal(); return true;
    }
    public void Schedule(DateTimeOffset? startAt, DateTimeOffset? stopAt)
    {
        if (stopAt.HasValue && (!startAt.HasValue || stopAt <= startAt)) throw new ArgumentException("Choose a stop time after the start time.");
        lock (gate)
        {
            var previous = (StartAt, StopAt);
            StartAt = startAt; StopAt = stopAt;
            try { Persist(); } catch { (StartAt, StopAt) = previous; throw; }
        }
        Changed?.Invoke();
    }
    public bool TakeScheduledStart(DateTimeOffset now)
    {
        lock (gate)
        {
            if (lifetime != null || !StartAt.HasValue || now < StartAt) return false;
            var previous = (StartAt, StopAt);
            var resumable = entries.Where(e => e.State is QueueState.Paused or QueueState.Interrupted).Select(e => (Entry: e, e.State)).ToArray();
            try
            {
                StartAt = null;
                if (StopAt <= now) { StopAt = null; Persist(); return false; }
                foreach (var job in resumable) job.Entry.State = QueueState.Pending;
                Persist(); return true;
            }
            catch
            {
                (StartAt, StopAt) = previous;
                foreach (var job in resumable) job.Entry.State = job.State;
                throw;
            }
        }
    }
    public void CheckScheduledStop(DateTimeOffset now)
    {
        bool stop;
        lock (gate) { stop = StopAt.HasValue && now >= StopAt; if (stop) { StartAt = null; StopAt = null; } }
        if (stop) { Pause(); Signal(); }
    }
    public void CapturePreparedOptions(DownloadOptions options)
    {
        lock (gate)
        {
            var entry = entries.FirstOrDefault(e => e.Id == options.JobId) ?? throw new InvalidOperationException("The download is no longer queued.");
            var previous = entry.Options;
            entry.UpdateOptions(options);
            try { Persist(); } catch { entry.UpdateOptions(previous); throw; }
        }
    }
    public bool RefreshSource(string page, string url, string? mediaId)
    {
        lock (gate)
        {
            var waiting = entries.Where(e => e.State == QueueState.AwaitingSource && e.Options.SourcePage == page).ToArray();
            if (waiting.Length == 0) return false;
            var entry = waiting.FirstOrDefault(e => !string.IsNullOrEmpty(e.Options.MediaId) && e.Options.MediaId == mediaId) ?? throw new ArgumentException("Choose the same video to refresh this download.");
            var options = entry.Options;
            if (!string.IsNullOrEmpty(options.MediaId) && options.MediaId != mediaId) throw new ArgumentException("Choose the same video to refresh this download.");
            // A new signed URL never appends to unverified partial data from the old resource.
            var previous = (Options: entry.Options, entry.State, entry.Error, entry.Percent, entry.Transfer);
            options.Url = url; options.Selection = null; options.QualityKey = null; options.PartialGeneration++;
            entry.UpdateOptions(options); entry.State = QueueState.Paused; entry.Error = null;
            entry.Percent = 0; entry.Transfer = null;
            try { Persist(); }
            catch { entry.UpdateOptions(previous.Options); entry.State = previous.State; entry.Error = previous.Error; entry.Percent = previous.Percent; entry.Transfer = previous.Transfer; throw; }
        }
        Changed?.Invoke(); return true;
    }
    public string? RequestSourceRefresh(Guid id)
    {
        string? page;
        lock (gate)
        {
            var entry = entries.FirstOrDefault(e => e.Id == id && e.State is QueueState.Failed or QueueState.Paused or QueueState.Interrupted or QueueState.AwaitingSource);
            page = entry?.Options.SourcePage;
            if (entry == null || string.IsNullOrEmpty(page) || string.IsNullOrEmpty(entry.Options.MediaId)) return null;
            var previous = entry.State;
            entry.State = QueueState.AwaitingSource;
            try { Persist(); } catch { entry.State = previous; throw; }
        }
        Changed?.Invoke(); return page;
    }
}
