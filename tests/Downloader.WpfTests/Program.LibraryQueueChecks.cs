using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static void PrepareLibraryQueue(string root)
    {
        string video = Path.Combine(root, "completed-video.mp4"), audio = Path.Combine(root, "completed-audio.mp3");
        File.WriteAllBytes(video, new byte[2048]); File.WriteAllBytes(audio, new byte[1024]);
        var snapshot = new QueueSnapshot();
        void Add(string title, QueueState state, bool sound = false, string file = null)
        {
            int number = snapshot.Jobs.Count;
            snapshot.Jobs.Add(new StoredQueueJob
            {
                Id = Guid.NewGuid(), Title = title, State = state, Percent = state == QueueState.Completed ? 100 : state == QueueState.Pending ? 0 : 42,
                Options = new DownloadOptions { Url = "https://youtu.be/fixture" + number.ToString("D4", System.Globalization.CultureInfo.InvariantCulture),
                    Folder = root, Audio = sound, Height = sound ? 0 : 720, ThumbnailUrl = "https://example.test/thumbnail.png", Duration = 504 },
                Files = file == null ? Array.Empty<string>() : new[] { file }
            });
        }
        Add("Coastal paths · Completed video", QueueState.Completed, file: video);
        Add("A new perspective · Waiting to download", QueueState.Pending);
        Add("Evening notes · Completed audio", QueueState.Completed, true, audio);
        Add("Audio notes · Paused", QueueState.Paused, true);
        Add("Archived video · File moved or deleted", QueueState.Completed, file: Path.Combine(root, "missing.mp4"));
        Add("Restored download · Interrupted", QueueState.Downloading);
        Add("Audio session · Failed", QueueState.Failed, true);
        Add("Short film · Cancelled", QueueState.Cancelled);
        Add("Browser video · Refresh needed", QueueState.AwaitingSource);
        new LocalQueueStorage(root).Save(snapshot);
    }

    private static async Task CheckLibraryQueue(MainWindow window, MainViewModel model, FakeProvider provider, string root, string captures)
    {
        model.Folder = root;
        QueueItemView[] Visible() => model.LibraryItems.Cast<QueueItemView>().ToArray();
        async Task Until(Func<bool> ready, string label)
        {
            for (int i = 0; i < 200 && !ready(); i++) await Task.Delay(25);
            Check(ready(), label);
        }
        Check(model.Queue.Entries.Count == 9 && model.Queue.Entries.Single(e => e.Title.StartsWith("Restored", StringComparison.Ordinal)).State == QueueState.Interrupted &&
            provider.DownloadCalls == 0 && !model.Queue.IsRunning,
            "Version-one saved jobs restore without loss or automatic resumption");
        Check(model.LibraryCount == 3 && model.WaitingCount == 6 && model.VideoCount == 2 && model.AudioCount == 1 &&
            model.PendingCount == 1 && model.PausedCount == 2 && model.AttentionCount == 3,
            "Completed media counts and unfinished status counts form separate scopes");
        Check(model.QueueItems.Where(row => !row.IsComplete).Count() == 6 && model.QueueItems.Where(row => !row.IsComplete).All(row => !row.IsComplete) &&
            model.QueueLabel == "Queue (6)", "Advanced queue management uses the same unfinished-work scope and count");
        var work = model.QueueItems.Where(row => !row.IsComplete).ToArray();
        var completedOrder = model.QueueItems.Where(row => row.IsComplete).Select(row => row.Id).ToArray();
        Check(!model.MoveQueueUpCommand.CanExecute(work[0].Id) && !model.MoveQueueDownCommand.CanExecute(work[^1].Id),
            "Queue ordering disables unavailable edges even with completed records stored between jobs");
        model.MoveQueueUpCommand.Execute(work[1].Id);
        Check(model.QueueItems.Where(row => !row.IsComplete).Take(2).Select(row => row.Id).SequenceEqual(new[] { work[1].Id, work[0].Id }) &&
            model.QueueItems.Where(row => row.IsComplete).Select(row => row.Id).SequenceEqual(completedOrder),
            "Moving unfinished work skips completed records and preserves the Library order");
        model.MoveQueueDownCommand.Execute(work[1].Id);
        await InspectQueueModal(window, model, async dialog =>
        {
            var jobs = model.QueueItems.Where(row => !row.IsComplete).ToArray();
            Check(jobs.Length == 6 && jobs.All(row => !row.IsComplete),
                "The actual queue-management dialog excludes completed media");
            Capture(dialog, Path.Combine(captures, "queue-management.png"));
            await Task.CompletedTask;
        });
        var finished = model.QueueItems.First(row => row.IsComplete && !row.IsAudio);
        foreach (string locale in new[] { "en", "ar" })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { 1280d, 760d })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    window.Width = width;
                    model.LibraryFilterCommand.Execute("all"); model.LibrarySearch = "";
                    await Task.Delay(160); window.UpdateLayout();
                    Check(Visible().Length == 3 && Visible().All(row => row.IsComplete) && model.IsLibraryAll &&
                        !window.ManagerContent.QueueActions.IsVisible && window.ManagerContent.LibraryFilters.IsVisible && !window.ManagerContent.QueueFilters.IsVisible,
                        "Library shows only completed media and media filters: " + locale + "/" + dark + "/" + width);
                    model.SelectedLibraryItem = finished;
                    Check(Visible().Any(row => row.MissingFile), "Missing completed files retain their explicit unavailable state in Library");
                    Capture(window, Path.Combine(captures, $"library-{locale}-{(dark ? "dark" : "light")}-{width}.png"));
                    model.LibraryFilterCommand.Execute("queue"); model.LibrarySearch = "";
                    await Task.Delay(100); window.UpdateLayout();
                    Check(Visible().Length == 6 && Visible().All(row => !row.IsComplete) && model.IsLibraryQueue &&
                        model.SelectedLibraryItem != finished && Visible().Contains(model.SelectedLibraryItem) &&
                        window.ManagerContent.QueueActions.IsVisible && window.ManagerContent.QueueFilters.IsVisible && !window.ManagerContent.LibraryFilters.IsVisible,
                        "Queue shows unfinished work with its own actions and never retains a hidden Library selection");
                    Check(ReferenceEquals(window.ManagerContent.StartQueueButton.Command, model.QueuePrimaryCommand) && window.ManagerContent.StartQueueButton.IsEnabled &&
                        ReferenceEquals(window.ManagerContent.PauseQueueButton.Command, model.QueuePauseCommand) && !window.ManagerContent.PauseQueueButton.IsEnabled,
                        "Queue controls reuse existing start and pause commands with their correct idle availability");
                    Check(Descendants<ScrollViewer>(window.ManagerContent.LibraryContent).All(scroll => scroll.ScrollableWidth == 0) &&
                        window.ManagerContent.LibraryFilters.ActualWidth <= window.ManagerContent.LibraryContent.ActualWidth,
                        "Both workspaces fit without horizontal scrolling");
                    Capture(window, Path.Combine(captures, $"queue-{locale}-{(dark ? "dark" : "light")}-{width}.png"));
                    foreach (var (filter, count) in new[] { ("pending", 1), ("paused", 2), ("attention", 3), ("active", 0) })
                    {
                        model.LibraryFilterCommand.Execute(filter);
                        Check(Visible().Length == count && model.IsLibraryQueue && model.IsQueuePage,
                            "The " + filter + " filter stays inside Queue and includes only its matching saved states");
                    }
                }
        model.SelectLanguageCommand.Execute("en");
        model.LibraryFilterCommand.Execute("all"); model.LibrarySearch = "Evening notes";
        Check(Visible().Single().IsAudio && Visible().Single().IsComplete, "Library search finds completed audio");
        model.LibraryFilterCommand.Execute("queue"); model.LibrarySearch = "A new perspective";
        Check(Visible().Single().Entry.State == QueueState.Pending, "Queue search finds unfinished work");
        model.LibraryFilterCommand.Execute("all");
        Check(model.LibrarySearch == "Evening notes" && Visible().Single().IsComplete, "Returning to Library restores its own search");
        model.LibraryFilterCommand.Execute("queue");
        Check(model.LibrarySearch == "A new perspective" && Visible().Single().Entry.State == QueueState.Pending, "Returning to Queue restores its independent search");
        model.LibrarySearch = "No matching local fixture";
        Check(model.LibraryEmpty && model.EmptyLibraryHeading == "No matching downloads", "A search miss is distinguished from a cleared queue");
        model.LibraryFilterCommand.Execute("complete"); model.LibrarySearch = "";
        Check(model.IsSavedLibrary && Visible().All(row => row.IsComplete), "The old completed-filter command still opens completed media");
        model.LibraryFilterCommand.Execute("audio"); Check(Visible().Length == 1 && Visible().All(row => row.IsComplete && row.IsAudio), "Audio categories exclude unfinished audio jobs");
        model.LibraryFilterCommand.Execute("video"); Check(Visible().Length == 2 && Visible().All(row => row.IsComplete && !row.IsAudio), "Video categories exclude unfinished video jobs");

        var pending = model.Queue.Entries.Single(entry => entry.State == QueueState.Pending);
        provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = Path.Combine(root, "completed-video.mp4") };
        model.LibraryFilterCommand.Execute("paused"); model.LibrarySearch = "";
        await Task.Delay(80); window.UpdateLayout();
        ((IInvokeProvider)new ButtonAutomationPeer(window.ManagerContent.StartQueueButton).GetPattern(PatternInterface.Invoke)).Invoke();
        await Until(() => model.IsIdle && pending.State == QueueState.Completed, "The visible Start download action completes the pending job");
        Check(model.IsQueuePage && model.IsScopeAll && !Visible().Any(row => row.Id == pending.Id) &&
            model.LibraryCount == 6 && model.WaitingCount == 3 && model.PausedCount == 0 && model.AttentionCount == 3,
            "Explicit Resume completes paused, interrupted and pending jobs while preserving failed, cancelled and source-refresh work");
        model.LibraryFilterCommand.Execute("all");
        Check(Visible().Any(row => row.Id == pending.Id), "The same completed job appears in Library without duplication");
        var restored = new DownloadQueue(new LocalQueueStorage(root));
        Check(restored.Entries.Count == 9 && restored.Entries.Single(entry => entry.Id == pending.Id).State == QueueState.Completed &&
            new LocalQueueStorage(root).Load().Version == 1,
            "Separation keeps the existing queue schema and every saved job intact");

        using var release = new ManualResetEventSlim();
        provider.DownloadHandler = (_, token, progress) =>
        {
            progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 42, DownloadedBytes = 4200000, TotalBytes = 10000000 });
            WaitHandle.WaitAny(new[] { release.WaitHandle, token.WaitHandle }); token.ThrowIfCancellationRequested();
            return new DownloadResult { FilePath = Path.Combine(root, "completed-video.mp4") };
        };
        var request = new BrowserRequest(2, "download", "https://youtu.be/jNQXAC9IVRw", "separated_queue_browser", Output: new("video", "mp4", 720));
        try
        {
            Check(model.ReceiveBrowserRequest(request).Code == "started", "A confirmed browser download retains its immediate-start capability");
            await Until(() => model.IsQueuePage && model.SelectedLibraryItem?.IsActive == true && model.SelectedLibraryItem.Percent == 42,
                "Confirmed browser work opens Queue and exposes its live transfer");
            model.LibraryFilterCommand.Execute("active");
            Check(Visible().Length == 1 && Visible()[0].IsActive && model.IsLibraryQueue, "The active filter shows the actual engine transfer while Queue remains selected");
            Guid browserJobId = Visible()[0].Id;
            window.Width = 1280; await Task.Delay(100); window.UpdateLayout();
            Capture(window, Path.Combine(captures, "queue-active.png"));
            ((IInvokeProvider)new ButtonAutomationPeer(window.ManagerContent.PauseQueueButton).GetPattern(PatternInterface.Invoke)).Invoke();
            await Until(() => model.IsIdle, "The visible Pause queue action stops the live transfer");
            Check(model.Queue.Entries.Single(entry => entry.Id == browserJobId).State == QueueState.Paused && model.LibraryEmpty && !model.HasLibrarySelection,
                "Pausing removes the row from the active filter and clears its hidden inspector selection");
        }
        finally { release.Set(); if (model.Queue.IsRunning) model.Queue.Pause(); }
    }
}
