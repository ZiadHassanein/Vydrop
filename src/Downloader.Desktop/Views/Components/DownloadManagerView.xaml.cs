using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace YouTubeDownloader;

public partial class DownloadManagerView : UserControl
{
    private bool compact;
    private IInputElement detailsReturnFocus;
    private string detailsScope;
    private Guid? detailsItem;
    private static readonly DependencyPropertyKey IsCompactPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsCompact), typeof(bool), typeof(DownloadManagerView), new PropertyMetadata(false));
    public static readonly DependencyProperty IsCompactProperty = IsCompactPropertyKey.DependencyProperty;
    public bool IsCompact => (bool)GetValue(IsCompactProperty);
    public static readonly RoutedCommand ShowDetailsCommand = new();
    private readonly WorkspaceMotion workspaceMotion;
    internal event Action WorkspaceLayoutChanged;
    public DownloadManagerView()
    {
        InitializeComponent();
        CommandBindings.Add(new CommandBinding(ShowDetailsCommand, (_, e) =>
        {
            if (DataContext is MainViewModel model && e.Parameter is Guid id && model.QueueItems.FirstOrDefault(row => row.Id == id) is { } item)
            {
                model.SelectedLibraryItem = item;
                ShowDetails(e.OriginalSource as IInputElement);
            }
            e.Handled = true;
        }, (_, e) => { e.CanExecute = compact && e.Parameter is Guid id && DataContext is MainViewModel model && model.LibraryItems.Contains(model.QueueItems.FirstOrDefault(item => item.Id == id)); e.Handled = true; }));
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
        if (DetailOverlay.IsVisible && DataContext is MainViewModel model && (!model.LibraryVisible || detailsScope != CurrentLibraryScope || detailsItem != model.SelectedLibraryItem?.Id)) HideDetails();
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is nameof(MainViewModel.AddDownloadVisible) or nameof(MainViewModel.SettingsVisible) or nameof(MainViewModel.HistoryVisible) or nameof(MainViewModel.HasMediaPreview) or nameof(MainViewModel.ActiveWorkspace)) RefreshWorkspace();
    }
    private string CurrentLibraryScope => DataContext is MainViewModel model ? model.IsQueuePage ? "queue" : model.IsLibraryVideo ? "video" : model.IsLibraryAudio ? "audio" : "all" : "";
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
        else workspaceMotion.Navigate(model.IsQueuePage ? "queue" : "library", LibraryContent, model.IsQueuePage ? 2 : 1);
    }
    private bool HasInspector => DataContext is MainViewModel model && !model.HasWorkspacePage && !model.SettingsVisible && !model.HistoryVisible && (!model.AddDownloadVisible || model.HasMediaPreview);

    private void LayoutManager()
    {
        double previousWidth = DetailsColumn.Width.Value;
        bool next = ActualWidth < 1080;
        NavigationColumn.Width = new GridLength(next ? 216 : 224);
        DetailsColumn.Width = new GridLength(next || !HasInspector ? 0 : ActualWidth > 1360 ? 350 : 320);
        LibraryContent.Margin = new Thickness(ActualWidth < 960 ? 14 : 24, 4, 18, 0);
        SetValue(IsCompactPropertyKey, next);
        if (compact != next)
        {
            compact = next;
            if (DetailsBorder.Parent is Panel parent) parent.Children.Remove(DetailsBorder);
            if (compact) { Grid.SetColumn(DetailsBorder, 0); Grid.SetRow(DetailsBorder, 0); CompactDetailsHost.Children.Add(DetailsBorder); }
            else { Grid.SetColumn(DetailsBorder, 2); Grid.SetRow(DetailsBorder, 1); ManagerRoot.Children.Add(DetailsBorder); HideDetails(); }
        }
        if (previousWidth != DetailsColumn.Width.Value) WorkspaceLayoutChanged?.Invoke();
    }
    private void ShowDetails(IInputElement returnFocus = null)
    {
        if (!compact) return;
        detailsReturnFocus = returnFocus ?? Keyboard.FocusedElement;
        detailsScope = CurrentLibraryScope;
        detailsItem = (DataContext as MainViewModel)?.SelectedLibraryItem?.Id;
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
    private void DownloadDoubleClicked(object sender, MouseButtonEventArgs e) { if (compact && (DataContext as MainViewModel)?.SelectedLibraryItem != null) ShowDetails(); }
    private void HideDetails()
    {
        bool visible = DetailOverlay.IsVisible;
        DetailOverlay.Visibility = Visibility.Collapsed;
        if (DataContext is MainViewModel model && model.LibraryVisible) LibraryContent.Visibility = Visibility.Visible;
        if (visible)
        {
            if (detailsReturnFocus is UIElement { IsVisible: true, IsEnabled: true } target) target.Focus();
            else DownloadsList.Focus();
            detailsReturnFocus = null;
        }
    }
}
