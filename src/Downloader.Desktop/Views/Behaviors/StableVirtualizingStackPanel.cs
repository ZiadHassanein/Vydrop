using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace YouTubeDownloader;

public sealed class StableVirtualizingStackPanel : VirtualizingStackPanel
{
    private bool measuring, retryPending;

    protected override Size MeasureOverride(Size constraint)
    {
        // A native resize can reenter layout while the same generator owns a batch.
        // Keep the last measure until that batch finishes, then measure the new viewport.
        if (measuring)
        {
            if (!retryPending)
            {
                retryPending = true;
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () => { retryPending = false; InvalidateMeasure(); });
            }
            return DesiredSize;
        }
        measuring = true;
        try { return base.MeasureOverride(constraint); }
        finally { measuring = false; }
    }
}
