using System.IO;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckResumeWorkflows(MainViewModel model, FakeProvider provider, string root)
    {
        model.Folder = root; model.Audio = false;
        string file = Path.Combine(root, "resume-workflows.mp4"); File.WriteAllBytes(file, new byte[128]);
        provider.DownloadHandler = (_, _, _) => new() { FilePath = file, Completed = 1 };
        async Task<QueueEntry> Paused(DownloadOptions options)
        {
            var job = model.Queue.Add(options, "Retained output name");
            await model.Queue.RunJobAsync(job.Id, (_, progress, _) =>
            {
                progress.Report(new() { Stage = "Downloading", Percent = 64 });
                model.Queue.Pause(); throw new OperationCanceledException();
            });
            return job;
        }
        string manualUrl = "https://www.youtube.com/watch?v=fixture1001";
        model.NewDownloadCommand.Execute(null); model.Url = manualUrl;
        await WaitFor(() => model.CanDownload, "The manual resume link is inspected");
        model.SelectedQuality = model.Qualities.First(choice => choice.Height == 720);
        var manual = await Paused(new() { Url = manualUrl, Folder = root, Height = 720, PartialGeneration = 3, Connections = 4 });
        int countBefore = model.Queue.Entries.Count;
        using (var release = new ManualResetEventSlim())
        {
            provider.InspectionHook = token => release.Wait(token);
            var running = model.DownloadCommand.ExecuteAsync(null);
            try
            {
                await WaitFor(() => model.Queue.IsRunning, "Manual download enters the queue");
                Check(model.Queue.Entries.Count == countBefore && manual.State == QueueState.Preparing && manual.Percent == 64,
                    "Add download resumes the existing paused job without duplicating it or resetting its saved progress");
            }
            finally { release.Set(); await running; provider.InspectionHook = null; }
        }
        Check(manual.State == QueueState.Completed && provider.LastRequest.JobId == manual.Id && provider.LastRequest.PartialGeneration == 3 && provider.LastRequest.Connections == 4,
            "Manual resume retains the partial-file identity and the job's original download settings");

        var browser = new BrowserRequest(2, "download", "https://www.youtube.com/watch?v=fixture1002", "review_resume", Output: new("video", "webm", 720));
        var paused = await Paused(new() { Url = browser.Url, Folder = root, Height = 720, VideoContainer = VideoContainer.WebM, PartialGeneration = 5 });
        countBefore = model.Queue.Entries.Count;
        using (var release = new ManualResetEventSlim())
        {
            provider.InspectionHook = token => release.Wait(token);
            try
            {
                Check(model.ReceiveBrowserRequest(browser).Code == "started", "A new explicit browser confirmation can start a paused download");
                await WaitFor(() => model.Queue.IsRunning, "Browser resume enters the queue");
                Check(model.Queue.Entries.Count == countBefore && paused.State == QueueState.Preparing && paused.Percent == 64,
                    "Browser download resumes the matching job instead of creating a duplicate");
            }
            finally { release.Set(); await WaitFor(() => model.IsIdle, "Browser resume completes"); provider.InspectionHook = null; }
        }
        Check(paused.State == QueueState.Completed && provider.LastRequest.JobId == paused.Id && provider.LastRequest.PartialGeneration == 5,
            "Browser resume retains its partial-file identity through completion");

        var different = await Paused(new() { Url = browser.Url, Folder = root, Height = 720, VideoContainer = VideoContainer.Mp4 });
        countBefore = model.Queue.Entries.Count;
        model.ReceiveBrowserRequest(browser with { RequestId = "review_other_output" });
        await WaitFor(() => model.IsIdle && model.Queue.Entries.Count == countBefore + 1, "A different output finishes independently");
        Check(different.State == QueueState.Paused && model.Queue.Entries.Count == countBefore + 1 && provider.LastRequest.VideoContainer == VideoContainer.WebM,
            "Requesting another format preserves the paused original and creates the requested output");
        provider.DownloadHandler = null;
    }
}
