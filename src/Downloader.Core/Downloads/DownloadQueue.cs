#nullable enable
namespace YouTubeDownloader;

public enum QueueState { Pending, Preparing, Downloading, Completed, Failed, Cancelled, Paused, Interrupted, AwaitingSource }

public sealed class QueueEntry
{
    private DownloadOptions request;
    public Guid Id { get; internal set; } = Guid.NewGuid();
    public string Url => request.Url;
    public string Title { get; internal set; }
    public QueueState State { get; internal set; }
    public double Percent { get; internal set; }
    public int Attempts { get; internal set; }
    public string? Error { get; internal set; }
    public DownloadResult? Result { get; internal set; }
    public DownloadProgress? Transfer { get; internal set; }
    internal long Generation { get; set; }
    public DownloadOptions Options => request.Snapshot();
    internal void UpdateOptions(DownloadOptions value) => request = value.Snapshot();
    internal QueueEntry(DownloadOptions options, string? title)
        => (request, Title) = (options.Snapshot(), title ?? options.Url);
}

// A single dispatcher owns each job, including retries and durable state transitions.
public sealed partial class DownloadQueue
{
    public const int Capacity = 100;
    private readonly object gate = new();
    private readonly List<QueueEntry> entries = new();
    private CancellationTokenSource? lifetime;
    public event Action? Changed;
    public bool IsRunning { get { lock (gate) return lifetime != null; } }
    public IReadOnlyList<QueueEntry> Entries { get { lock (gate) return entries.ToArray(); } }

    public QueueEntry Add(DownloadOptions options, string? title = null)
    {
        if (options == null || string.IsNullOrWhiteSpace(options.Url)) throw new ArgumentException("Choose a media link.");
        QueueEntry entry;
        lock (gate)
        {
            if (entries.Count >= Capacity) throw new InvalidOperationException("The queue can hold at most 100 downloads. Remove finished items first.");
            if (entries.Any(e => e.State is QueueState.Pending or QueueState.Preparing or QueueState.Downloading && e.Url == options.Url && e.Options.Folder == options.Folder && OutputProfiles.Key(e.Options) == OutputProfiles.Key(options)))
                throw new InvalidOperationException("This download is already in the queue.");
            entry = new(options, title);
            entry.UpdateOptions(WithJobIdentity(entry.Options, entry.Id));
            entries.Add(entry);
            try { Persist(); } catch { entries.Remove(entry); throw; }
        }
        Signal(false);
        return entry;
    }
    public bool Remove(Guid id)
    {
        bool removed;
        lock (gate) removed = entries.RemoveAll(e => e.Id == id && e.State is not (QueueState.Preparing or QueueState.Downloading)) > 0;
        if (removed) Signal();
        return removed;
    }
    public bool Retry(Guid id)
    {
        lock (gate)
        {
            var entry = entries.FirstOrDefault(e => e.Id == id);
            if (entry == null || entry.State is not (QueueState.Failed or QueueState.Cancelled or QueueState.Paused or QueueState.Interrupted)) return false;
            entry.State = QueueState.Pending;
            entry.Error = null;
            entry.Percent = 0;
            entry.Attempts = 0;
            entry.Result = null;
            entry.Transfer = null;
            entry.Generation++;
        }
        Signal();
        return true;
    }
    public void Stop()
    {
        CancellationTokenSource? run;
        lock (gate) run = lifetime;
        // Cancellation can invoke provider callbacks; never hold the mutation gate here.
        try { run?.Cancel(); }
        catch (ObjectDisposedException) { } // The run may have finished after the snapshot.
    }

