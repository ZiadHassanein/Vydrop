using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckTopNavigation(string captures)
    {
        // Real WPF layout and commands, rendered offscreen without opening or focusing a window.
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        Directory.CreateDirectory(captures);
        string root = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); PrepareLibraryQueue(root);
        MotionPreferences.SetTestOverride(false);
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var model = new MainViewModel(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        int savedJobs = model.Queue.Entries.Count;
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
            foreach (var (width, height) in new[] { (760, 500), (980, 740), (1280, 820) })
            {
                model.SelectLanguageCommand.Execute(language);
                if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                model.LibraryFilterCommand.Execute("queue");
                var view = new DownloadManagerView { DataContext = model, FontSize = 14 };
                view.SetResourceReference(Control.ForegroundProperty, "Ink");
                void Layout()
                {
                    view.Measure(new Size(width, height)); view.Arrange(new Rect(0, 0, width, height)); view.UpdateLayout();
                }
                Layout();
                Rect Bounds(FrameworkElement element) => element.TransformToAncestor(view).TransformBounds(new Rect(element.RenderSize));
                string variant = $"{language}-{(dark ? "dark" : "light")}-{width}";
                var navigation = Bounds(view.NavigationSurface); var body = Bounds(view.WorkspaceViewport);
                Check(navigation.Left >= -1 && navigation.Right <= width + 1 &&
                    navigation.Bottom <= body.Top + 1 && body.Top > 0 && body.Top <= 70 && body.Height >= height - 70,
                    $"The single toolbar stays above the usable workspace: {variant} (navigation={navigation}, body={body})");
                Check(Math.Abs(body.Width - (width - view.DetailsColumn.Width.Value)) <= 1 && body.Left >= -1 &&
                    (width >= 1080 ? body.Width >= 950 : body.Width >= width - 1),
                    "The workspace uses the freed sidebar width: " + variant);
                var rowButtons = Descendants<Button>(view.DownloadsList).ToArray();
                Check(!rowButtons.Any(button => button.Name is "RowMoveUp" or "RowMoveDown") &&
                    rowButtons.Any(button => button.Name == "RowPrimaryAction") && rowButtons.Any(button => button.Name == "RowFolder") && rowButtons.Any(button => button.Name == "RowRemove"),
                    "Rows retain their download and file actions without reorder arrows: " + variant);
                var actions = view.NavigationItems.Children.OfType<ToggleButton>().ToArray();
                Check(actions.Length == 5 && actions.All(action => action.ActualHeight >= 44 && Bounds(action).Left >= navigation.Left - 1 &&
                    Bounds(action).Right <= navigation.Right + 1 && Bounds(action).Top >= navigation.Top - 1 && Bounds(action).Bottom <= navigation.Bottom + 1),
                    $"All five actions fit beside the logo at full click size: {variant}; compact={view.IsNavigationCompact}; actions={string.Join(";", actions.Select(action => Bounds(action).ToString(CultureInfo.InvariantCulture)))}");
                Check(actions.SelectMany((left, index) => actions.Skip(index + 1).Select(right => Bounds(left).IntersectsWith(Bounds(right)))).All(overlap => !overlap),
                    "Navigation actions never overlap: " + variant);
                Check(actions.Max(action => Bounds(action).Top) - actions.Min(action => Bounds(action).Top) <= 1 && navigation.Height <= 64,
                    "All five actions remain in exactly one row: " + variant);
                Check(model.IsArabic ? HeaderBounds(actions[0]).Left > HeaderBounds(actions[^1]).Left : HeaderBounds(actions[0]).Left < HeaderBounds(actions[^1]).Left,
                    "Page order follows the interface direction beside the logo: " + variant);
                Check(actions.All(action => !string.IsNullOrWhiteSpace(AutomationProperties.GetName(action)) && action.ToolTip is string tooltip && !string.IsNullOrWhiteSpace(tooltip)),
                    "Every action has a localized tooltip and accessible name: " + variant);
                Check(view.IsNavigationCompact ? actions.All(action => action.Template.FindName("NavigationLabel", action) is ContentPresenter { Visibility: Visibility.Collapsed } && Math.Abs(action.ActualWidth - 44) <= 1) :
                    Descendants<TextBlock>(view.NavigationItems).All(text =>
                    new FormattedText(text.Text, CultureInfo.InvariantCulture, text.FlowDirection,
                        new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch), text.FontSize, text.Foreground,
                        VisualTreeHelper.GetDpi(text).PixelsPerDip).WidthIncludingTrailingWhitespace <= text.ActualWidth + 1),
                    "Compact navigation shows icons only; wide labels and counters fit without truncation: " + variant);
                Check(width < 900 ? view.IsNavigationCompact : !view.IsNavigationCompact,
                    "Names stay visible whenever the toolbar has room: " + variant);
                Rect HeaderBounds(FrameworkElement element) => element.TransformToAncestor(view.Header).TransformBounds(new Rect(element.RenderSize));
                var brand = HeaderBounds(view.Header.BrandButton); var preferences = HeaderBounds(view.Header.HeaderActions);
                var toolbarNavigation = HeaderBounds(view.NavigationSurface);
                Check(!brand.IntersectsWith(preferences) && brand.Left >= 0 && brand.Right <= width && preferences.Left >= 0 && preferences.Right <= width &&
                    view.Header.BrandWordmark.FlowDirection == FlowDirection.LeftToRight &&
                    Math.Abs(brand.Top + brand.Height / 2 - toolbarNavigation.Top - toolbarNavigation.Height / 2) <= 4 &&
                    (language == "ar" ? preferences.Right <= toolbarNavigation.Left + 1 && toolbarNavigation.Right <= brand.Left + 1 : brand.Right <= toolbarNavigation.Left + 1 && toolbarNavigation.Right <= preferences.Left + 1),
                    "Logo, navigation and preferences share one row without overlap: " + variant);
                Check(((System.Windows.Shapes.Path)actions[2].Template.FindName("NavigationGlyph", actions[2])).Stroke == (Brush)app.Resources["NavigationActiveInk"] &&
                    actions.All(action => action.Template.FindName("NavigationSelection", action) == null && action.Template.FindName("Face", action) == null),
                    "The selected icon is accented without button backgrounds or outlines: " + variant);
                Check(ReferenceEquals(actions[0].Command, model.NewDownloadCommand) && ReferenceEquals(actions[1].Command, model.LibraryFilterCommand) &&
                    ReferenceEquals(actions[2].Command, model.LibraryFilterCommand) && ReferenceEquals(actions[3].Command, model.HistoryCommand) &&
                    ReferenceEquals(actions[4].Command, model.SettingsCommand),
                    "The five page actions retain their existing commands: " + variant);
                foreach (var (index, active) in new (int, Func<bool>)[]
                {
                    (0, () => model.IsAddDownloadPage), (1, () => model.IsLibraryPage), (2, () => model.IsLibraryQueue),
                    (3, () => model.HistoryVisible), (4, () => model.SettingsVisible)
                })
                {
                    actions[index].Command.Execute(actions[index].CommandParameter); Layout();
                    Check(active() && actions[index].IsChecked == true && model.Queue.Entries.Count == savedJobs,
                        "Navigation changes workspace without changing saved jobs: " + index + "/" + variant);
                }
                model.LibraryFilterCommand.Execute("video"); Layout();
                Check(actions[1].IsChecked == true && model.LibraryItems.Cast<QueueItemView>().Count() == 2,
                    "Video filtering keeps Library active: " + variant);
                model.LibraryFilterCommand.Execute("audio"); Layout();
                Check(actions[1].IsChecked == true && model.LibraryItems.Cast<QueueItemView>().Single().IsAudio,
                    "Audio filtering keeps Library active: " + variant);
                var settingsView = new SettingsView { DataContext = model };
                settingsView.Measure(new Size(800, 600)); settingsView.Arrange(new Rect(0, 0, 800, 600)); settingsView.UpdateLayout();
                Check(ReferenceEquals(settingsView.BrowserIntegrationButton.Command, model.BrowserCommand),
                    "Browser integration is available through Settings: " + variant);
                model.OpenWorkspace(WorkspacePage.BrowserIntegration); Layout();
                Check(model.IsSettingsPage && actions[4].IsChecked == true,
                    "Browser integration keeps its parent Settings action selected: " + variant);
                model.LibraryFilterCommand.Execute("queue"); Layout();
                CaptureVisual(view, Path.Combine(captures, "top-navigation-" + variant + ".png"));
                if (width >= 1080)
                {
                    Guid selected = model.SelectedLibraryItem.Id;
                    view.Measure(new Size(760, 500)); view.Arrange(new Rect(0, 0, 760, 500)); view.UpdateLayout();
                    Check(view.IsNavigationCompact && actions.All(action => Math.Abs(action.ActualWidth - 44) <= 1 &&
                        action.Template.FindName("NavigationLabel", action) is ContentPresenter { Visibility: Visibility.Collapsed }) &&
                        actions.Max(action => Bounds(action).Top) - actions.Min(action => Bounds(action).Top) <= 1,
                        "Shrinking the same workspace switches to one row of icons: " + variant);
                    Layout();
                    Check(!view.IsNavigationCompact && actions.All(action => action.Template.FindName("NavigationLabel", action) is ContentPresenter { Visibility: Visibility.Visible }) &&
                        actions.Max(action => Bounds(action).Top) - actions.Min(action => Bounds(action).Top) <= 1,
                        "Widening restores labels in the same single row: " + variant);
                    Check(model.SelectedLibraryItem.Id == selected && model.Queue.Entries.Count == savedJobs,
                        "Resizing preserves selection and all saved jobs: " + variant);
                }
            }
        MotionPreferences.SetTestOverride(null); app.Shutdown();
    }
}
