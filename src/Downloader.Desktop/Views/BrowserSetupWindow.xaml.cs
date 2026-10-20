using System.Windows;

namespace YouTubeDownloader;

public partial class BrowserSetupWindow : Window
{
    private readonly System.Windows.Threading.DispatcherTimer verification = new() { Interval = TimeSpan.FromSeconds(1) };
    public BrowserSetupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeResources.ApplyTitleBar(this, (Owner?.DataContext as MainViewModel)?.IsDark == true);
        ContentRendered += (_, _) => SetupBrowserPicker.Focus();
        verification.Tick += (_, _) => (DataContext as BrowserSetupViewModel)?.RefreshVerification();
        Loaded += (_, _) => verification.Start();
        Closed += (_, _) => verification.Stop();
    }
}
