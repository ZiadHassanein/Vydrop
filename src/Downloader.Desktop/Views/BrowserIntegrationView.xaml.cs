using System.Windows.Controls;

namespace YouTubeDownloader;

public partial class BrowserIntegrationView : UserControl
{
    private readonly System.Windows.Threading.DispatcherTimer verification = new() { Interval = TimeSpan.FromSeconds(1) };
    public BrowserIntegrationView()
    {
        InitializeComponent();
        Loaded += (_, _) => SetupBrowserPicker.Focus();
        verification.Tick += (_, _) => (DataContext as BrowserSetupViewModel)?.RefreshVerification();
        Loaded += (_, _) => verification.Start();
        Unloaded += (_, _) => verification.Stop();
    }
}
