using System.Windows;
using System.Windows.Threading;

namespace YouTubeDownloader;

internal sealed class WorkspaceMotion
{
    private readonly FrameworkElement owner;
    private DispatcherOperation pending;
    private string key, nextKey;
    private int rank, nextRank;
    private FrameworkElement target, nextTarget;
    internal WorkspaceMotion(FrameworkElement owner)
    {
        this.owner = owner;
        owner.Unloaded += (_, _) => { pending?.Abort(); pending = null; ViewMotion.Stop(target); };
    }
    internal void Navigate(string surface, FrameworkElement view, int position)
    {
        if (surface == (pending == null ? key : nextKey)) return;
        nextKey = surface; nextTarget = view; nextRank = position;
        pending ??= owner.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            pending = null;
            bool changed = key != null && key != nextKey;
            int direction = nextRank >= rank ? 1 : -1;
            ViewMotion.Stop(target);
            key = nextKey; target = nextTarget; rank = nextRank;
            if (changed && owner.IsLoaded && target.IsVisible)
                ViewMotion.Navigate(target, direction * (owner.FlowDirection == FlowDirection.RightToLeft ? -1 : 1));
        }));
    }
}
