using System.Text.Json;
using YouTubeDownloader;

internal static class QueueRecoveryChecks
{
    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };
    internal static async Task Run()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "queue-recovery-" + Guid.NewGuid().ToString("N"));
        var failures = new List<string>();
        foreach (var check in new Func<string, Task>[] { BackupChecks, InvalidSnapshotChecks, ScheduledStartChecks, CompletionMetadataChecks })
        {
            try { await check(Path.Combine(root, check.Method.Name)); }
            catch (Exception error) { failures.Add(error.Message); Console.WriteLine("FAIL: " + error.Message); }
        }
        if (failures.Count > 0) throw new Exception(string.Join(Environment.NewLine, failures));
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }

    private static Task BackupChecks(string root)
    {
        var store = new LocalQueueStorage(root);
        var queue = new DownloadQueue(store);
        var first = queue.Add(new() { Url = "https://example.com/first" });
        queue.Add(new() { Url = "https://example.com/second" });
        byte[] backup = File.ReadAllBytes(store.FilePath + ".backup");
        File.WriteAllText(store.FilePath, "{damaged-primary");
        var recovered = new DownloadQueue(store);
        Check(recovered.Entries.Single().Id == first.Id, "Queue recovery restores the last valid backup");
        recovered.Add(new() { Url = "https://example.com/after-recovery" });
        Check(File.ReadAllBytes(store.FilePath + ".backup").SequenceEqual(backup), "Saving a recovered queue preserves its last valid backup");
        File.WriteAllText(store.FilePath, "{damaged-again");
        Check(new DownloadQueue(store).Entries.Single().Id == first.Id, "A second damaged primary can still recover after the first recovery was saved");
        File.Delete(store.FilePath);
        Check(new DownloadQueue(store).Entries.Single().Id == first.Id, "A missing primary queue also recovers from its existing backup");
        recovered = new DownloadQueue(store);
        recovered.Add(new() { Url = "https://example.com/valid-replacement" });
        byte[] primary = File.ReadAllBytes(store.FilePath);
        var invalid = store.Load(); invalid.Version = 2;
        bool rejected = false;
        try { store.Save(invalid); } catch (InvalidDataException) { rejected = true; }
        Check(rejected && File.ReadAllBytes(store.FilePath).SequenceEqual(primary) && File.ReadAllBytes(store.FilePath + ".backup").SequenceEqual(backup),
            "Invalid replacement snapshots cannot modify either valid queue file");
        return Task.CompletedTask;
    }

    private static Task InvalidSnapshotChecks(string root)
    {
        foreach (string invalid in new[] { "version", "jobs", "playlist", "state", "duplicate" })
        {
            var store = new LocalQueueStorage(Path.Combine(root, invalid));
            var queue = new DownloadQueue(store);
            var first = queue.Add(new() { Url = "https://example.com/first" });
            queue.Add(new() { Url = "https://example.com/second" });
            var snapshot = store.Load();
            if (invalid == "version") snapshot.Version = 2;
            if (invalid == "jobs") snapshot.Jobs = null;
            if (invalid == "playlist") snapshot.Jobs[0].Options.Playlist = new() { Entries = null };
            if (invalid == "state") snapshot.Jobs[0].State = (QueueState)99;
            if (invalid == "duplicate") snapshot.Jobs[1].Id = snapshot.Jobs[0].Id;
            File.WriteAllBytes(store.FilePath, JsonSerializer.SerializeToUtf8Bytes(snapshot, Json));
            var recovered = new DownloadQueue(store);
            Check(recovered.StorageError == null && recovered.Entries.Count == 1 && recovered.Entries[0].Id == first.Id && Directory.GetFiles(Path.GetDirectoryName(store.FilePath), "queue.json.damaged-*").Length > 0,
                "Invalid queue " + invalid + " data falls back to the valid backup and retains the original");
        }
        string unrecoverable = Path.Combine(root, "no-backup"); Directory.CreateDirectory(unrecoverable);
        var damaged = new LocalQueueStorage(unrecoverable);
        File.WriteAllText(damaged.FilePath, "{unique-damaged-data");
        var replacement = new DownloadQueue(damaged);
        replacement.Add(new() { Url = "https://example.com/replacement" });
        replacement.Add(new() { Url = "https://example.com/another" });
        Check(Directory.GetFiles(unrecoverable, "queue.json.damaged-*").Any(path => File.ReadAllText(path) == "{unique-damaged-data"),
            "Later saves retain an unrecoverable original instead of rotating it out of the backup");
        return Task.CompletedTask;
    }

    private sealed class SwitchableStore : IQueueStorage
    {
        internal bool Fail;
        public QueueSnapshot Load() => new();
        public void Save(QueueSnapshot snapshot) { if (Fail) throw new IOException("Fixture storage unavailable"); }
    }

    private static async Task CompletionMetadataChecks(string root)
    {
        var store = new LocalQueueStorage(root);
        var queue = new DownloadQueue(store);
        var completed = queue.Add(new() { Url = "https://example.com/completed", Folder = root });
        var pending = queue.Add(new() { Url = "https://example.com/pending", Folder = root, MaximumAttempts = 1 });
        Check(completed.CompletedAt == null && pending.CompletedAt == null, "Queued downloads do not claim a completion date before success");
        DateTimeOffset started = DateTimeOffset.UtcNow;
        await queue.RunJobAsync(completed.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = Path.Combine(root, "completed.mp4") }));
        Check(completed.CompletedAt >= started && completed.CompletedAt <= DateTimeOffset.UtcNow,
            "A successful download records its actual completion date");
        await queue.RunJobAsync(pending.Id, (_, _, _) => throw new InvalidOperationException("Fixture failure"));
        var restored = new DownloadQueue(store);
        Check(store.Load().Version == 1 && restored.Entries.Select(job => job.Id).SequenceEqual(queue.Entries.Select(job => job.Id)) &&
            restored.Entries[0].CompletedAt == completed.CompletedAt && restored.Entries[1].CompletedAt == null,
            "Completion dates survive restart without changing saved queue order, schema version, or failed jobs");
        var legacy = store.Load();
        legacy.Jobs[0].CompletedAt = null;
        string legacyJson = JsonSerializer.Serialize(legacy, Json).Replace("\"CompletedAt\":null,", "", StringComparison.Ordinal);
        File.WriteAllText(store.FilePath, legacyJson);
        restored = new DownloadQueue(store);
        Check(restored.StorageError == null && restored.Entries.Count == 2 && restored.Entries[0].State == QueueState.Completed &&
            restored.Entries[0].CompletedAt == null && restored.Entries[1].State == QueueState.Failed,
            "Older queue files without completion dates retain all completed and unfinished jobs");
    }

    private static async Task ScheduledStartChecks(string root)
    {
        var store = new SwitchableStore();
        var queue = new DownloadQueue(store);
        var job = queue.Add(new() { Url = "https://example.com/scheduled", Folder = root });
        await queue.RunJobAsync(job.Id, (_, progress, _) =>
        {
            progress.Report(new() { Stage = "Downloading", Percent = 42 });
            queue.Pause(); throw new OperationCanceledException();
        });
        var start = DateTimeOffset.UtcNow.AddMinutes(1);
        queue.Schedule(start, start.AddMinutes(1));
        store.Fail = true;
        try { queue.TakeScheduledStart(start); } catch (IOException) { }
        Check(queue.StartAt == start && queue.StopAt == start.AddMinutes(1) && job.State == QueueState.Paused && job.Percent == 42,
            "A failed scheduled-start save preserves its schedule and paused job state");
        store.Fail = false;
        Check(queue.TakeScheduledStart(start) && job.State == QueueState.Pending && !queue.TakeScheduledStart(start), "A recovered scheduled start is consumed exactly once");
        queue.Schedule(start, start.AddMinutes(1)); store.Fail = true;
        try { queue.TakeScheduledStart(start.AddMinutes(2)); } catch (IOException) { }
        Check(queue.StartAt == start && queue.StopAt == start.AddMinutes(1), "A failed expired-window save preserves both scheduled times");
        store.Fail = false;
        Check(!queue.TakeScheduledStart(start.AddMinutes(2)) && queue.StartAt == null && queue.StopAt == null, "An expired recovered window is consumed without starting downloads");
    }
}
