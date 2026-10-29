using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace YouTubeDownloader;

public partial class DownloadFormView : UserControl
{
    public DownloadFormView()
    {
        InitializeComponent();
        Loaded += (_, _) => ArrangeFields();
        SizeChanged += (_, e) => { if (e.WidthChanged) ArrangeFields(); };
        AudioFormatMenu.PlacementTarget = AudioChoice;
        VideoFormatMenu.PlacementTarget = VideoChoice;
        AudioFormatMenu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(AudioChoice, popup);
        VideoFormatMenu.CustomPopupPlacementCallback = (popup, _, _) => ContextMenuPlacement.CenterWithinClient(VideoChoice, popup);
        Unloaded += (_, _) => { AudioFormatMenu.IsOpen = false; VideoFormatMenu.IsOpen = false; };
    }

    private void ArrangeFields()
    {
        bool stacked = ActualWidth - 48 < 520;
        ArrangeField(FormatRow, FormatCaption, FormatPicker, stacked);
        ArrangeField(OutputControls, QualityCaption, QualityControls, stacked);
        ArrangeField(FolderAnchor, FolderCaption, FolderControls, stacked);
        bool compactActions = ActualWidth - 48 < 600;
        Grid.SetColumnSpan(AddToQueueButton, compactActions ? 3 : 1);
        Grid.SetRow(DownloadAction, compactActions ? 1 : 0);
        Grid.SetRow(FormCancel, compactActions ? 1 : 0);
        AddToQueueButton.Margin = new Thickness(0, 0, 0, compactActions ? 8 : 0);
    }

    private static void ArrangeField(Grid row, TextBlock label, FrameworkElement field, bool stacked)
    {
        row.ColumnDefinitions[0].Width = new GridLength(stacked ? 0 : 88);
        Grid.SetColumn(label, stacked ? 1 : 0);
        Grid.SetRow(field, stacked ? 1 : 0);
        label.Margin = stacked ? new Thickness(0, 0, 0, 6) : new Thickness(0, 0, 14, 0);
    }

    private void ClearLinkClicked(object sender, RoutedEventArgs args)
    {
        if (DataContext is not MainViewModel model || !(model.IsIdle || model.IsCheckingMedia)) return;
        model.Url = "";
        LinkInput.Focus();
    }

    private void AudioClicked(object sender, RoutedEventArgs args) => OpenAudioMenu();

    private void AudioKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Down or Key.Up)) return;
        OpenAudioMenu();
        args.Handled = true;
    }

    private void AudioMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareAudioMenu();
    private void VideoMenuOpening(object sender, ContextMenuEventArgs args) => args.Handled = !PrepareVideoMenu();
    private void VideoClicked(object sender, RoutedEventArgs args) => OpenVideoMenu();
    private void VideoKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key is not (Key.Down or Key.Up)) return;
        OpenVideoMenu();
        args.Handled = true;
    }
    private void OpenVideoMenu()
    {
        if (PrepareVideoMenu()) VideoFormatMenu.IsOpen = true;
    }

    private void OpenAudioMenu()
    {
        if (!PrepareAudioMenu()) return;
        AudioFormatMenu.Placement = PlacementMode.Custom;
        AudioFormatMenu.IsOpen = true;
    }

    private bool PrepareAudioMenu()
    {
        if (DataContext is not MainViewModel { IsIdle: true } model) return false;
        model.Audio = true;
        PrepareMenu(AudioFormatMenu, model, model.Encodings, model.SelectedEncoding, choice => model.SelectedEncoding = choice);
        return true;
    }

    private bool PrepareVideoMenu()
    {
        if (DataContext is not MainViewModel { IsIdle: true } model) return false;
        model.Audio = false;
        PrepareMenu(VideoFormatMenu, model, model.VideoFormats, model.SelectedVideoFormat, choice => model.SelectedVideoFormat = choice);
        return VideoFormatMenu.Items.Count > 0;
    }

    private void PrepareMenu<T>(ContextMenu menu, MainViewModel model, IEnumerable<T> choices, T selected, Action<T> choose)
    {
        // Native popups are detached and must refresh direction/font before reuse.
        menu.SetCurrentValue(FlowDirectionProperty, model.ContentDirection);
        menu.SetCurrentValue(FontFamilyProperty, model.ContentFont);
        menu.Items.Clear();
        foreach (var choice in choices)
        {
            var item = new MenuItem
            {
                Header = choice.ToString(), Tag = choice, IsCheckable = true, IsChecked = Equals(choice, selected),
                Style = (Style)FindResource("SelectionMenuItem")
            };
            item.Click += (_, _) => { if (model.IsIdle) choose(choice); menu.IsOpen = false; };
            menu.Items.Add(item);
        }
    }
}
