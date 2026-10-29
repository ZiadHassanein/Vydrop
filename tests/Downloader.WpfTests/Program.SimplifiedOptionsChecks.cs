using System.IO;
using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckSimplifiedOptions(string captures)
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        Directory.CreateDirectory(captures);
        string root = Path.Combine(AppContext.BaseDirectory, "simplified-options-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); PrepareLibraryQueue(root);
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var model = new MainViewModel(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        var original = model.Queue.Add(new() { Url = "https://example.com/old.mp4", Folder = root });
        model.DownloadConnections = 8; model.QueueAttempts = 3; model.DirectFileAcceleration = true;
        foreach (var (language, dark, width) in new[] { ("en", false, 760d), ("ar", true, 760d), ("ar", false, 450d), ("en", true, 450d) })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            model.IsSpeedLimited = false;
            var preferences = new DownloadLimitsView { DataContext = model, FontSize = 14, FlowDirection = model.ContentDirection };
            var advanced = (Expander)WpfTree.Find(preferences, "AdvancedDownloadOptions");
            Check(!advanced.IsExpanded, "Advanced connection/retry controls start collapsed: " + language);
            CaptureOptions(preferences, $"speed-{language}-{dark}-{width}.png", width, 350);
            model.IsSpeedLimited = true; model.SelectedSpeedPreset = 2048;
            CaptureOptions(preferences, $"speed-limited-{language}-{dark}-{width}.png", width, 420);
            advanced.IsExpanded = true;
            CaptureOptions(preferences, $"advanced-{language}-{dark}-{width}.png", width, 700);
            Check(model.DownloadConnections == 8 && model.QueueAttempts == 3 && model.DirectFileAcceleration && model.SpeedLimitKiB == 2048,
                "Disclosure, language and theme retain all existing preference values");
            var composer = new QueueComposerView { DataContext = model, FontSize = 14, FlowDirection = model.ContentDirection };
            composer.SetResourceReference(Control.ForegroundProperty, "Ink");
            Check(composer.FindName("ScheduleOptions") == null && !Descendants<DatePicker>(composer).Any() && !Descendants<DownloadLimitsView>(composer).Any(),
                "Add to queue contains links without scheduling or duplicate speed controls");
            CaptureOptions(composer, $"queue-{language}-{dark}-{width}.png", width, 700);
            Check(ReferenceEquals(composer.QueueStartButton.Command, model.QueueDownloadCommand) &&
                ReferenceEquals(composer.AddQueueLinksButton.Command, model.AddLinksCommand),
                "Adding links and starting/resuming the queue retain their existing commands");
        }
        Check(original.Options.Connections == 1 && original.Options.BytesPerSecond == 0 && original.Options.MaximumAttempts == 2,
            "Existing queued work keeps its captured download options");
        model.SelectLanguageCommand.Execute("en");
        // Simulate a queue written by a version that offered scheduled downloads.
        var legacyStart = DateTimeOffset.UtcNow.AddDays(1);
        model.Queue.Schedule(legacyStart, legacyStart.AddHours(1));
        var storage = new LocalQueueStorage(root);
        string savedJobs = System.Text.Json.JsonSerializer.Serialize(storage.Load().Jobs);
        var restoredProvider = new FakeProvider();
        using var restoredService = new DownloadService(restoredProvider, new EmptyStorage());
        var restored = new MainViewModel(restoredService, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        Check(!restored.Queue.StartAt.HasValue && !restored.Queue.StopAt.HasValue && storage.Load().StartAt == null && storage.Load().StopAt == null,
            "Opening an older queue retires and persists both legacy timers");
        Check(System.Text.Json.JsonSerializer.Serialize(storage.Load().Jobs) == savedJobs &&
            restored.Queue.Entries.Select(job => job.Id).SequenceEqual(model.Queue.Entries.Select(job => job.Id)),
            "Removing scheduling preserves saved jobs, order, states, completed file paths and per-job settings");
        Check(restoredProvider.DownloadCalls == 0 && !restored.Queue.IsRunning && restored.DownloadConnections == 8 &&
            restored.QueueAttempts == 3 && restored.DirectFileAcceleration && restored.SpeedLimitKiB == 2048,
            "Legacy schedules never start work and download preferences remain unchanged");
        app.Shutdown();

        void CaptureOptions(FrameworkElement view, string name, double width, double height)
        {
            var frame = new Border { Padding = new Thickness(20), Child = view };
            frame.SetResourceReference(Border.BackgroundProperty, "Page");
            frame.Measure(new Size(width, height));
            if (view is DownloadLimitsView) height = Math.Min(height, frame.DesiredSize.Height);
            frame.Arrange(new Rect(0, 0, width, height)); frame.UpdateLayout();
            Check(frame.DesiredSize.Width <= width + 1, "Options fit the available workspace width: " + name);
            CaptureVisual(frame, Path.Combine(captures, name));
            frame.Child = null;
        }
    }
}
