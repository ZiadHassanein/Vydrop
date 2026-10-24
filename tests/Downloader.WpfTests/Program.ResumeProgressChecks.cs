using System.IO;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckResumeProgress(MainWindow window, MainViewModel model, FakeProvider provider, string root, string captures)
    {
        model.Folder = root;
        var entry = model.Queue.Add(new DownloadOptions { Url = "https://youtu.be/jNQXAC9IVRw", Folder = root, Height = 720 }, "Resume percentage fixture");
        await model.Queue.RunJobAsync(entry.Id, (_, progress, _) =>
        {
            progress.Report(new() { Stage = "Downloading", Percent = 53, DownloadedBytes = 530, TotalBytes = 1000 });
            model.Queue.Pause();
            throw new OperationCanceledException();
        });
        Check(entry.State == QueueState.Paused && entry.Percent == 53, "Pausing retains the measured 53 percent");
        using var release = new ManualResetEventSlim();
        using var transfer = new ManualResetEventSlim();
        provider.InspectionHook = token => release.Wait(token);
        string file = Path.Combine(root, "resumed.mp4"); File.WriteAllBytes(file, new byte[1000]);
        provider.DownloadHandler = (_, token, progress) =>
        {
            progress.Report(new() { Stage = "Downloading", Percent = 54, DownloadedBytes = 540, TotalBytes = 1000 });
            transfer.Wait(token);
            return new() { FilePath = file, Completed = 1 };
        };
        var running = model.QueuePrimaryCommand.ExecuteAsync(null);
        try
        {
            await WaitFor(() => entry.State == QueueState.Preparing && model.QueueRunning, "Resume enters actual metadata preparation");
            await Task.Delay(100); window.UpdateLayout();
            var row = model.QueueItems.Single();
            var bars = Descendants<ProgressBar>(window.ManagerContent.DownloadsList).Where(bar => bar.IsVisible).ToArray();
            Check(row.Percent == 53 && row.RowStatus.Contains("53", StringComparison.Ordinal) && !row.IsPreparingWithoutProgress && bars.Length == 1 && bars[0].Value == 53 && !bars[0].IsIndeterminate,
                "Resuming displays the saved percentage and a filled bar while checking the video");
            Capture(window, Path.Combine(captures, "resume-preserves-53-percent.png"));
            release.Set();
            await WaitFor(() => entry.State == QueueState.Downloading && entry.Percent == 54, "Language fixture holds a real active transfer at 54 percent");
            model.SelectedLibraryItem = model.QueueItems.Single();
            var activeRequest = provider.LastRequest;
            foreach (string language in new[] { "ar", "en" })
            {
                Check(model.SelectLanguageCommand.CanExecute(language) && model.ToggleLanguageCommand.CanExecute(null), "Language controls remain available during downloads");
                model.SelectLanguageCommand.Execute(language);
                await Task.Delay(60); window.UpdateLayout();
                Check(model.IsArabic == (language == "ar") && model.QueueRunning && entry.Percent == 54 && entry.State == QueueState.Downloading && model.SelectedLibraryItem?.Id == entry.Id &&
                    ReferenceEquals(activeRequest, provider.LastRequest) && activeRequest.Height == 720 && activeRequest.JobId == entry.Id,
                    "Language changes preserve the live job, measured progress, selected row and output settings: " + language);
                Capture(window, Path.Combine(captures, "language-during-download-" + language + ".png"));
            }
            model.ToggleLanguageCommand.Execute(null);
            Check(model.IsArabic && entry.Percent == 54, "The header language toggle also works during transfer");
            model.ToggleLanguageCommand.Execute(null);
        }
        finally { release.Set(); transfer.Set(); await running; provider.InspectionHook = null; provider.DownloadHandler = null; }
        Check(entry.State == QueueState.Completed && entry.Percent == 100 && provider.LastRequest.JobId == entry.Id,
            "Measured progress continues on the same job and completes at 100 percent");
    }
}
