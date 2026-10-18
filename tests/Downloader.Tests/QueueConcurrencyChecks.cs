using YouTubeDownloader;

internal static class QueueConcurrencyChecks
{
    internal static async Task Run()
    {
        int checks = 0;
        for (int iteration = 0; iteration < 150; iteration++)
        {
            var queue = new DownloadQueue();
            var entry = queue.Add(new DownloadOptions { Url = "https://example.com/video", Folder = "fixture" });
            using var stop = new CancellationTokenSource();
            Task reporter = Task.CompletedTask;
            IProgress<DownloadProgress> late = null;
            int terminalSeen = 0, regressed = 0;
            Action observer = () =>
            {
                // Also exercises re-entry into the queue from an event subscriber.
                var state = queue.Entries.Single().State;
                if (state is QueueState.Completed or QueueState.Cancelled or QueueState.Failed)
                    Interlocked.Exchange(ref terminalSeen, 1);
                else if (Volatile.Read(ref terminalSeen) != 0)
                    Interlocked.Exchange(ref regressed, 1);
            };
            queue.Changed += observer;
            int outcome = iteration % 3;
            await queue.RunAsync(async (_, progress, _) =>
            {
                late = progress;
                var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                reporter = Task.Run(() =>
                {
                    ready.SetResult();
                    while (!stop.IsCancellationRequested)
                    {
                        progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 17, ItemTitle = "Fixture" });
                        Thread.Yield();
                    }
                }, CancellationToken.None);
                await ready.Task;
                if (outcome == 1) throw new OperationCanceledException();
                if (outcome == 2) throw new InvalidOperationException("Fixture rejected.");
                return new DownloadResult { FilePath = "fixture.mp4", Completed = 1 };
            });
            stop.Cancel();
            await reporter;
            late.Report(new DownloadProgress { Stage = "Checking video", Percent = 0 });
            var expected = outcome == 0 ? QueueState.Completed : outcome == 1 ? QueueState.Cancelled : QueueState.Failed;
            if (entry.State != expected || regressed != 0 || entry.Attempts != 1)
                throw new Exception("Concurrent progress changed a terminal queue entry.");
            queue.Changed -= observer;
            if (!queue.Remove(entry.Id)) throw new Exception("A finished queue entry cannot be removed.");
            checks++;
        }

        var retryQueue = new DownloadQueue();
        var retryEntry = retryQueue.Add(new DownloadOptions { Url = "https://example.com/retry", Folder = "fixture" });
        IProgress<DownloadProgress> previous = null;
        await retryQueue.RunAsync((_, progress, _) => { previous = progress; throw new InvalidOperationException("Fixture rejected."); });
        if (!retryQueue.Retry(retryEntry.Id)) throw new Exception("Failed queue job cannot be retried.");
        await retryQueue.RunAsync((_, progress, _) =>
        {
            progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 45, ItemTitle = "Current attempt" });
            previous.Report(new DownloadProgress { Stage = "Checking video", Percent = 1, ItemTitle = "Stale attempt" });
            if (retryEntry.Percent != 45 || retryEntry.Title != "Current attempt") throw new Exception("Old-generation progress escaped retry isolation.");
            return Task.FromResult(new DownloadResult { Completed = 1, FilePath = "retry.mp4" });
        });
        Console.WriteLine((checks + 1) + " queue concurrency checks passed.");
    }
}
