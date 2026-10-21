using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckDownloadImprovements(MainWindow owner, MainViewModel model, FakeProvider provider, string root, string captures)
    {
        const string firstUrl = "https://youtu.be/jNQXAC9IVRw", secondUrl = "https://youtu.be/aqz-KE-bpKQ";
        model.Folder = System.IO.Path.Combine(root, "media");
        model.DownloadConnections = 4; model.SpeedLimitKiB = 512; model.QueueAttempts = 3; model.DirectFileAcceleration = true;
        var preferences = new SettingsRepository(root).Load();
        Check(preferences.Connections == 4 && preferences.SpeedLimitKiB == 512 && preferences.QueueAttempts == 3 && preferences.DirectFileAcceleration, "Download limits persist without replacing existing preferences");
        var request = new BrowserRequest(2, "enqueue", firstUrl, "same-request", "https://www.youtube.com/watch?v=jNQXAC9IVRw", "jNQXAC9IVRw");
        Check(model.ReceiveBrowserRequest(request).Code == "queued" && model.QueueItems.Count == 1, "Explicit browser queue action persists a job without starting a download");
        Check(model.ReceiveBrowserRequest(request).Code == "queued" && model.QueueItems.Count == 1 && model.ReceiveBrowserRequest(request with { Url = secondUrl }).Code == "invalid", "Request identity prevents duplicate jobs and rejects reused IDs with changed payloads");
        provider.DownloadDelayMilliseconds = 10000;
        var running = model.QueueStartCommand.ExecuteAsync(null);
        for (int wait = 0; wait < 100 && !(model.QueueRunning && model.IsBusy); wait++) await Task.Delay(25);
        Check(model.QueueRunning && model.IsBusy, "Queue started for browser handoff fixture");
        Check(model.ReceiveBrowserRequest(new BrowserRequest(1, "open-link", secondUrl)).Accepted && model.QueueItems.Count == 2 && model.QueueRunning, "Legacy extension links can be queued while downloading without cancelling the active job");
        Check(model.ReceiveBrowserRequest(new(2, "ping", RequestId: "health")).Code == "connected" && model.QueueItems.Count == 2, "Connection tests do not change links or create downloads");
        model.QueuePauseCommand.Execute(null); await running;
        var first = model.Queue.Entries[0];
        Check(first.State == QueueState.Paused && model.QueueItems[0].CanRetry && model.QueueItems[0].RetryAction == "Resume", "The pause action exposes an explicit Resume action");
        var recovered = new DownloadQueue(new LocalQueueStorage(root));
        Check(recovered.Entries.Count == 2 && recovered.Entries[0].State == QueueState.Paused && recovered.Entries[1].State == QueueState.Pending && !recovered.IsRunning, "A restarted desktop restores paused and pending jobs without auto-starting");
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            await InspectQueueModal(owner, model, async dialog =>
            {
                dialog.Width = 640; dialog.Height = 540;
                var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                var limits = Descendants<Expander>(dialog).Single(e => Equals(e.Header, model.DownloadLimitsLabel));
                limits.IsExpanded = true;
                await Task.Delay(120); dialog.UpdateLayout();
                Check(viewport.ScrollableWidth <= 1 && Descendants<DownloadLimitsView>(dialog).Any(), "Download controls fit the existing queue design in " + language + (dark ? " dark" : " light"));
                viewport.ScrollToVerticalOffset(limits.TransformToAncestor(viewport).Transform(new Point()).Y + viewport.VerticalOffset);
                await Task.Delay(75);
                Capture(dialog, System.IO.Path.Combine(captures, "download-controls-" + language + (dark ? "-dark" : "-light") + ".png"));
                limits.IsExpanded = false; viewport.ScrollToEnd(); await Task.Delay(100);
                Check(viewport.ScrollableWidth <= 1 && model.QueueItems[0].CanRefresh, "Queue recovery controls stay reachable in both directions");
                Capture(dialog, System.IO.Path.Combine(captures, "durable-queue-" + language + (dark ? "-dark" : "-light") + ".png"));
            });
        }
        model.SelectLanguageCommand.Execute("en");
        model.ScheduledDate = DateTime.Today.AddDays(1); model.ScheduledTime = "12:30"; model.ScheduledStopTime = "13:00";
        model.ScheduleQueueCommand.Execute(null);
        Check(model.Queue.StartAt.HasValue && new DownloadQueue(new LocalQueueStorage(root)).StartAt == model.Queue.StartAt, "The queue schedule is saved through the desktop controls");
        model.ClearScheduleCommand.Execute(null);
        model.ScheduledTime = "invalid"; model.ScheduleQueueCommand.Execute(null);
        Check(!model.Queue.StartAt.HasValue && model.QueueMessage.Contains("valid times", StringComparison.Ordinal), "Invalid schedule input gives recovery feedback without starting work");
        provider.DownloadDelayMilliseconds = 10;
        model.RetryQueueCommand.Execute(first.Id);
        await model.QueueStartCommand.ExecuteAsync(null);
        Check(model.Queue.Entries.All(e => e.State == QueueState.Completed), "Resumed and subsequently queued downloads complete through the real desktop workflow");
        model.Url = firstUrl;
        for (int wait = 0; wait < 100 && !model.CanDownload; wait++) await Task.Delay(25);
        model.AddQueueCommand.Execute(null);
        var pending = model.Queue.Entries[^1]; int countBefore = model.QueueItems.Count;
        await model.DownloadCommand.ExecuteAsync(null);
        Check(pending.State == QueueState.Completed && model.QueueItems.Count == countBefore,
            "Download reuses an already queued matching job instead of rejecting it or creating a duplicate");
    }
}
