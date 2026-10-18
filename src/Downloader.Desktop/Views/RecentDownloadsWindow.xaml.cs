using System.Windows;

namespace YouTubeDownloader;

public partial class RecentDownloadsWindow : Window
{
    public RecentDownloadsWindow()
    {
        InitializeComponent();
        Activated += (_, _) => (DataContext as RecentDownloadsViewModel)?.RefreshFiles();
    }
}
