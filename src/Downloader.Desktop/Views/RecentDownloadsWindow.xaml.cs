using System.Windows;

namespace YouTubeDownloader;

public partial class RecentDownloadsWindow : Window
{
    public RecentDownloadsWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ThemeResources.ApplyTitleBar(this, Owner?.DataContext is MainViewModel model && model.IsDark);
        Activated += (_, _) => (DataContext as RecentDownloadsViewModel)?.RefreshFiles();
    }
}
