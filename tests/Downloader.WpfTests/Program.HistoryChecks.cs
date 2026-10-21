using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckRecentDownloads(Window owner, MainViewModel model, string captures)
    {
        var main = (MainWindow)owner;
        string root = System.IO.Path.Combine(captures, "history-fixtures"); System.IO.Directory.CreateDirectory(root);
        var files = Enumerable.Range(1, 7).Select(i => System.IO.Path.GetFullPath(System.IO.Path.Combine(root, i + " — Local test media — عنوان عربي طويل.mp3"))).ToArray();
        foreach (var file in files.Take(4)) System.IO.File.WriteAllBytes(file, new byte[2048]);
        double originalHeight = owner.Height;
        foreach (string locale in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true }) foreach (int entries in new[] { 0, 1, 5, 7 })
        {
            Check(model.IsIdle && model.SelectLanguageCommand.CanExecute(locale), "History locale fixtures only switch language while idle");
            model.SelectLanguageCommand.Execute(locale); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            owner.Height = entries == 7 ? 500 : originalHeight;
            model.HistoryCommand.Execute(null);
            var history = new RecentDownloadsViewModel(files.Take(entries), key => Texts.For(locale, key), path => new DesktopActions(owner).OpenMedia(path, true));
            var view = main.ManagerContent.HistoryContent; view.SetCurrentValue(FrameworkElement.DataContextProperty, history);
            await Task.Delay(120); owner.UpdateLayout();
            Check(model.HistoryVisible && view.IsVisible && !main.EditorOverlay.IsVisible && owner.OwnedWindows.Count == 0, "History occupies the center workspace without a modal: " + locale + "/" + dark + "/" + entries);
            Check(history.Files.Count == Math.Min(entries, 5) && (entries != 0 || history.IsEmpty), "History preserves its five-file limit and empty state");
            var scroll = Descendants<ScrollViewer>(view.HistoryList).Single();
            Check(scroll.ScrollableWidth < 1, "Long bilingual history names stay inside the center panel");
            if (entries > 0)
            {
                var reveals = Descendants<Button>(view.HistoryList).ToArray();
                Check(reveals.Length == Math.Min(entries, 5) && reveals.All(button => button.IsEnabled == ((RecentDownloadView)button.DataContext).Available), "History reveal actions follow actual file availability");
                if (entries == 5)
                {
                    owner.Activate(); await Task.Delay(60);
                    Check(reveals.Last(button => button.IsEnabled).Focus(), "The keyboard traversal fixture starts on the final available History action");
                    for (int tab = 0; tab < 12 && System.Windows.Input.Keyboard.FocusedElement is DependencyObject focused && view.IsAncestorOf(focused); tab++)
                        ((UIElement)System.Windows.Input.Keyboard.FocusedElement).MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next));
                    Check(System.Windows.Input.Keyboard.FocusedElement is DependencyObject next && !view.IsAncestorOf(next), "Tab can leave History for workspace navigation (focused: " + System.Windows.Input.Keyboard.FocusedElement + ")");
                }
                if (entries == 1)
                {
                    System.IO.File.Delete(files[0]); history.RefreshFiles(); await Task.Delay(40); owner.UpdateLayout();
                    var title = Descendants<TextBlock>(view.HistoryList).Single(text => text.Text == history.Files[0].Title);
                    Check(!history.Files[0].Available && !reveals[0].IsEnabled && title.TextDecorations.Contains(TextDecorations.Strikethrough[0]), "A deleted history file is struck through and its reveal action disables");
                    System.IO.File.WriteAllBytes(files[0], new byte[2048]);
                }
            }
            Capture(owner, System.IO.Path.Combine(captures, "history-" + locale + (dark ? "-dark-" : "-light-") + entries + ".png"));
        }
        owner.Height = originalHeight; model.SelectLanguageCommand.Execute("en"); model.NewDownloadCommand.Execute(null);
    }

    private static async Task CheckHorizontalScrolling(Window owner, string captures)
    {
        var viewport = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border { Width = 1200, Height = 1000, Background = (Brush)Application.Current.Resources["Surface"] }
        };
        var dialog = new Window { Owner = owner, Width = 440, Height = 320, Content = viewport, ShowInTaskbar = false };
        try
        {
            dialog.Show();
            await Task.Delay(150);
            dialog.UpdateLayout();
            var horizontal = Descendants<ScrollBar>(viewport).Single(bar => bar.Orientation == Orientation.Horizontal);
            var track = (Track)horizontal.Template.FindName("PART_Track", horizontal);
            Check(track.Orientation == Orientation.Horizontal && horizontal.ActualWidth > 300 && horizontal.ActualHeight <= 16 &&
                track.Thumb.ActualWidth >= 28 && track.Thumb.ActualHeight <= 16,
                "An actual horizontal scrollbar renders a horizontal thumb instead of a vertical dot");
            double before = viewport.HorizontalOffset;
            ScrollBar.PageRightCommand.Execute(null, horizontal);
            await Task.Delay(75);
            Check(viewport.HorizontalOffset > before, "Horizontal page-right moves content along the horizontal axis");
            ScrollBar.PageLeftCommand.Execute(null, horizontal);
            await Task.Delay(75);
            Check(viewport.HorizontalOffset < viewport.ScrollableWidth && viewport.VerticalOffset == 0,
                "Horizontal page-left does not move content vertically");
            Capture(dialog, System.IO.Path.Combine(captures, "horizontal-scrollbar.png"));
        }
        finally { dialog.Close(); }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }
}
