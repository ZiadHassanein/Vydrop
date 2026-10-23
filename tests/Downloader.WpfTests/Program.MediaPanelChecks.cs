using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckMediaPanel(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        var original = (window.Width, window.Height, window.Left, window.Top, model.Folder, model.Audio, model.IsArabic, model.IsDark);
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        var context = (ScrollViewer)WpfTree.Find(window, "ContextViewport");
        var form = (FrameworkElement)WpfTree.Find(window, "DownloadForm");
        var preview = (FrameworkElement)WpfTree.Find(window, "MediaPreview");
        var heading = (FrameworkElement)WpfTree.Find(window, "EditorHeading");
        var inline = (FrameworkElement)WpfTree.Find(form, "InlineActivity");
        string folder = Path.GetFullPath(Path.Combine(captures, "media-fixtures"));
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "Owned UI fixture.mp4");
        File.WriteAllText(file, "Synthetic UI fixture; not playable media.");
        Rect Bounds(FrameworkElement view) => view.TransformToAncestor(window).TransformBounds(new Rect(view.RenderSize));
        async Task Settle() { await Task.Delay(250); window.UpdateLayout(); }
        async Task Input(string url) { model.Url = url; await Task.Delay(900); await Settle(); Check(model.CanDownload, "Media inspection completes: " + url); }
        void HeadingFirst(string state)
        {
            viewport.ScrollToTop(); window.UpdateLayout();
            Check(heading.IsVisible && Bounds(heading).Top < Bounds((FrameworkElement)WpfTree.Find(form, "LinkInput")).Top,
                "New download is the first form content with no title or status banner above it: " + state);
        }
        try
        {
            model.NewDownloadCommand.Execute(null); model.Url = ""; model.Audio = true; model.Folder = folder;
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            window.Width = 740; await Settle();
            double compactWidth = window.Width;
            var work = window.CurrentWorkArea();
            window.Left = work.Left + 20;
            double left = window.Left, top = window.Top, height = window.Height;
            foreach (string locale in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    await Input("https://www.youtube.com/shorts/jNQXAC9IVRw?si=fixture");
                    Check(window.Width == compactWidth && Math.Abs(window.Top - top) <= 1 && Math.Abs(window.Height - height) <= 1 &&
                        model.InlineMediaPreview && preview.IsVisible && !context.IsVisible,
                        "Shorts use the inline compact preview without changing the window size: " + locale + "/" + dark + $" width={window.Width}, top={top}->{window.Top}, height={height}->{window.Height}, inline={model.InlineMediaPreview}, preview={preview.IsVisible}, context={context.IsVisible}, add={model.AddDownloadVisible}");
                    var image = (Image)WpfTree.Find(preview, "ThumbnailImage");
                    Check(image.IsVisible && image.Source != null && image.Stretch == Stretch.Uniform && model.PreviewDuration == "8:24",
                        "Shorts retain the same real thumbnail, duration and uncropped artwork treatment as video");
                    HeadingFirst("Short " + locale);
                    Capture(window, Path.Combine(captures, "media-expanded-" + locale + (dark ? "-dark" : "-light") + ".png"));
                    model.Url = ""; await Settle();
                    Check(Math.Abs(window.Width - compactWidth) <= 1 && Math.Abs(window.Left - left) <= 1 && !context.IsVisible,
                        "Clearing media restores the original narrow frame and removes the context pane");
                }
            window.Width = 1280;
            context = Descendants<ScrollViewer>(window.ManagerContent.DetailsBorder).First();
            await Input("https://www.youtube.com/watch?v=YKMJXdNBvNQ&list=RDYKMJXdNBvNQ&start_radio=1");
            model.WholePlaylist = true; await Task.Delay(900); await Settle();
            Check(model.HasPlaylist && model.CanDownload && model.PlaylistLabel.Contains("50") && model.PreviewImage != null,
                "A Mix has a visible bounded playlist choice, actual entries and artwork");
            HeadingFirst("playlist");
            Capture(window, Path.Combine(captures, "media-mix-ar-light.png"));
            // The context scroll must not shift the form's heading or its separate scroll offset.
            double headingTop = Bounds(heading).Top;
            context.ScrollToEnd(); await Settle();
            Check(Math.Abs(Bounds(heading).Top - headingTop) <= 1 && viewport.VerticalOffset == 0,
                "Scrolling media details cannot scroll the download form");
            model.Url = "";
            model.SelectLanguageCommand.Execute("en");
            if (!model.IsDark) model.ThemeCommand.Execute(null);
            await Input("https://youtu.be/jNQXAC9IVRw");
            context.ScrollToTop(); await Settle();
            provider.DownloadDelayMilliseconds = 5000;
            var running = model.DownloadCommand.ExecuteAsync(null);
            await Task.Delay(150); await Settle();
            Check(model.IsBusy && window.ManagerContent.DownloadsList.IsVisible && Descendants<ProgressBar>(window.ManagerContent.DownloadsList).Any(bar => bar.IsVisible),
                "Active progress appears in the actual Queue row");
            Check(model.QueueItems.Single().Entry.State == QueueState.Downloading && model.QueueItems.Single().Percent > 0,
                "A main-window download forwards its actual progress into its durable queue job");
            Check(model.IsQueuePage && !model.AddDownloadVisible, "Starting a download opens its Queue workspace");
            Capture(window, Path.Combine(captures, "media-progress-en-dark.png"));
            model.CancelCommand.Execute(null); await running; model.NewDownloadCommand.Execute(null); await Settle();
            Check(model.Url.Length > 0 && context.IsVisible && window.Width > 740 && model.IsIdle,
                "Cancellation keeps the link and context available for retry");
            provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = file, Files = new() { file }, Total = 1, Completed = 1 };
            await model.DownloadCommand.ExecuteAsync(null); model.NewDownloadCommand.Execute(null); await Settle();
            Check(model.Url == "" && window.ManagerContent.DetailsColumn.Width.Value == 0 && !context.IsVisible && !inline.IsVisible && model.HasFile && (model.RecentDownloadCount > 0),
                "Successful download collapses the pane and signals completion in recent downloads");
            HeadingFirst("completed");
            Check(!inline.IsVisible, "Routine completion does not add a block below the main action");
            Capture(window, Path.Combine(captures, "media-completed-en-dark.png"));
            model.Url = "invalid next link"; await Task.Delay(700);
            Check(!model.HasFile, "A new invalid link cannot expose the previous download's file actions");
            await Input("https://youtu.be/jNQXAC9IVRw");
            window.Width = 820; await Settle();
            Check(model.InlineMediaPreview && preview.Parent == WpfTree.Find(form, "InlinePreviewHost") &&
                Bounds(preview).Top > Bounds((FrameworkElement)WpfTree.Find(form, "LinkInput")).Bottom &&
                ((Image)WpfTree.Find(preview, "ThumbnailImage")).IsVisible,
                "Manual narrowing retains thumbnail and details directly below the link");
            HeadingFirst("manual narrow fallback");
            Capture(window, Path.Combine(captures, "media-manual-narrow.png"));
            model.Url = ""; await Settle();
            Check(Math.Abs(window.Width - 820) <= 1, "Automatic collapse respects an explicit manual resize");
            window.Width = 740;
            window.Left = work.Right - 760;
            double edgeLeft = window.Left;
            await Input("https://youtu.be/jNQXAC9IVRw");
            Check(window.Left >= work.Left && window.Left + window.Width <= work.Right && Math.Abs(window.Left - edgeLeft) <= 1,
                "Inline preview near the screen edge preserves the window position within the work area");
            model.Url = ""; await Settle();
            Check(Math.Abs(window.Left - edgeLeft) <= 1 && window.Width >= window.MinWidth,
                "Collapse restores the original position after screen-edge containment");
            window.Left = left;
            Check(model.QueueItems.Count == 2 && model.QueueItems.Any(item => item.Entry.State == QueueState.Cancelled) && model.QueueItems.Any(item => item.HasCompletedOutput),
                "Main-window downloads retain their cancelled and completed jobs for durable queue recovery");
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            await Input("https://www.youtube.com/shorts/jNQXAC9IVRw");
            model.AddQueueCommand.Execute(null); model.DismissNoticeCommand.Execute(null);
            await model.QueueStartCommand.ExecuteAsync(null); await Settle();
            Check(model.QueueItems.Single().HasCompletedOutput && model.Url == "" && window.Width >= window.MinWidth && !context.IsVisible,
                "Queue completion matches the canonical Short URL and restores the narrow frame");
            foreach (string locale in new[] { "en", "ar" })
            {
                model.SelectLanguageCommand.Execute(locale);
                model.LibraryFilterCommand.Execute("all");
                await Settle();
                var completed = model.LibraryItems.Cast<QueueItemView>().Single();
                Check(completed.IsComplete && completed.CompletedFolder == folder && model.OpenQueueFolderCommand.CanExecute(completed.Id),
                    "Completed outputs retain their real folder action in Library: " + locale);
                await InspectQueueModal(window, model, async dialog =>
                {
                    var jobs = model.QueueItems.Where(row => !row.IsComplete).ToArray();
                    Check(jobs.Length == 0 && !model.HasQueue,
                        "Completed outputs are available in Library without reappearing in queue management");
                    Capture(dialog, Path.Combine(captures, "media-queue-empty-" + locale + ".png"));
                    await Task.CompletedTask;
                });
            }
            File.Delete(file); Directory.Delete(folder);
            Check(!model.OpenQueueFolderCommand.CanExecute(model.QueueItems.Single().Id), "A deleted output folder cannot be opened by a stale queue action");
            model.NewDownloadCommand.Execute(null); await Settle();
            HeadingFirst("queue finished");
        }
        finally
        {
            model.CancelCommand.Execute(null);
            provider.DownloadHandler = null; provider.DownloadDelayMilliseconds = 10000;
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            model.Url = ""; model.SettingsVisible = false; model.Folder = original.Folder; model.Audio = original.Audio;
            model.SelectLanguageCommand.Execute(original.IsArabic ? "ar" : "en");
            if (model.IsDark != original.IsDark) model.ThemeCommand.Execute(null);
            window.Width = original.Width; window.Height = original.Height; window.Left = original.Left; window.Top = original.Top;
        }
    }
}
