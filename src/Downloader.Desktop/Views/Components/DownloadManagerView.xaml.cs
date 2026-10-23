using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace YouTubeDownloader;

public partial class DownloadManagerView : UserControl
{
    private bool compact;
    private readonly WorkspaceMotion workspaceMotion;
    internal event Action WorkspaceLayoutChanged;
    public DownloadManagerView()
    {
        InitializeComponent();
        workspaceMotion = new WorkspaceMotion(this);
        ((Panel)Header.BrandButton.Parent).Children.Remove(Header.BrandButton);
        BrandHost.Children.Add(Header.BrandButton);
        SizeChanged += (_, _) => LayoutManager();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is MainViewModel previous) previous.PropertyChanged -= WorkspaceChanged;
            if (e.NewValue is MainViewModel next) next.PropertyChanged += WorkspaceChanged;
            RefreshWorkspace();
        };
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && DetailOverlay.IsVisible) { HideDetails(); e.Handled = true; } };
    }
    private void WorkspaceChanged(object sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is nameof(MainViewModel.AddDownloadVisible) or nameof(MainViewModel.SettingsVisible) or nameof(MainViewModel.HistoryVisible) or nameof(MainViewModel.HasMediaPreview) or nameof(MainViewModel.ActiveWorkspace)) RefreshWorkspace();
    }
    internal void RefreshWorkspace()
    {
        if (DataContext is not MainViewModel model) return;
        bool editor = !model.HasWorkspacePage && (model.AddDownloadVisible || model.SettingsVisible);
        LibraryContent.Visibility = !model.HasWorkspacePage && !editor && !model.HistoryVisible ? Visibility.Visible : Visibility.Collapsed;
        HistoryContent.Visibility = !model.HasWorkspacePage && !editor && model.HistoryVisible ? Visibility.Visible : Visibility.Collapsed;
        WorkspaceHost.Visibility = model.HasWorkspacePage ? Visibility.Visible : Visibility.Collapsed;
        EditorHost.Visibility = editor ? Visibility.Visible : Visibility.Collapsed;
        EditorDetailsHost.Visibility = model.AddDownloadVisible && model.HasMediaPreview && !model.SettingsVisible ? Visibility.Visible : Visibility.Collapsed;
        LibraryDetails.Visibility = editor ? Visibility.Collapsed : Visibility.Visible;
        DetailsBorder.Visibility = HasInspector ? Visibility.Visible : Visibility.Collapsed;
        if (!model.LibraryVisible) HideDetails();
        LayoutManager();
        if (model.HasWorkspacePage) workspaceMotion.Navigate("workspace:" + model.ActiveWorkspace, WorkspaceHost, model.IsBrowserIntegrationPage ? 6 : model.ActiveWorkspace == WorkspacePage.QueueComposer ? 2 : 0);
        else if (model.SettingsVisible) workspaceMotion.Navigate("settings", EditorHost, 7);
        else if (model.HistoryVisible) workspaceMotion.Navigate("history", HistoryContent, 3);
        else if (model.AddDownloadVisible) workspaceMotion.Navigate("add", EditorHost, 0);
        else workspaceMotion.Navigate(model.IsQueuePage ? "queue" : model.IsLibraryVideo ? "video" : model.IsLibraryAudio ? "audio" : "library", LibraryContent, model.IsQueuePage ? 2 : model.IsLibraryVideo ? 4 : model.IsLibraryAudio ? 5 : 1);
    }
    private bool HasInspector => DataContext is MainViewModel model && !model.HasWorkspacePage && !model.SettingsVisible && !model.HistoryVisible && (!model.AddDownloadVisible || model.HasMediaPreview);

    private void LayoutManager()
    {
        double previousWidth = DetailsColumn.Width.Value;
        bool next = ActualWidth < 1080;
        NavigationColumn.Width = new GridLength(ActualWidth < 960 ? 176 : 224);
        DetailsColumn.Width = new GridLength(next || !HasInspector ? 0 : ActualWidth > 1360 ? 350 : 320);
        LibraryContent.Margin = new Thickness(ActualWidth < 960 ? 14 : 24, 4, 18, 0);
        CompactDetailsButton.Visibility = next ? Visibility.Visible : Visibility.Collapsed;
        if (compact != next)
        {
            compact = next;
            if (DetailsBorder.Parent is Panel parent) parent.Children.Remove(DetailsBorder);
            if (compact) { Grid.SetColumn(DetailsBorder, 0); Grid.SetRow(DetailsBorder, 0); CompactDetailsHost.Children.Add(DetailsBorder); }
            else { Grid.SetColumn(DetailsBorder, 2); Grid.SetRow(DetailsBorder, 1); ManagerRoot.Children.Add(DetailsBorder); HideDetails(); }
        }
        if (previousWidth != DetailsColumn.Width.Value) WorkspaceLayoutChanged?.Invoke();
    }
    private void ShowDetailsClicked(object sender, RoutedEventArgs e)
    {
        if (!compact) return;
        DetailOverlay.Visibility = Visibility.Visible;
        LibraryContent.Visibility = Visibility.Collapsed;
        DetailsCloseButton.Focus();
    }
    private void StartQueueClicked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel model) return;
        model.LibraryFilterCommand.Execute("queue");
        model.LibrarySearch = "";
    }
    private void HideDetailsClicked(object sender, RoutedEventArgs e) => HideDetails();
    private void DownloadDoubleClicked(object sender, MouseButtonEventArgs e) { if (compact && (DataContext as MainViewModel)?.SelectedLibraryItem != null) ShowDetailsClicked(sender, e); }
    private void HideDetails()
    {
        bool visible = DetailOverlay.IsVisible;
        DetailOverlay.Visibility = Visibility.Collapsed;
        if (DataContext is MainViewModel model && model.LibraryVisible) LibraryContent.Visibility = Visibility.Visible;
        if (visible) DownloadsList.Focus();
    }
}
