using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckLibraryOrder(string captures)
    {
        // Isolated state and unshown native controls keep the user's app and desktop untouched.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        MotionPreferences.SetTestOverride(false);
        Directory.CreateDirectory(captures);
        string root = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); PrepareLibraryQueue(root);
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        MainViewModel LoadModel() => new(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        var legacyModel = LoadModel();
        Check(legacyModel.LibraryItems.Cast<QueueItemView>().Select(row => row.Id).SequenceEqual(
            legacyModel.Queue.Entries.Where(job => job.State == QueueState.Completed).Reverse().Select(job => job.Id)),
            "Existing downloads without dates show the latest saved entry first without losing jobs");

        var store = new LocalQueueStorage(root);
        var snapshot = store.Load();
        var finished = snapshot.Jobs.Where(job => job.State == QueueState.Completed).ToArray();
        finished[0].CompletedAt = DateTimeOffset.UtcNow.AddHours(-3);
        finished[1].CompletedAt = DateTimeOffset.UtcNow.AddHours(-2);
        finished[2].CompletedAt = DateTimeOffset.UtcNow.AddHours(-4);
        store.Save(snapshot);
        var model = LoadModel();
        Guid[] queueOrder = model.Queue.Entries.Select(job => job.Id).ToArray();
        Guid[] Visible() => model.LibraryItems.Cast<QueueItemView>().Select(row => row.Id).ToArray();
        Check(Visible().SequenceEqual(new[] { finished[1].Id, finished[0].Id, finished[2].Id }),
            "Library uses completion dates rather than queue position");
        model.LibraryFilterCommand.Execute("video");
        Check(Visible().SequenceEqual(new[] { finished[0].Id, finished[2].Id }), "Video filtering preserves newest-first order");
        model.LibrarySearch = "Coastal";
        Check(Visible().SequenceEqual(new[] { finished[0].Id }), "Search still finds the matching completed download");
        model.LibrarySearch = ""; model.LibraryFilterCommand.Execute("queue");
        Check(Visible().SequenceEqual(model.Queue.Entries.Where(job => job.State != QueueState.Completed).Select(job => job.Id)),
            "Queue presentation retains its original execution order");

        var pending = model.Queue.Entries.Single(job => job.State == QueueState.Pending);
        model.LibraryFilterCommand.Execute("all");
        var previousSelection = model.SelectedLibraryItem;
        model.Queue.RunJobAsync(pending.Id, (_, _, _) => Task.FromResult(new DownloadResult
        { FilePath = Path.Combine(root, "completed-video.mp4") })).GetAwaiter().GetResult();
        Check(Visible().First() == pending.Id && pending.CompletedAt != null && ReferenceEquals(previousSelection, model.SelectedLibraryItem),
            "An earlier queued job appears first as soon as it completes, without navigation or changing the existing selection");
        var selected = model.SelectedLibraryItem;
        int resets = 0;
        model.LibraryItems.CollectionChanged += (_, args) =>
        { if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) resets++; };
        foreach (var (language, dark, width) in new[] { ("en", false, 1280), ("en", true, 980), ("ar", false, 980), ("ar", true, 1280) })
        {
            model.SelectLanguageCommand.Execute(language);
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            var view = new DownloadManagerView { DataContext = model, FontSize = 14 };
            view.SetResourceReference(Control.ForegroundProperty, "Ink");
            view.Measure(new Size(width, 740)); view.Arrange(new Rect(0, 0, width, 740)); view.UpdateLayout();
            Check(view.AddDownloadButton.Visibility == Visibility.Collapsed && view.AddDownloadButton.ActualWidth == 0 &&
                ReferenceEquals(view.NavigationItems.Children.OfType<ToggleButton>().First().Command, model.NewDownloadCommand),
                "Library has no duplicate header action and keeps Add download in top navigation: " + language + "/" + dark);
            CaptureVisual(view, Path.Combine(captures, $"library-{language}-{(dark ? "dark" : "light")}-{width}.png"));
        }
        Check(resets == 0 && ReferenceEquals(selected, model.SelectedLibraryItem) && Visible().First() == pending.Id,
            "Language and theme changes keep sorted rows and selection stable without resetting the list");
        Check(model.Queue.Entries.Select(job => job.Id).SequenceEqual(queueOrder), "Library sorting never reorders stored queue jobs");
        var restarted = LoadModel();
        Check(restarted.LibraryItems.Cast<QueueItemView>().First().Id == pending.Id && restarted.Queue.Entries.Count == queueOrder.Length,
            "The newest completed download stays first after reopening with every saved job intact");
        model.SelectLanguageCommand.Execute("en"); model.LibraryFilterCommand.Execute("audio");
        Check(Visible().SequenceEqual(new[] { finished[1].Id }), "Audio filtering retains the completed audio item");
        MotionPreferences.SetTestOverride(null); app.Shutdown();
    }
}
