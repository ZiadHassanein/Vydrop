using System.Windows;

namespace YouTubeDownloader;
public partial class App : Application
{
    private Mutex instance;
    private CancellationTokenSource browserLifetime;
    private EventWaitHandle installClose;
    private RegisteredWaitHandle installCloseWait;
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
        if (!smoke)
        {
            var companion = new BrowserCompanionFiles(AppContext.BaseDirectory, AppPaths.UserData);
            try
            {
                companion.RecoverInterruptedUpdate();
                if (AppUpdates.IsUpdateStartup) companion.UpdateInstalledCopies();
                else companion.UpdateManagedCopies();
            }
            catch (Exception error) when (!AppUpdates.IsUpdateStartup && error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                // Downloads remain usable; Browser integration can retry local preparation.
            }
            BrowserRegistration.EnsureAutomatic(Environment.ProcessPath);
        }
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
        if (!smoke)
        {
            installClose = new EventWaitHandle(false, EventResetMode.AutoReset,
                (ReleaseChannel.IsPreview ? "Local\\ViFree.Preview.Desktop" : "Local\\YouTubeDownloader.Desktop") + ".InstallClose");
            installCloseWait = ThreadPool.RegisterWaitForSingleObject(installClose, (_, _) =>
            {
                if (!Dispatcher.HasShutdownStarted) _ = Dispatcher.BeginInvoke(new Action(() => MainWindow.Close()));
            }, null, Timeout.Infinite, false);
        }
        if ((e.Args.Length is 1 or 2) && e.Args[0] == "--browser-setup")
            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var model = (MainViewModel)MainWindow.DataContext;
                if (e.Args.Length == 2 && Enum.TryParse<SetupBrowser>(e.Args[1], out var browser) && Enum.IsDefined(browser))
                    new DesktopActions(MainWindow).ShowBrowserIntegration(text => Texts.For(model.IsArabic ? "ar" : "en", text), new BrowserSetupActions(), browser);
                else model.BrowserCommand.Execute(null);
            }));
        if (!smoke)
        {
            _ = AppUpdates.CleanupCompletedAsync();
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
        installCloseWait?.Unregister(null);
        installClose?.Dispose();
        instance?.Dispose();
        browserLifetime?.Cancel();
        browserLifetime?.Dispose();
        base.OnExit(e);
    }
}
