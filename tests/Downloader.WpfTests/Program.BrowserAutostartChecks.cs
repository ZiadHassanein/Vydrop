using System.IO;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckBrowserAutostart(MainWindow window, MainViewModel model, FakeProvider provider, string root)
    {
        model.SelectLanguageCommand.Execute("en"); model.Folder = root; model.Url = "";
        string file = Path.Combine(root, "browser-confirmed.mp4"); File.WriteAllBytes(file, new byte[128]);
        int sequence = 0;
        BrowserRequest Request() => new(2, "download", "https://www.youtube.com/watch?v=fixture" + (++sequence).ToString("D4", System.Globalization.CultureInfo.InvariantCulture), "browser_start_" + sequence, Output: new("video", "webm", 720));
        async Task Until(Func<bool> ready)
        { for (int i = 0; i < 200 && !ready(); i++) await Task.Delay(25); Check(ready(), "Browser lifecycle operation reaches its expected state"); }
        provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = file };
        var first = Request(); int before = provider.DownloadCalls;
        Check(model.ReceiveBrowserRequest(first).Code == "started" && !model.AddDownloadVisible, "A confirmed browser choice starts directly in the manager without another editor");
        Check(model.ReceiveBrowserRequest(first).Accepted && model.ReceiveBrowserRequest(first with { Url = "https://youtu.be/jNQXAC9IVRw" }).Code == "invalid", "A replayed browser start is idempotent and cannot change its original payload");
        await Until(() => model.IsIdle && provider.DownloadCalls == before + 1);
        Check(provider.LastRequest.VideoContainer == VideoContainer.WebM && provider.LastRequest.Height == 720 && provider.LastRequest.Folder == root && provider.LastRequest.Captions == null, "The download engine receives the chosen format, height and saved folder without stale source settings");
        var completed = model.Queue.Entries.Single(e => e.Url == first.Url);
        Check(completed.State == QueueState.Completed && new DownloadQueue(new LocalQueueStorage(root)).Entries.Any(e => e.Id == completed.Id), "The direct browser download is durably saved before and after completion");
        foreach (string stop in new[] { "pause", "scheduled", "blocked", "stop-result", "cancel" })
        {
            using var release = new ManualResetEventSlim();
            provider.DownloadHandler = (_, token, _) =>
            {
                WaitHandle.WaitAny(new[] { release.WaitHandle, token.WaitHandle }); token.ThrowIfCancellationRequested();
                if (stop == "blocked") throw new UnauthorizedAccessException("Folder access denied");
                return stop == "stop-result" ? new DownloadResult { StopQueue = true, StopReason = "Stop the queue", Failed = 1 } : new DownloadResult { FilePath = file };
            };
            var active = Request(); model.ReceiveBrowserRequest(active);
            await Until(() => model.Queue.Entries.Single(e => e.Url == active.Url).State is QueueState.Preparing or QueueState.Downloading);
            var pending = Request(); Check(model.ReceiveBrowserRequest(pending).Code == "queued", "A busy browser handoff is durably queued: " + stop);
            model.HistoryCommand.Execute(null); await Task.Delay(30);
            Check(model.HistoryVisible && window.ManagerContent.HistoryContent.IsVisible && model.Queue.IsRunning && window.OwnedWindows.Count == 0, "History is a navigable center panel during an active transfer");
            model.NewDownloadCommand.Execute(null);
            Check(model.AddDownloadVisible && window.ManagerContent.EditorHost.IsVisible && model.Queue.IsRunning, "Add download remains inline while another transfer is active");
            model.LibraryFilterCommand.Execute("all");
            before = provider.DownloadCalls;
            if (stop == "pause") model.QueuePauseCommand.Execute(null);
            else if (stop == "scheduled") { var now = DateTimeOffset.UtcNow; model.Queue.Schedule(now.AddMinutes(-2), now.AddMinutes(-1)); model.Queue.CheckScheduledStop(now); }
            else if (stop == "cancel") model.CancelCommand.Execute(null);
            else release.Set();
            await Until(() => model.IsIdle); await Task.Delay(150);
            Check(model.Queue.Entries.Single(e => e.Url == pending.Url).State == QueueState.Pending && provider.DownloadCalls == before, "A " + stop + " stop preserves the next browser job without silently starting it");
            model.Queue.Remove(model.Queue.Entries.Single(e => e.Url == pending.Url).Id);
            if (stop == "scheduled") model.Queue.Schedule(null, null);
        }
        using (var release = new ManualResetEventSlim())
        {
            int started = 0;
            provider.DownloadHandler = (_, token, _) => { if (Interlocked.Increment(ref started) == 1) { WaitHandle.WaitAny(new[] { release.WaitHandle, token.WaitHandle }); token.ThrowIfCancellationRequested(); } return new DownloadResult { FilePath = file }; };
            var active = Request(); model.ReceiveBrowserRequest(active); await Until(() => started == 1);
            var pending = Request(); model.ReceiveBrowserRequest(pending); release.Set();
            await Until(() => model.IsIdle && started == 2);
            Check(model.Queue.Entries.Single(e => e.Url == pending.Url).State == QueueState.Completed, "Normal completion starts the next explicitly confirmed browser job");
        }
        provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = file };
    }
}
