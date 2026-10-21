using System.Text.Json;
using YouTubeDownloader;

internal static class DownloadImprovementChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    private sealed class FailingStore : IQueueStorage
    {
        public QueueSnapshot Load() => new();
        public void Save(QueueSnapshot snapshot) => throw new IOException("Fixture disk full");
    }
    private sealed class SwitchableStore : IQueueStorage
    {
        public bool Fail { get; set; }
        public QueueSnapshot Load() => new();
        public void Save(QueueSnapshot snapshot) { if (Fail) throw new IOException("Fixture disk full"); }
    }
    internal static async Task Run()
    {
        BrowserFormatChecks.Run();
        await BrowserFormatChecks.CheckLegacySelectionAsync();
        string root = Path.Combine(AppContext.BaseDirectory, "download-improvements-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var storage = new LocalQueueStorage(root);
        var queue = new DownloadQueue(storage);
        var first = queue.Add(new DownloadOptions { Url = "https://example.com/video.mp4", Folder = root, StableOutputName = "A title", SourcePage = "https://example.com/post", MediaId = "asset", Connections = 2, BytesPerSecond = 1048576, Captions = new("ar", false, CaptionFormat.Srt) }, "A title");
        var second = queue.Add(new DownloadOptions { Url = "https://example.com/second", Folder = root }, "Second");
        Check(queue.Move(second.Id, -1), "Idle queue supports reordering");
        var restored = new DownloadQueue(storage);
        Check(restored.Entries.Select(e => e.Id).SequenceEqual(new[] { second.Id, first.Id }) && restored.Entries[^1].Options.Connections == 2 && restored.Entries[^1].Options.Captions.Language == "ar", "Durable queue restores order, identities, output profile, limits and subtitles");
        var failedStore = new DownloadQueue(new FailingStore()); bool refused = false;
        try { failedStore.Add(first.Options); } catch (IOException) { refused = true; }
        Check(refused && failedStore.Entries.Count == 0, "A job is not accepted when durable storage fails");
        var switchable = new SwitchableStore(); var transactional = new DownloadQueue(switchable);
        var waiting = transactional.Add(first.Options);
        await transactional.RunJobAsync(waiting.Id, (_, _, _) => throw new InvalidOperationException("Fixture rejected"));
        transactional.RequestSourceRefresh(waiting.Id); switchable.Fail = true;
        try { transactional.RefreshSource("https://example.com/post", "https://example.com/new.mp4", "asset"); } catch (IOException) { }
        Check(waiting.State == QueueState.AwaitingSource && waiting.Url == first.Url && waiting.Options.PartialGeneration == 0, "A failed source save preserves the original waiting job for a later retry");
        try { transactional.Schedule(DateTimeOffset.UtcNow.AddHours(1), null); } catch (IOException) { }
        Check(transactional.StartAt == null, "A failed schedule save cannot leave an unconfirmed schedule active");
        var inbox = new DownloadQueue(); inbox.Add(new DownloadOptions { Url = "https://example.com/started" });
        await inbox.RunAsync((_, _, _) =>
        {
            inbox.Add(new DownloadOptions { Url = "https://example.com/arrived-later" });
            return Task.FromResult(new DownloadResult { Completed = 1 });
        });
        Check(inbox.Entries[0].State == QueueState.Completed && inbox.Entries[1].State == QueueState.Pending,
            "Links arriving during a queue run wait for the next explicit start instead of downloading silently");
        var snapshot = storage.Load(); snapshot.Jobs[0].State = QueueState.Downloading; storage.Save(snapshot);
        restored = new DownloadQueue(storage);
        Check(restored.Entries[0].State == QueueState.Interrupted && !restored.IsRunning, "Active jobs restore as interrupted and do not automatically download");
        restored.Retry(second.Id);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var running = restored.RunJobAsync(second.Id, async (_, _, token) => { entered.SetResult(); await Task.Delay(Timeout.Infinite, token); return new(); });
        await entered.Task; restored.Pause(); await running;
        Check(restored.Entries[0].State == QueueState.Paused && new DownloadQueue(storage).Entries[0].State == QueueState.Paused, "Pause cancels owned work and saves a resumable job");
        Check(restored.Retry(second.Id), "Paused jobs can be resumed explicitly");
        await restored.RunJobAsync(second.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = Path.Combine(root, "finished.mp4"), Files = new() { Path.Combine(root, "finished.mp4") }, Completed = 1 }));
        Check(new DownloadQueue(storage).Entries[0].State == QueueState.Completed && restored.Entries[^1].State == QueueState.Pending, "Running a single job leaves other explicitly queued jobs pending");
        var current = restored.Entries[^1];
        await restored.RunJobAsync(current.Id, (_, _, _) => throw new InvalidOperationException("Fixture rejected"));
        Check(restored.RequestSourceRefresh(current.Id) == "https://example.com/post", "Refreshing an expiring source retains its original public page");
        bool wrongAsset = false;
        try { restored.RefreshSource("https://example.com/post", "https://example.com/new.mp4", "another-asset"); } catch (ArgumentException) { wrongAsset = true; }
        Check(wrongAsset && restored.Entries[^1].State == QueueState.AwaitingSource, "A neighboring video cannot replace a waiting job");
        Check(restored.RefreshSource("https://example.com/post", "https://example.com/new.mp4", "asset") && current.Options.PartialGeneration == 1 && current.State == QueueState.Paused, "A renewed URL is saved, waits for user resume and cannot append to unverified old partials");
        var now = DateTimeOffset.UtcNow;
        restored.Schedule(now.AddMinutes(1), now.AddMinutes(2));
        Check(!restored.TakeScheduledStart(now) && restored.TakeScheduledStart(now.AddMinutes(1)) && current.State == QueueState.Pending && restored.StartAt == null, "Scheduled starts are one-shot and resume waiting jobs only when due");
        restored.Schedule(now.AddMinutes(-2), now.AddMinutes(-1));
        Check(!restored.TakeScheduledStart(now) && restored.StartAt == null, "An expired start/stop window cannot trigger late downloads after restart");
        restored.Schedule(now.AddMinutes(-2), now.AddMinutes(-1));
        restored.CheckScheduledStop(now);
        Check(!restored.TakeScheduledStart(now) && restored.StartAt == null && restored.StopAt == null && new DownloadQueue(storage).StartAt == null,
            "Checking an elapsed stop consumes its start too, so the same timer tick cannot restart the queue");
        bool invalidWindow = false;
        try { restored.Schedule(now, now.AddSeconds(-1)); } catch (ArgumentException) { invalidWindow = true; }
        Check(invalidWindow, "Schedule rejects stop times before start times");
        File.WriteAllText(storage.FilePath, "{broken");
        var recovered = new DownloadQueue(storage);
        Check(recovered.Entries.Count > 0 && Directory.GetFiles(root, "queue.json.damaged-*").Length == 1, "A damaged queue recovers from its atomic backup and preserves the damaged original");
        string malformed = Path.Combine(root, "malformed"); Directory.CreateDirectory(malformed); File.WriteAllText(Path.Combine(malformed, "queue.json"), "{broken");
        Check(new DownloadQueue(new LocalQueueStorage(malformed)).StorageError != null && File.ReadAllText(Path.Combine(malformed, "queue.json")) == "{broken", "Unreadable queue data is retained and reported rather than discarded");
        string malformedPlaylist = Path.Combine(root, "malformed-playlist"); Directory.CreateDirectory(malformedPlaylist);
        var malformedJob = new QueueSnapshot { Jobs = new() { new() { Id = Guid.NewGuid(), Options = new DownloadOptions { Url = "https://example.com/video", Playlist = new PlaylistInfo { Entries = null } } } } };
        new LocalQueueStorage(malformedPlaylist).Save(malformedJob);
        Check(new DownloadQueue(new LocalQueueStorage(malformedPlaylist)).StorageError != null, "Malformed saved playlist data cannot crash queue recovery");

        using var legacy = JsonDocument.Parse("{\"version\":1,\"type\":\"open-link\",\"url\":\"https://example.com/video\"}");
        Check(BrowserProtocol.ReadRequest(legacy.RootElement).Version == 1, "Exact legacy browser envelopes are still accepted");
        using var modern = JsonDocument.Parse("{\"version\":2,\"type\":\"enqueue\",\"requestId\":\"fixture_1\",\"url\":\"https://example.com/video.mp4\",\"sourcePage\":\"https://example.com/post\",\"mediaId\":\"asset\"}");
        Check(BrowserProtocol.ReadRequest(modern.RootElement).SourcePage == "https://example.com/post", "Version 2 validates and retains source context");
        foreach (string input in new[] { "{\"version\":2,\"type\":\"ping\",\"requestId\":\"x\",\"url\":\"https://example.com\"}", "{\"version\":2,\"type\":\"ping\",\"requestId\":\"x\",\"requestId\":\"y\"}", "[]", "{\"version\":2,\"type\":\"enqueue\",\"requestId\":\"x\",\"url\":\"file:///C:/private.mp4\"}" })
        {
            bool rejected = false; using var invalid = JsonDocument.Parse(input);
            try { BrowserProtocol.ReadRequest(invalid.RootElement); } catch (Exception ex) when (ex is InvalidDataException or ArgumentException) { rejected = true; }
            Check(rejected, "Malformed, duplicate or non-public native fields are rejected");
        }
        using var lifetime = new CancellationTokenSource();
        var listening = BrowserLinkTransport.ListenRequestsAsync(request => new(true, request.Type == "ping" ? "connected" : "queued"), lifetime.Token);
        var receipt = await BrowserLinkTransport.TrySendRequestAsync(new(2, "ping", RequestId: "test-ping"), 500, CancellationToken.None);
        Check(receipt?.Code == "connected", "Health checks complete a native pipe round trip without sending a media URL");
        lifetime.Cancel(); await listening;
        var coldQueue = new DownloadQueue(new LocalQueueStorage(Path.Combine(root, "cold-start")));
        using var coldLifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task coldListener = Task.CompletedTask; int launches = 0;
        var coldReceipt = await BrowserHost.DeliverRequestAsync(new(2, "enqueue", "https://example.com/cold.mp4", "cold-request"), () =>
        {
            launches++;
            coldListener = BrowserLinkTransport.ListenRequestsAsync(request =>
            {
                coldQueue.Add(new DownloadOptions { Url = request.Url, Folder = root });
                return new(true, "queued");
            }, coldLifetime.Token);
        }, coldLifetime.Token);
        Check(launches == 1 && coldReceipt is { Accepted: true, Code: "queued" } && new DownloadQueue(new LocalQueueStorage(Path.Combine(root, "cold-start"))).Entries.Count == 1 && !coldQueue.IsRunning,
            "Cold-start delivery launches once, reaches the new listener and persists the job without starting a download");
        coldLifetime.Cancel(); await coldListener;
        using var delayedLifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        BrowserRequest delayedRequest = null;
        var delayedListener = Task.Run(async () =>
        {
            // The listener appears just after the initial v2 connection timeout.
            // Startup must retry v2 rather than handing a queue job to the v1 path.
            await Task.Delay(350, delayedLifetime.Token);
            await BrowserLinkTransport.ListenRequestsAsync(request => { delayedRequest = request; return new(true, "queued"); }, delayedLifetime.Token);
        });
        try
        {
            var delayedReceipt = await BrowserHost.DeliverRequestAsync(new(2, "enqueue", "https://example.com/delayed.mp4", "delayed-request", "https://example.com/post", "asset"), () => { }, delayedLifetime.Token);
            Check(delayedReceipt is { Accepted: true, Code: "queued" } && delayedRequest is { Version: 2, Type: "enqueue", SourcePage: "https://example.com/post", MediaId: "asset" },
                "A listener appearing after connection timeout retains the queue action and source context without a legacy downgrade");
        }
        finally { delayedLifetime.Cancel(); await delayedListener; }
        using var oldLifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var oldListener = Task.Run(async () =>
        {
            for (int request = 0; request < 2; request++)
            {
                using var pipe = new System.IO.Pipes.NamedPipeServerStream(BrowserLinkTransport.PipeName, System.IO.Pipes.PipeDirection.InOut, 1,
                    System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(oldLifetime.Token);
                using var message = await BrowserProtocol.ReadAsync(pipe, oldLifetime.Token);
                await BrowserProtocol.WriteAsync(pipe, new { ok = message.RootElement.GetProperty("version").GetInt32() == 1 }, oldLifetime.Token);
            }
        });
        var oldReceipt = await BrowserHost.DeliverRequestAsync(new(2, "enqueue", "https://example.com/legacy.mp4", "old-request"), () => throw new Exception("An old running app must not be relaunched."), oldLifetime.Token);
        await oldListener;
        Check(oldReceipt is { Accepted: true, Code: "opened" }, "A v2 host falls back to an exact v1 link when an older running app returns an unversioned response");
        using var closingLifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var closingListener = Task.Run(async () =>
        {
            for (int index = 0; index < 2; index++)
            {
                if (index == 1) await Task.Delay(1000, closingLifetime.Token); // Released v1 apps back off after rejecting v2.
                using var pipe = new System.IO.Pipes.NamedPipeServerStream(BrowserLinkTransport.PipeName, System.IO.Pipes.PipeDirection.InOut, 1,
                    System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(closingLifetime.Token);
                using var message = await BrowserProtocol.ReadAsync(pipe, closingLifetime.Token);
                if (index == 1) await BrowserProtocol.WriteAsync(pipe, new { ok = message.RootElement.GetProperty("version").GetInt32() == 1 }, closingLifetime.Token);
            }
        });
        var closingReceipt = await BrowserHost.DeliverRequestAsync(new(2, "enqueue", "https://example.com/old-closing.mp4", "closing-request"), () => throw new Exception("An older connected app must not be relaunched."), closingLifetime.Token);
        await closingListener;
        Check(closingReceipt is { Accepted: true, Code: "opened" }, "Legacy listeners that close an unsupported v2 frame still receive the exact v1 fallback");
        await DirectRangeChecks.Run(root);
        Console.WriteLine("Download improvement checks passed.");
    }
}
