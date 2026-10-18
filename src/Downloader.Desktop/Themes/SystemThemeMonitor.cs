#nullable enable
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace YouTubeDownloader;

// Owns system notifications independently of download/view-model state.
internal sealed class SystemThemeMonitor : IDisposable
{
    private readonly Window owner;
    private readonly Func<bool> dark;
    private readonly Action bindingsChanged;
    private HwndSource? source;
    private DispatcherOperation? pending;
    private bool disposed;
    internal static int ActiveSubscriptions { get; private set; }

    internal SystemThemeMonitor(Window owner, Func<bool> dark, Action bindingsChanged)
    {
        this.owner = owner;
        this.dark = dark;
        this.bindingsChanged = bindingsChanged;
        owner.SourceInitialized += SourceReady;
        owner.Closed += Closed;
        SystemParameters.StaticPropertyChanged += ParametersChanged;
        ActiveSubscriptions++;
        AttachSource();
    }

    private void SourceReady(object? sender, EventArgs args) => AttachSource();
    private void AttachSource()
    {
        if (source != null || disposed) return;
        source = PresentationSource.FromVisual(owner) as HwndSource;
        source?.AddHook(WindowMessage);
    }

    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Defer until WPF has invalidated its cached system brushes.
        if (message is 0x0015 or 0x001A or 0x031A) ScheduleRefresh(); // SYSCOLORCHANGE, SETTINGCHANGE, THEMECHANGED
        return IntPtr.Zero;
    }

    private void ParametersChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SystemParameters.HighContrast)) ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (disposed || owner.Dispatcher.HasShutdownStarted) return;
        if (!owner.Dispatcher.CheckAccess())
        {
            owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, ScheduleRefresh);
            return;
        }
        if (pending?.Status == DispatcherOperationStatus.Pending) return;
        pending = owner.Dispatcher.BeginInvoke(DispatcherPriority.Background, Refresh);
    }

    internal void Refresh()
    {
        owner.Dispatcher.VerifyAccess();
        if (disposed) return;
        if (owner is MainWindow main) main.ThemeTransition.Reset();
        ThemeResources.Apply(dark(), animate: false);
        bindingsChanged();
        foreach (Window window in Application.Current.Windows) ThemeResources.ApplyTitleBar(window, dark());
    }

    private void Closed(object? sender, EventArgs args) => Dispose();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        pending?.Abort();
        source?.RemoveHook(WindowMessage);
        source = null;
        owner.SourceInitialized -= SourceReady;
        owner.Closed -= Closed;
        SystemParameters.StaticPropertyChanged -= ParametersChanged;
        ActiveSubscriptions--;
    }
}
