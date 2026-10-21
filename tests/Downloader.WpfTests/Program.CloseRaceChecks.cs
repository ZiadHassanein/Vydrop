using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckCloseRace(string root)
    {
        const string single = "https://youtu.be/jNQXAC9IVRw";
        const string playlist = "https://www.youtube.com/playlist?list=PLFixture";
        int subscriptions = SystemThemeMonitor.ActiveSubscriptions;
        foreach (string language in new[] { "en", "ar" })
            foreach (string scenario in new[] { "single", "playlist", "close-success", "close-cancel", "close-recovery" })
            {
                var repository = new SettingsRepository(System.IO.Path.Combine(root, language + "-" + scenario));
                var provider = new FakeProvider();
                var service = new DownloadService(provider, new EmptyStorage());
                var child = new MainWindow(service, repository, new FixtureThumbnailLoader()) { ShowActivated = false };
                var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                child.Closed += (_, _) => closed.TrySetResult();
                child.Show();
                var model = (MainViewModel)child.DataContext;
                try
                {
                    model.SelectLanguageCommand.Execute(language);
                    model.Folder = repository.FilePath + "-downloads";
                    System.IO.Directory.CreateDirectory(model.Folder);
                    model.Url = scenario == "playlist" ? playlist : single;
                    await WaitFor(() => model.CanDownload && model.PreviewImage != null, "Close fixture has inspected media and artwork");
                    if (scenario == "close-recovery")
                    {
                        provider.FailDispose = true;
                        try { await model.CloseAsync(); throw new Exception("The shutdown failure was not injected."); }
                        catch (System.IO.IOException) { Check(true, "The failed provider shutdown is reported"); }
                        Check(child.IsVisible, "An interrupted shutdown leaves the fixture available for recovery");
                        model.Url = "";
                        Check(model.Url == "", "Thumbnail changes after an interrupted close do not touch a disposed source");
                        continue;
                    }
                    var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    provider.DownloadHandler = (request, token, _) =>
                    {
                        started.TrySetResult();
                        if (scenario.StartsWith("close-", StringComparison.Ordinal))
                        {
                            if (!token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5))) throw new TimeoutException("Close did not cancel the active fixture.");
                            if (scenario == "close-cancel") token.ThrowIfCancellationRequested();
                        }
                        var file = System.IO.Path.Combine(request.Folder, "result.mp4");
                        System.IO.File.WriteAllText(file, "Local fixture, never opened as media.");
                        return new DownloadResult { FilePath = file };
                    };
                    model.DownloadCommand.Execute(null);
                    await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
                    if (scenario.StartsWith("close-", StringComparison.Ordinal))
                    {
                        child.Close();
                        await closed.Task.WaitAsync(TimeSpan.FromSeconds(8));
                    }
                    else await model.DownloadCommand.ExecutionTask;
                    if (scenario.StartsWith("close-", StringComparison.Ordinal))
                    {
                        int calls = provider.DownloadCalls;
                        var receipt = model.ReceiveBrowserRequest(new(2, "download", single, "closing_start", Output: new("video", "mp4", 720)));
                        Check(!receipt.Accepted && receipt.Code == "busy" && provider.DownloadCalls == calls, "A closing desktop rejects new browser starts without creating a job");
                    }
                    bool cancelled = scenario == "close-cancel";
                    Check(model.HasFile == !cancelled && model.RecentDownloadCount == (cancelled ? 0 : 1), "Completion/cancellation retains exactly the expected output history in " + language + " " + scenario);
                    Check(model.DownloadActionState == (cancelled ? DownloadActionState.Ready : DownloadActionState.Complete), "Closing never turns a successful result into a failed download");
                    if (!cancelled)
                    {
                        Check(model.OpenFileCommand.CanExecute(null) && model.RevealCommand.CanExecute(null), "Successful output still exposes both file actions");
                        Check(repository.Load().Recent.Count == 1, "Successful file history survives settings reload");
                    }
                }
                finally
                {
                    provider.FailDispose = false;
                    if (child.IsVisible) child.Close();
                    if (await Task.WhenAny(closed.Task, Task.Delay(1500)) != closed.Task)
                    {
                        // A deliberately broken shutdown must not keep the entire test process alive.
                        var handler = typeof(MainWindow).GetMethod("ClosingAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                            .CreateDelegate<System.ComponentModel.CancelEventHandler>(child);
                        child.Closing -= handler;
                        child.Close();
                    }
                    await closed.Task.WaitAsync(TimeSpan.FromSeconds(3));
                }
            }
        Check(SystemThemeMonitor.ActiveSubscriptions == subscriptions, "Closed fixture windows release all system-theme subscriptions");
    }

    private static async Task WaitFor(Func<bool> condition, string message)
    {
        for (int attempt = 0; attempt < 150 && !condition(); attempt++) await Task.Delay(30);
        Check(condition(), message);
    }
}
