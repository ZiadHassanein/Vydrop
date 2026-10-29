using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckFormPolish(string captures)
    {
        // Compose the real app offscreen. No window is shown and no desktop input is sent.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        // A hidden fixture has no Application.Run loop to install WPF's UI context.
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        MotionPreferences.SetTestOverride(false);
        Directory.CreateDirectory(captures);
        string state = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
        var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var window = new MainWindow(service, new SettingsRepository(state), new FixtureThumbnailLoader());
        var model = (MainViewModel)window.DataContext;
        var root = (FrameworkElement)window.Content;
        var form = window.DownloadForm;
        model.Folder = Path.Combine(state, "media"); Directory.CreateDirectory(model.Folder);
        string savedFolder = model.Folder;
        foreach (var (language, dark, width, ready) in new[]
        {
            ("en", true, 980, false), ("en", false, 1280, true),
            ("ar", true, 760, true), ("ar", false, 980, false)
        })
        {
            model.Url = "";
            model.SelectLanguageCommand.Execute(language);
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            model.NewDownloadCommand.Execute(null);
            if (ready)
            {
                model.Url = "https://youtu.be/jNQXAC9IVRw";
                DateTime deadline = DateTime.UtcNow.AddSeconds(8);
                while (!model.CanDownload && DateTime.UtcNow < deadline)
                {
                    var frame = new DispatcherFrame();
                    var pulse = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(20) };
                    pulse.Tick += (_, _) => { pulse.Stop(); frame.Continue = false; };
                    pulse.Start(); Dispatcher.PushFrame(frame);
                }
                Check(model.CanDownload && model.HasMediaPreview, $"The polished form retains real fixture inspection and output selection (ready={model.CanDownload}, preview={model.HasMediaPreview}, idle={model.IsIdle}, checking={model.IsCheckingMedia}, status={model.StatusHeading}, detail={model.StatusDetail})");
                model.Audio = language == "ar";
            }
            void Layout()
            {
                root.Measure(new Size(width, 740)); root.Arrange(new Rect(0, 0, width, 740)); root.UpdateLayout();
            }
            Layout();
            Rect Bounds(FrameworkElement element) => element.TransformToAncestor(form).TransformBounds(new Rect(element.RenderSize));
            Check(form.LinkInput.ActualWidth > 200 && Bounds(form.LinkInput).Right <= form.ActualWidth + 1 &&
                Bounds(form.PasteButton).Right <= form.ActualWidth + 1 && !Bounds(form.LinkInput).IntersectsWith(Bounds(form.PasteButton)),
                "The URL and Paste controls remain usable and do not overlap");
            Check(ReferenceEquals(form.PasteButton.Command, model.PasteCommand) && ReferenceEquals(form.DestinationPicker.Command, model.BrowseCommand) &&
                ReferenceEquals(form.DownloadAction.DownloadButton.Command, model.DownloadActionCommand),
                "The redesigned controls retain paste, destination and download behavior");
            Check(form.VideoChoice.IsChecked == !model.Audio && form.AudioChoice.IsChecked == model.Audio && model.Folder == savedFolder,
                "Video/audio selection and the saved destination survive the visual changes");
            double center = form.DownloadAction.TranslatePoint(new Point(form.DownloadAction.ActualWidth / 2, 0), form.FormActions).X;
            Check(Math.Abs(center - form.FormActions.ActualWidth / 2) <= 1 && form.DownloadAction.DownloadButton.ActualHeight >= 44,
                "Download remains centered with a full click target");
            Check(window.EditorHeading.FontSize <= 28 && model.Queue.Entries.Count == 0,
                "Page headings stay compact and previewing the form never queues a download");
            CaptureVisual(root, Path.Combine(captures, $"add-download-{language}-{(dark ? "dark" : "light")}-{width}-{(ready ? "ready" : "empty")}.png"));
            if (ready)
            {
                form.ClearLinkButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Layout();
                Check(model.Url == "" && form.ClearLinkButton.Visibility == Visibility.Collapsed && !model.CanDownload && model.Queue.Entries.Count == 0,
                    "Clear link resets the inspected source without changing downloads");
            }
        }
        MotionPreferences.SetTestOverride(null); app.Shutdown();
    }
}
