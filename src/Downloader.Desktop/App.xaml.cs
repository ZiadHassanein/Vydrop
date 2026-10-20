using System.Windows;

namespace YouTubeDownloader;
public partial class App : Application
{
    private Mutex instance;
    private CancellationTokenSource browserLifetime;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (BrowserHost.IsInvocation(e.Args))
        {
            await BrowserHost.RunAsync(e.Args);
            Shutdown();
            return;
        }
        bool smoke = e.Args.Length == 2 && e.Args[0] == "--smoke-test";
        if (!smoke && AppUpdates.RunHelper(e.Args))
        {
            Shutdown();
            return;
        }

        instance = new Mutex(true, smoke ? "Local\\YouTubeDownloader.Smoke." + Environment.ProcessId : ReleaseChannel.IsPreview ? "Local\\ViFree.Preview.Desktop" : "Local\\YouTubeDownloader.Desktop", out bool first);
        if (!first)
        {
            Shutdown();
            return;
        }

        ThemeResources.Apply(true);
        if (smoke)
        {
            string root = Path.GetFullPath(e.Args[1]);
            var service = new DownloadService(new YtDlpProvider(new DependencyManager(Path.Combine(root, "Tools"), Array.Empty<string>())), new LocalPlaylistStorage(Path.Combine(root, "PlaylistState")));
            MainWindow = new MainWindow(service, new SettingsRepository(root));
            MainWindow.ContentRendered += (_, _) =>
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "startup-ready.txt"), "ready");
            };
        }
        else
            MainWindow = new MainWindow();
        MainWindow.Show();
        if (e.Args.Length == 1 && e.Args[0] == "--browser-setup")
            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
                ((MainViewModel)MainWindow.DataContext).BrowserCommand.Execute(null)));
        if (!smoke)
        {
            browserLifetime = new();
            _ = BrowserLinkTransport.ListenRequestsAsync(request => Dispatcher.Invoke(() =>
            {
                var receipt = ((MainViewModel)MainWindow.DataContext).ReceiveBrowserRequest(request);
                if (receipt.Accepted && request.Type != "ping") { if (MainWindow.WindowState == WindowState.Minimized) MainWindow.WindowState = WindowState.Normal; MainWindow.Activate(); }
                return receipt;
            }), browserLifetime.Token);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instance?.Dispose();
        browserLifetime?.Cancel();
        browserLifetime?.Dispose();
        base.OnExit(e);
    }
}
