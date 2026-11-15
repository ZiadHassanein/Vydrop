using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YouTubeDownloader;

public static class LinkTextSelection
{
    public static readonly DependencyProperty SelectAllOnDoubleClickProperty = DependencyProperty.RegisterAttached(
        "SelectAllOnDoubleClick", typeof(bool), typeof(LinkTextSelection), new PropertyMetadata(false, Changed));

    public static bool GetSelectAllOnDoubleClick(DependencyObject element) => (bool)element.GetValue(SelectAllOnDoubleClickProperty);
    public static void SetSelectAllOnDoubleClick(DependencyObject element, bool value) => element.SetValue(SelectAllOnDoubleClickProperty, value);

    private static void Changed(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not TextBox input) return;
        if ((bool)args.NewValue) input.PreviewMouseDown += SelectLink;
        else input.PreviewMouseDown -= SelectLink;
    }

    private static void SelectLink(object sender, MouseButtonEventArgs args)
    {
        if (args.ChangedButton != MouseButton.Left || args.ClickCount != 2 ||
            sender is not TextBox { Text.Length: > 0 } input) return;
        input.Focus();
        input.SelectAll();
        // Stop native word selection from shortening the whole-link selection.
        args.Handled = true;
    }
}
