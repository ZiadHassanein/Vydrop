using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckHistoryRetention(string captures)
    {
        // Exercise persistence and real WPF controls without opening or focusing a window.
        var app = new Application();
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        MotionPreferences.SetTestOverride(false);
        captures = Path.GetFullPath(captures); Directory.CreateDirectory(captures);
        string root = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var paths = Enumerable.Range(1, 110).Select(number => Path.Combine(root, $"{number:D3} - Local audio - ملف صوتي.mp3")).ToArray();
        foreach (string path in paths) File.WriteAllBytes(path, new byte[1024]);
        File.Delete(paths[50]);
        var repository = new SettingsRepository(root);
        repository.Save(new SavedState { Theme = "Light", Language = "en", Folder = root, FolderVersion = 1,
            Connections = 4, Recent = new[] { null, "invalid path", paths[0], paths[0].ToUpperInvariant() }.Concat(paths).ToList() });
        var stored = repository.Load();
        Check(stored.Recent.Count == 100 && stored.Recent.SequenceEqual(paths.Take(100)) && stored.Connections == 4,
            "Settings keep the latest 100 distinct valid paths in order and preserve other preferences");
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var model = new MainViewModel(service, repository, new DesktopActions(new Window()), new FixtureThumbnailLoader());
        string latest = Path.Combine(root, "Latest completed video.mp4"); File.WriteAllBytes(latest, new byte[2048]);
        var job = model.Queue.Add(new DownloadOptions { Url = "https://youtu.be/jNQXAC9IVRw", Folder = root });
        // The real desktop completion path must retain 100 entries, not trim them back to five.
        using var downloadService = new DownloadService(new FakeProvider
        { DownloadHandler = (_, _, _) => new DownloadResult { Completed = 1, FilePath = latest } }, new EmptyStorage());
        var downloader = new MainViewModel(downloadService, repository, new DesktopActions(new Window()), new FixtureThumbnailLoader());
        Task download = downloader.LibraryActionCommand.ExecuteAsync(job.Id);
        Until(() => download.IsCompleted); download.GetAwaiter().GetResult();
        downloader.HistoryCommand.Execute(null);
        Check(downloader.History.Files.Count == 100 && downloader.History.Files[0].FilePath == latest &&
            !downloader.History.Files.Any(file => file.FilePath == paths[99]),
            "A new completed download appears first and only the oldest entry beyond 100 is retired");
        Check(repository.Load().Recent.SequenceEqual(downloader.History.Files.Select(file => file.FilePath)),
            "The complete 100-file history is persisted after downloading and opening History");
        var restarted = new MainViewModel(service, repository, new DesktopActions(new Window()), new FixtureThumbnailLoader());
        restarted.HistoryCommand.Execute(null);
        Check(restarted.History.Files.Count == 100 && restarted.History.Files[0].FilePath == latest,
            "Restarting retains the expanded history and newest-first order");
        foreach (var (language, dark, width) in new[] { ("en", false, 1000), ("en", true, 760), ("ar", false, 760), ("ar", true, 1000) })
        {
            restarted.SelectLanguageCommand.Execute(language);
            if (restarted.IsDark != dark) restarted.ThemeCommand.Execute(null);
            restarted.HistoryCommand.Execute(null);
            var history = restarted.History;
            var view = new HistoryView { DataContext = history, FlowDirection = restarted.ContentDirection, FontFamily = restarted.ContentFont, FontSize = 14 };
            void Layout() { view.Measure(new Size(width, 600)); view.Arrange(new Rect(0, 0, width, 600)); view.UpdateLayout(); }
            Layout();
            Check(history.Files.Count == 100 && history.Description.Contains("100", StringComparison.Ordinal) &&
                history.Description == (language == "ar" ? "آخر 100 ملف اكتمل تنزيله." : "Your last 100 completed files."),
                "History shows the real 100-file limit in " + language);
            var missing = history.Files.Single(file => file.FilePath == paths[50]);
            Check(!missing.Available && !history.RevealCommand.CanExecute(missing) && history.RevealCommand.CanExecute(history.Files[0]),
                "Missing entries stay marked and reveal actions follow actual availability");
            var scroll = Descendants<ScrollViewer>(view.HistoryList).First();
            scroll.ScrollToEnd(); Pump(); Layout();
            Check(scroll.VerticalOffset > 0 && scroll.ScrollableHeight > 0 && scroll.ScrollableWidth <= 1,
                "All 100 entries remain reachable through vertical scrolling without horizontal overflow");
            scroll.ScrollToHome(); Pump(); Layout();
            CaptureVisual(view, Path.Combine(captures, $"history-{language}-{(dark ? "dark" : "light")}-{width}.png"));
        }

        string migrationRoot = Path.Combine(captures, "legacy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(migrationRoot);
        var older = Enumerable.Range(1, 10).Select(number => Path.Combine(migrationRoot, $"old-{number:D2}.mp4")).ToArray();
        var snapshot = new QueueSnapshot();
        foreach (string path in older) snapshot.Jobs.Add(new StoredQueueJob
        { Id = Guid.NewGuid(), State = QueueState.Completed, Title = Path.GetFileName(path), Percent = 100,
            Options = new DownloadOptions { Url = "https://example.test/" + snapshot.Jobs.Count, Folder = migrationRoot }, Files = new[] { path } });
        string unfinished = Path.Combine(migrationRoot, "unfinished.mp4");
        snapshot.Jobs.Add(new StoredQueueJob { Id = Guid.NewGuid(), State = QueueState.Failed, Title = "Unfinished",
            Options = new DownloadOptions { Url = "https://example.test/unfinished", Folder = migrationRoot }, Files = new[] { unfinished } });
        new LocalQueueStorage(migrationRoot).Save(snapshot);
        var oldRepository = new SettingsRepository(migrationRoot);
        oldRepository.Save(new SavedState { Folder = migrationRoot, FolderVersion = 1, Recent = older.Reverse().Take(5).ToList() });
        var migrated = new MainViewModel(service, oldRepository, new DesktopActions(new Window()), new FixtureThumbnailLoader());
        migrated.HistoryCommand.Execute(null);
        Check(migrated.History.Files.Select(file => file.FilePath).SequenceEqual(older.Reverse()) &&
            oldRepository.Load().Recent.Count == 10 && !migrated.History.Files.Any(file => file.FilePath == unfinished),
            "Opening an old five-file history restores older completed queue outputs without duplicates or unfinished work");
        migrated.HistoryCommand.Execute(null);
        Check(migrated.History.Files.Count == 10 && migrated.Queue.Entries.Count == 11,
            "Repeated opening is idempotent and preserves all queued jobs");
        MotionPreferences.SetTestOverride(null); app.Shutdown();

        void Until(Func<bool> ready)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!ready() && DateTime.UtcNow < deadline) Pump();
            Check(ready(), "The isolated desktop download finishes within its bounded test window");
        }
        void Pump()
        {
            var frame = new DispatcherFrame();
            var pulse = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(20) };
            pulse.Tick += (_, _) => { pulse.Stop(); frame.Continue = false; }; pulse.Start(); Dispatcher.PushFrame(frame);
        }
    }
}
