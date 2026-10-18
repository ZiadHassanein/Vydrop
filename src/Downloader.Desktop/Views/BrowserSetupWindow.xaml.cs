using System.Windows;

namespace YouTubeDownloader;

public partial class BrowserSetupWindow : Window
{
    public BrowserSetupWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeResources.ApplyTitleBar(this, (Owner?.DataContext as MainViewModel)?.IsDark == true);
        ContentRendered += (_, _) => SetupBrowserPicker.Focus();
    }
}
