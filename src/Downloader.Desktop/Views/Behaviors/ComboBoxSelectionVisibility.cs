using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Media;

namespace YouTubeDownloader;

public static class ComboBoxSelectionVisibility
{
    public static readonly DependencyProperty KeepVisibleProperty = DependencyProperty.RegisterAttached(
        "KeepVisible", typeof(bool), typeof(ComboBoxSelectionVisibility), new PropertyMetadata(false, Changed));

    public static bool GetKeepVisible(DependencyObject element) => (bool)element.GetValue(KeepVisibleProperty);
    public static void SetKeepVisible(DependencyObject element, bool value) => element.SetValue(KeepVisibleProperty, value);

    private static void Changed(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not ComboBox combo) return;
        if ((bool)args.NewValue) { combo.DropDownOpened += RevealSelection; combo.SelectionChanged += SelectionChanged; }
        else { combo.DropDownOpened -= RevealSelection; combo.SelectionChanged -= SelectionChanged; }
    }

    private static void SelectionChanged(object sender, SelectionChangedEventArgs args) => RevealSelection(sender, EventArgs.Empty);

    private static void RevealSelection(object sender, EventArgs args)
    {
        var combo = (ComboBox)sender;
        if (!combo.IsDropDownOpen) return;
        if (combo.Template.FindName("PART_Popup", combo) is not Popup { Child: FrameworkElement panel }) return;
        if (!panel.IsLoaded)
        {
            // DropDownOpened precedes the popup's first native layout and item generation.
            void ClearPending()
            {
                panel.Loaded -= Loaded;
                combo.DropDownClosed -= Closed;
            }
            void Loaded(object source, RoutedEventArgs loaded) { ClearPending(); RevealSelection(combo, EventArgs.Empty); }
            void Closed(object source, EventArgs closed) => ClearPending();
            panel.Loaded += Loaded;
            combo.DropDownClosed += Closed;
            return;
        }
        combo.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            if (!combo.IsDropDownOpen || combo.SelectedItem == null) return;
            panel.UpdateLayout();
            if (combo.ItemContainerGenerator.ContainerFromItem(combo.SelectedItem) is not FrameworkElement row ||
                row.Visibility != Visibility.Visible || FindViewport(row) is not ScrollViewer viewport ||
                viewport.Template.FindName("PART_ScrollContentPresenter", viewport) is not FrameworkElement presenter || !presenter.IsAncestorOf(row)) return;
            var bounds = row.TransformToAncestor(presenter).TransformBounds(new Rect(row.RenderSize));
            if (bounds.Top < 0) viewport.ScrollToVerticalOffset(viewport.VerticalOffset + bounds.Top);
            else if (bounds.Bottom > presenter.ActualHeight)
                viewport.ScrollToVerticalOffset(viewport.VerticalOffset + bounds.Bottom - presenter.ActualHeight);
            viewport.UpdateLayout();
        });
    }

    private static ScrollViewer FindViewport(DependencyObject child)
    {
        // A searchable popup also contains the search TextBox's private ScrollViewer.
        // Walk from the selected row so only its actual items viewport is considered.
        for (var parent = VisualTreeHelper.GetParent(child); parent != null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is ScrollViewer viewport) return viewport;
        return null;
    }
}
