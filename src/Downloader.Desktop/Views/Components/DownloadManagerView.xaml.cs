using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YouTubeDownloader;

public partial class DownloadManagerView : UserControl
{
    private bool compact;
    public DownloadManagerView()
    {
        InitializeComponent();
        ((Panel)Header.BrandButton.Parent).Children.Remove(Header.BrandButton);
        BrandHost.Children.Add(Header.BrandButton);
        Header.BrandLetters.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "NavigationInk");
        Header.HeaderFrame.BorderThickness = new Thickness(0);
        Header.HeaderFrame.Padding = new Thickness(14, 7, 18, 7);
        Header.RecentDownloadsButton.Visibility = Visibility.Collapsed;
        Header.HeaderActions.Children.OfType<Border>().First().Visibility = Visibility.Collapsed;
        ContactLinks.SettingsButton.Visibility = Visibility.Collapsed;
        Grid.SetColumn(ContactLinks.ContactButton, 0);
        ContactLinks.ContactButton.HorizontalAlignment = HorizontalAlignment.Left;
        ContactLinks.ContactButton.SetResourceReference(Control.ForegroundProperty, "NavigationMuted");
        SizeChanged += (_, _) => LayoutManager();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape && DetailOverlay.IsVisible) { HideDetails(); e.Handled = true; } };
    }
    private void LayoutManager()
    {
        bool next = ActualWidth < 1080;
        NavigationColumn.Width = new GridLength(ActualWidth < 960 ? 176 : 190);
        DetailsColumn.Width = new GridLength(next ? 0 : ActualWidth > 1360 ? 370 : 330);
        LibraryContent.Margin = new Thickness(ActualWidth < 960 ? 14 : 24, 4, 18, 0);
        CompactDetailsButton.Visibility = next ? Visibility.Visible : Visibility.Collapsed;
        if (compact == next) return;
        compact = next;
        if (DetailsBorder.Parent is Panel parent) parent.Children.Remove(DetailsBorder);
        if (compact) { Grid.SetColumn(DetailsBorder, 0); Grid.SetRow(DetailsBorder, 0); CompactDetailsHost.Children.Add(DetailsBorder); }
        else { Grid.SetColumn(DetailsBorder, 2); Grid.SetRow(DetailsBorder, 1); ManagerRoot.Children.Add(DetailsBorder); HideDetails(); }
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
        var menu = new ContextMenu { PlacementTarget = button, FlowDirection = model.ContentDirection, FontFamily = model.ContentFont };
        void Add(string header, System.Windows.Input.ICommand command, bool enabled = true) => menu.Items.Add(new MenuItem { Header = header, Command = command, CommandParameter = row.Id, IsEnabled = enabled });
        Add(row.PrimaryAction, model.LibraryActionCommand);
        if (row.IsActive) Add(model.CancelLabel, model.LibraryCancelCommand);
        Add(model.OpenSaveFolderLabel, model.OpenQueueFolderCommand, row.CanOpenFolder);
        if (row.CanRefresh) Add(model.RefreshSourceLabel, model.RefreshQueueSourceCommand);
        Add(model.RemoveLabel, model.RemoveQueueCommand, row.CanRemove);
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