    public Task RunAsync(Func<DownloadOptions, IProgress<DownloadProgress>, CancellationToken, Task<DownloadResult>> execute,
        Func<TimeSpan, CancellationToken, Task>? wait = null, CancellationToken token = default) => RunJobAsync(null, execute, wait, token);
    public async Task RunJobAsync(Guid? onlyJob, Func<DownloadOptions, IProgress<DownloadProgress>, CancellationToken, Task<DownloadResult>> execute,
        Func<TimeSpan, CancellationToken, Task>? wait = null, CancellationToken token = default)
    {
        CancellationTokenSource run;
        HashSet<Guid> batch;
        lock (gate)
        {
            if (lifetime != null) throw new InvalidOperationException("The queue is already running.");
            run = lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            // A later browser handoff must wait for another explicit start action.
            batch = entries.Where(e => e.State == QueueState.Pending && (!onlyJob.HasValue || e.Id == onlyJob)).Select(e => e.Id).ToHashSet();
        }
        wait ??= Task.Delay;
        Signal();
        try
        {
            while (!run.IsCancellationRequested)
            {
                QueueEntry? entry;
                lock (gate)
                {
                    entry = entries.FirstOrDefault(e => e.State == QueueState.Pending && batch.Contains(e.Id));
                    if (entry == null) break;
                    entry.State = QueueState.Preparing;
                }
                Signal();
                bool halt = false;
                for (int attempt = 0; attempt < Math.Clamp(entry.Options.MaximumAttempts, 1, 5); attempt++)
                {
                    try
                    {
                        long generation;
                        lock (gate)
                        {
                            generation = ++entry.Generation;
                            entry.Attempts++;
                        }
                        var progress = new QueueProgress(p =>
                        {
                            lock (gate)
                            {
                                if (!ReferenceEquals(lifetime, run) || entry.Generation != generation || run.IsCancellationRequested || entry.State is QueueState.Cancelled or QueueState.Completed or QueueState.Failed) return;
                                entry.State = p.Stage is "Preparing media tools" or "Checking video" or "Checking playlist" ? QueueState.Preparing : QueueState.Downloading;
                                entry.Percent = Math.Clamp(p.Percent, 0, 100);
                                entry.Transfer = p;
                                if (!string.IsNullOrWhiteSpace(p.ItemTitle)) entry.Title = p.ItemTitle;
                            }
                            Signal(false);
                        });
                        var result = await execute(entry.Options, progress, run.Token).ConfigureAwait(false);
                        lock (gate) entry.Result = result;
                        if (result.Cancelled) throw new OperationCanceledException();
                        halt = result.StopQueue;
                        if (result.Failed > 0 || result.Remaining > 0 || result.StopReason != null)
                            throw new InvalidOperationException(result.StopReason ?? "Some playlist items failed. Retry to resume completed files.");
                        lock (gate)
                        {
                            entry.State = QueueState.Completed;
                            entry.Percent = 100;
                            entry.Error = null;
                        }
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        lock (gate)
                        {
                            entry.State = pauseRequested ? QueueState.Paused : QueueState.Cancelled;
                            entry.Error = null;
                        }
                        halt = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        string error = ex.Message ?? "Download failed.";
                        if (attempt + 1 < Math.Clamp(entry.Options.MaximumAttempts, 1, 5) && !halt && ex is not MediaToolRecoveryException && !DownloadErrors.IsBlocked(error) && DownloadErrors.IsTransient(error))
                        {
                            lock (gate)
                            {
                                entry.Generation++;
                                entry.Error = "Connection interrupted. Retrying…";
                            }
                            Signal();
                            try { await wait(TimeSpan.FromSeconds(3), run.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException)
                            {
                                lock (gate) { entry.State = pauseRequested ? QueueState.Paused : QueueState.Cancelled; entry.Error = null; }
                                halt = true;
                                break;
                            }
                            continue;
                        }
                        lock (gate)
                        {
                            entry.Error = error.Length > 512 ? error[..512] : error;
                            entry.State = QueueState.Failed;
                        }
                        halt = halt || ex is UnauthorizedAccessException or MediaToolRecoveryException || DownloadErrors.IsBlocked(error) || DownloadErrors.IsTransient(error);
                        break;
                    }
                }
                Signal();
                if (halt) break;
                // Pace separate jobs as well as items within a playlist.
                if (!onlyJob.HasValue && Entries.Any(e => e.State == QueueState.Pending && batch.Contains(e.Id)))
                    await wait(TimeSpan.FromSeconds(2), run.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (gate) { if (ReferenceEquals(lifetime, run)) lifetime = null; pauseRequested = false; }
            run.Dispose();
            Signal();
        }
    }
    private sealed class QueueProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => report(value); }
}
