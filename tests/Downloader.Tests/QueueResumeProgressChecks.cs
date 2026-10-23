using YouTubeDownloader;

internal static class QueueResumeProgressChecks
{
    private sealed class Store(QueueSnapshot saved) : IQueueStorage
    {
        public bool Fail;
        public QueueSnapshot Load() => saved;
        public void Save(QueueSnapshot snapshot)
        {
            if (Fail) throw new IOException("Fixture disk full");
            saved = snapshot;
        }
    }
    internal static async Task Run()
    {
        foreach (var state in new[] { QueueState.Paused, QueueState.Interrupted, QueueState.Cancelled, QueueState.Failed })
        {
            var store = new Store(new QueueSnapshot { Jobs = new() { new() { Id = Guid.NewGuid(), State = state, Percent = 53, Options = new() { Url = "https://example.com/video" } } } });
            var queue = new DownloadQueue(store);
            var entry = queue.Entries.Single();
            void Check(bool value) { if (!value) throw new Exception("Resume percentage regressed for " + state); }
            Check(queue.Retry(entry.Id) && entry.Percent == 53 && new DownloadQueue(store).Entries.Single().Percent == 53);
            await queue.RunAsync((_, progress, _) =>
            {
                Check(entry.State == QueueState.Preparing && entry.Percent == 53 && entry.Transfer == null);
                foreach (string stage in new[] { "Preparing media tools", "Checking video", "Checking playlist", "Connecting", "Downloading" })
                {
                    progress.Report(new() { Stage = stage, Percent = 0, Indeterminate = true });
                    Check(entry.Percent == 53);
                }
                progress.Report(new() { Stage = "Downloading", Percent = 54, DownloadedBytes = 540, TotalBytes = 1000 });
                Check(entry.Percent == 54);
                // A server may genuinely restart a transfer: never invent retained bytes.
                progress.Report(new() { Stage = "Downloading", Percent = 2, DownloadedBytes = 20, TotalBytes = 1000 });
                Check(entry.Percent == 2);
                progress.Report(new() { Stage = "Finishing download", Percent = 100, Indeterminate = true });
                Check(entry.Percent == 100);
                return Task.FromResult(new DownloadResult { FilePath = "fixture.mp4", Completed = 1 });
            });
            Check(entry.State == QueueState.Completed && entry.Percent == 100);
        }
        var sourceStore = new Store(new QueueSnapshot { Jobs = new() { new() { Id = Guid.NewGuid(), State = QueueState.Failed, Percent = 53, Options = new() { Url = "https://example.com/old", SourcePage = "https://example.com/post", MediaId = "asset" } } } });
        var source = new DownloadQueue(sourceStore);
        var job = source.Entries.Single();
        source.RequestSourceRefresh(job.Id);
        sourceStore.Fail = true;
        try { source.RefreshSource(job.Options.SourcePage, "https://example.com/new", "asset"); } catch (IOException) { }
        if (job.Percent != 53 || job.Options.PartialGeneration != 0) throw new Exception("A failed source refresh lost the original progress.");
        sourceStore.Fail = false;
        source.RefreshSource(job.Options.SourcePage, "https://example.com/new", "asset");
        if (job.Percent != 0 || job.Options.PartialGeneration != 1) throw new Exception("A replacement source retained progress from unrelated bytes.");
        Console.WriteLine("PASS: Retry preserves saved progress through preparation, accepts measured updates, and resets only for a replacement source.");
    }
}
