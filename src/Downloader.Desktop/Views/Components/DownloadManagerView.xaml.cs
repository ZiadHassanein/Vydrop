using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace YouTubeDownloader;

public partial class DownloadManagerView : UserControl
{
    private bool compact;
    internal event Action WorkspaceLayoutChanged;
    public DownloadManagerView()
    {
        InitializeComponent();
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
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is nameof(MainViewModel.AddDownloadVisible) or nameof(MainViewModel.SettingsVisible) or nameof(MainViewModel.HistoryVisible) or nameof(MainViewModel.HasMediaPreview)) RefreshWorkspace();
    }
    internal void RefreshWorkspace()
    {
        if (DataContext is not MainViewModel model) return;
        bool editor = model.AddDownloadVisible || model.SettingsVisible;
        LibraryContent.Visibility = !editor && !model.HistoryVisible ? Visibility.Visible : Visibility.Collapsed;
        HistoryContent.Visibility = !editor && model.HistoryVisible ? Visibility.Visible : Visibility.Collapsed;
        EditorHost.Visibility = editor ? Visibility.Visible : Visibility.Collapsed;
        EditorDetailsHost.Visibility = model.AddDownloadVisible && model.HasMediaPreview && !model.SettingsVisible ? Visibility.Visible : Visibility.Collapsed;
        LibraryDetails.Visibility = editor ? Visibility.Collapsed : Visibility.Visible;
        DetailsBorder.Visibility = HasInspector ? Visibility.Visible : Visibility.Collapsed;
        LayoutManager();
    }
    private bool HasInspector => DataContext is MainViewModel model && !model.SettingsVisible && !model.HistoryVisible && (!model.AddDownloadVisible || model.HasMediaPreview);

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
        NavigationSurface.IsEnabled = Header.IsEnabled = LibraryContent.IsEnabled = false;
        DetailsCloseButton.Focus();
    }
    private void RowMoreClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: QueueItemView row } button || DataContext is not MainViewModel model) return;
        model.SelectedLibraryItem = row;
        var menu = new ContextMenu { PlacementTarget = button, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom, FlowDirection = model.ContentDirection, FontFamily = model.ContentFont, Style = (Style)FindResource("ManagerMenu") };
        void Add(string header, System.Windows.Input.ICommand command, string icon, bool enabled = true, bool destructive = false)
        {
            var item = new MenuItem { Header = header, Command = command, CommandParameter = row.Id, IsEnabled = enabled, Tag = FindResource(icon), Style = (Style)FindResource("ManagerMenuItem") };
            if (destructive) item.SetResourceReference(ForegroundProperty, "Error");
            menu.Items.Add(item);
        }
        Add(row.PrimaryAction, model.LibraryActionCommand, row.IsActive ? "ManagerPauseIcon" : "ManagerPlayIcon");
        if (row.IsActive) Add(model.CancelLabel, model.LibraryCancelCommand, "CloseIcon");
        menu.Items.Add(new Separator());
        Add(model.OpenSaveFolderLabel, model.OpenQueueFolderCommand, "OpenFolderIcon", row.CanOpenFolder);
        if (row.CanRefresh) Add(model.RefreshSourceLabel, model.RefreshQueueSourceCommand, "RefreshIcon");
        menu.Items.Add(new Separator());
        Add(model.RemoveLabel, model.RemoveQueueCommand, "CloseIcon", row.CanRemove, destructive: true);
        menu.Closed += (_, _) => { button.ContextMenu = null; button.Focus(); };
        button.ContextMenu = menu;
        menu.IsOpen = true;
    }
    private void HideDetailsClicked(object sender, RoutedEventArgs e) => HideDetails();
    private void DownloadDoubleClicked(object sender, MouseButtonEventArgs e) { if (compact && (DataContext as MainViewModel)?.SelectedLibraryItem != null) ShowDetailsClicked(sender, e); }
    private void HideDetails()
    {
        bool visible = DetailOverlay.IsVisible;
        DetailOverlay.Visibility = Visibility.Collapsed;
        NavigationSurface.IsEnabled = Header.IsEnabled = LibraryContent.IsEnabled = true;
        if (visible) DownloadsList.Focus();
    }
}
