using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    // Real current-user transport and a newly launched desktop process; media bytes are a local provider fixture.
    private static async Task<int> CheckBrowserColdStart(string output)
    {
        string root = Path.GetFullPath(output);
        if (Directory.Exists(root)) throw new IOException("Choose a fresh cold-start test directory.");
        Directory.CreateDirectory(root);
        string pipe = "Vydrop.ColdStart." + Guid.NewGuid().ToString("N");
        string previous = BrowserLinkTransport.PipeNameOverride;
        Process child = null;
        BrowserLinkTransport.PipeNameOverride = pipe;
        try
        {
            var repository = new SettingsRepository(root);
            var settings = repository.Load(); settings.Folder = root; repository.Save(settings);
            var queue = new DownloadQueue(new LocalQueueStorage(root));
            var old = queue.Add(new() { Url = "https://youtu.be/jNQXAC9IVRw", Folder = root }, "An older pending job");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            Check(await BrowserLinkTransport.TrySendRequestAsync(new(2, "ping", RequestId: "before_launch"), 100, timeout.Token) == null, "Cold-start fixture begins with no desktop listener");
            int launches = 0;
            var receipt = await BrowserHost.DeliverRequestAsync(new(2, "download", "https://youtu.be/jNQXAC9IVRx", "cold_start", Output: new("video", "webm", 720)), () =>
            {
                launches++;
                var start = new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (string argument in new[] { "--browser-cold-child", root, pipe }) start.ArgumentList.Add(argument);
                child = Process.Start(start);
            }, timeout.Token);
            Check(receipt.Accepted && receipt.Code == "started" && launches == 1, "The real native delivery path launches a closed desktop exactly once and receives a started receipt");
            var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
            await child.WaitForExitAsync(timeout.Token);
            if (child.ExitCode != 0) throw new Exception(await stdout + await stderr);
            var restored = new DownloadQueue(new LocalQueueStorage(root));
            var added = restored.Entries.Single(e => e.Id != old.Id);
            Check(added.State == QueueState.Completed && added.Options.VideoContainer == VideoContainer.WebM && added.Options.Height == 720 && File.Exists(added.Result.FilePath), "A newly launched desktop downloads the exact browser output to the saved folder and persists completion");
            Check(restored.Entries.Single(e => e.Id == old.Id).State == QueueState.Pending, "Launching for a confirmed browser download leaves unrelated old pending jobs stopped");
            Console.WriteLine("PASS: Cold app handoff used a real new WPF process and framed named pipes; media extraction was locally simulated.");
            return 0;
        }
        finally
        {
            BrowserLinkTransport.PipeNameOverride = previous;
            if (child != null) { if (!child.HasExited) { child.Kill(true); await child.WaitForExitAsync(); } child.Dispose(); }
        }
    }

    private static int CheckBrowserColdChild(string root, string pipe)
    {
        BrowserLinkTransport.PipeNameOverride = pipe;
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        var provider = new FakeProvider { DownloadHandler = (options, _, _) =>
        {
            if (options.VideoContainer != VideoContainer.WebM || options.Height != 720 || options.Folder != root) throw new InvalidDataException("The browser output was changed during startup.");
            string file = Path.Combine(root, "cold-start.webm"); File.WriteAllBytes(file, new byte[4096]);
            return new DownloadResult { FilePath = file };
        } };
        var service = new DownloadService(provider, new EmptyStorage());
        var window = new MainWindow(service, new SettingsRepository(root), new FixtureThumbnailLoader()) { ShowActivated = false, ShowInTaskbar = false, Opacity = 0 };
        var model = (MainViewModel)window.DataContext;
        using var lifetime = new CancellationTokenSource();
        Task listener = null;
        int result = 1;
        var clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        var deadline = Stopwatch.StartNew();
        clock.Tick += async (_, _) =>
        {
            bool completed = model.Queue.Entries.Any(e => e.State == QueueState.Completed);
            if (!completed && deadline.Elapsed < TimeSpan.FromSeconds(15)) return;
            clock.Stop(); result = completed ? 0 : 1; lifetime.Cancel();
            if (listener != null) await listener;
            window.Close();
        };
        window.Loaded += (_, _) =>
        {
            listener = BrowserLinkTransport.ListenRequestsAsync(request => window.Dispatcher.Invoke(() => model.ReceiveBrowserRequest(request)), lifetime.Token);
            clock.Start();
        };
        app.Run(window);
        return result;
    }
}
