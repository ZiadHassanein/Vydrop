using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace YouTubeDownloader;
public partial class QueueComposerView : UserControl
{
    public QueueComposerView()
    {
        InitializeComponent();
        Loaded += (_, _) => ShowInitialContent();
    }

    private void ShowInitialContent()
    {
        if (DataContext is not MainViewModel model) return;
        FocusLinkInput(model.QueueLinkInputs.FirstOrDefault());
    }

    private void LinkActionClicked(object sender, RoutedEventArgs args)
    {
        var previous = (sender as FrameworkElement)?.DataContext as QueueLinkInputView;
        int position = DataContext is MainViewModel current && previous != null ? current.QueueLinkInputs.IndexOf(previous) : -1;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!IsLoaded || DataContext is not MainViewModel { ActiveWorkspace: WorkspacePage.QueueComposer } model) return;
            FocusLinkInput(previous != null && model.QueueLinkInputs.Contains(previous) ? previous :
                position >= 0 ? model.QueueLinkInputs.ElementAtOrDefault(Math.Min(position, model.QueueLinkInputs.Count - 1)) : model.QueueLinkInputs.LastOrDefault());
        }));
    }

    private void SubmitLinksClicked(object sender, RoutedEventArgs args)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            if (!IsLoaded || DataContext is not MainViewModel { ActiveWorkspace: WorkspacePage.QueueComposer } model) return;
            if (model.QueueLinkInputs.FirstOrDefault(draft => draft.HasError) is { } invalid) FocusLinkInput(invalid);
            else model.LibraryFilterCommand.Execute("queue");
        }));
    }

    private void LinkPasting(object sender, DataObjectPastingEventArgs args)
    {
        if (sender is not TextBox { DataContext: QueueLinkInputView field } || DataContext is not MainViewModel model ||
            args.DataObject.GetData(DataFormats.UnicodeText) is not string text || !(text.Contains('\r') || text.Contains('\n'))) return;
        args.CancelCommand();
        model.PasteQueueLinks(field, text);
        FocusLinkInput(field);
    }

    private void FocusLinkInput(QueueLinkInputView field)
    {
        if (field == null) return;
        QueueInputFields.UpdateLayout();
        var container = QueueInputFields.ItemContainerGenerator.ContainerFromItem(field);
        if (FindInput(container) is not { } input) return;
        input.BringIntoView();
        QueueViewport.UpdateLayout();
        input.Focus();
    }

    private static TextBox FindInput(DependencyObject root)
    {
        if (root == null) return null;
        if (root is TextBox input) return input;
        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindInput(VisualTreeHelper.GetChild(root, index)) is { } found) return found;
        return null;
    }
    private void CloseClicked(object sender, RoutedEventArgs args) => (DataContext as MainViewModel)?.LibraryFilterCommand.Execute("queue");
}
