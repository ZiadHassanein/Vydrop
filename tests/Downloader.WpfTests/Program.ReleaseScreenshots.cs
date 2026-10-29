using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CaptureReleaseScreenshots(string folder)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        MotionPreferences.SetTestOverride(false);
        Directory.CreateDirectory(folder);
        string state = Path.Combine(AppContext.BaseDirectory, "release-captures-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(state); PrepareLibraryQueue(state);
        var storage = new LocalQueueStorage(state); var samples = storage.Load();
        int videoNumber = 0;
        foreach (var sample in samples.Jobs)
        {
            if (sample.State == QueueState.Completed)
            {
                sample.Title = sample.Options.Audio ? "Example audio" : "Example video " + ++videoNumber;
                foreach (string file in sample.Files)
                {
                    if (!Path.GetFullPath(file).StartsWith(Path.GetFullPath(state) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Screenshot sample files must stay inside their fixture folder.");
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    if (!File.Exists(file)) File.WriteAllBytes(file, new byte[1024]);
                }
            }
            else sample.Title = sample.State switch
            {
                QueueState.Pending => "A new perspective", QueueState.Paused => "Evening notes",
                QueueState.Interrupted => "Project recording", QueueState.Failed => "A short lesson",
                QueueState.Cancelled => "A media example", _ => "A shared clip"
            };
        }
        storage.Save(samples);
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var window = new MainWindow(service, new SettingsRepository(state), new FixtureThumbnailLoader());
        var model = (MainViewModel)window.DataContext;
        model.Folder = Path.Combine(state, "Videos"); Directory.CreateDirectory(model.Folder);
        var root = (FrameworkElement)window.Content;
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            foreach (string view in new[] { "manager", "queue", "download", "setup" })
            {
                if (view == "setup")
                {
                    model.OpenWorkspace(WorkspacePage.BrowserIntegration);
                    window.ManagerContent.WorkspaceHost.Content = new BrowserIntegrationView
                    {
                        DataContext = new BrowserSetupViewModel(key => Texts.For(language, key), new SetupFixture(), language == "ar"),
                        FlowDirection = model.ContentDirection, FontFamily = model.ContentFont
                    };
                }
                else if (view == "download") model.NewDownloadCommand.Execute(null);
                else model.LibraryFilterCommand.Execute(view == "queue" ? "queue" : "all");
                root.Measure(new Size(1280, 761)); root.Arrange(new Rect(0, 0, 1280, 761)); root.UpdateLayout();
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => frame.Continue = false);
                Dispatcher.PushFrame(frame); root.UpdateLayout();
                var bitmap = new RenderTargetBitmap(1280, 761, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(root);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                if (view == "setup" && dark != (language == "en")) continue;
                string path = Path.Combine(folder, view == "setup" ? $"guided-setup-{language}.png" : $"vydrop-{(dark ? "dark" : "light")}-{language}-{view}.png");
                using var output = File.Create(path); encoder.Save(output);
                Console.WriteLine(Path.GetFileName(path));
            }
        }
        MotionPreferences.SetTestOverride(null); app.Shutdown();
    }
}
