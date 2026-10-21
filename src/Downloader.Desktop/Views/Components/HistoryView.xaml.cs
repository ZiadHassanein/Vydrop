using System.Windows.Controls;

namespace YouTubeDownloader;

public partial class HistoryView : UserControl
{
    public HistoryView()
    {
        InitializeComponent();
        IsVisibleChanged += (_, _) => { if (IsVisible) (DataContext as RecentDownloadsViewModel)?.RefreshFiles(); };
    }
}
