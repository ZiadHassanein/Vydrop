using System.Windows;
using System.Windows.Controls;

namespace YouTubeDownloader;

public sealed class PlaylistSelectionView : UserControl
{
    private readonly Func<string, string> translate;
    private readonly TaskCompletionSource<HashSet<string>> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Button all = new(), none = new(), apply = new(), back = new();
    private readonly TextBlock error = new();
    private readonly List<CheckBox> checks = new();
    public Task<HashSet<string>> Selection => completion.Task;

    internal PlaylistSelectionView(PlaylistInfo playlist, HashSet<string> selected, Func<string, string> translate)
    {
        this.translate = translate;
        SetResourceReference(BackgroundProperty, "Page");
        SetResourceReference(ForegroundProperty, "Ink");
        var grid = new Grid { Margin = new Thickness(24, 4, 24, 24) };
        foreach (var height in new[] { GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto, GridLength.Auto }) grid.RowDefinitions.Add(new RowDefinition { Height = height });
        var header = new StackPanel();
        header.Children.Add(new TextBlock { Text = playlist.Title, FontSize = 23, LineHeight = 32, MaxHeight = 64, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = playlist.Title, FontWeight = FontWeights.SemiBold });
        var selectActions = new WrapPanel { Margin = new Thickness(0, 12, 0, 12) };
        all.Style = none.Style = (Style)Application.Current.Resources["QuietButton"];
        none.Margin = new Thickness(12, 0, 0, 0);
        selectActions.Children.Add(all); selectActions.Children.Add(none); header.Children.Add(selectActions); grid.Children.Add(header);
        int index = 0;
        foreach (var entry in playlist.Entries)
        {
            var check = new CheckBox { Tag = entry.Id, Content = new TextBlock { Text = ++index + ". " + entry.Title, TextWrapping = TextWrapping.Wrap }, IsEnabled = entry.Id != null && entry.MediaUrl != null, IsChecked = entry.Id != null && entry.MediaUrl != null && (selected == null || selected.Contains(entry.Id)), Margin = new Thickness(0, 0, 0, 12) };
            check.SetResourceReference(Control.ForegroundProperty, "Ink"); checks.Add(check);
        }
        all.Click += (_, _) => { foreach (var check in checks.Where(check => check.IsEnabled)) check.IsChecked = true; };
        none.Click += (_, _) => { foreach (var check in checks) check.IsChecked = false; };
        var choices = new ListBox { ItemsSource = checks, Style = (Style)Application.Current.Resources["PlaylistChoices"] };
        choices.GotKeyboardFocus += (_, args) => { if (args.NewFocus is ListBoxItem { Content: CheckBox check }) check.Focus(); };
        Grid.SetRow(choices, 1); grid.Children.Add(choices);
        error.SetResourceReference(TextBlock.ForegroundProperty, "Error"); error.Margin = new Thickness(0, 12, 0, 0); Grid.SetRow(error, 2); grid.Children.Add(error);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        apply.Style = (Style)Application.Current.Resources["PrimaryButton"]; back.Margin = new Thickness(12, 0, 0, 0);
        apply.Click += (_, _) =>
        {
            if (!checks.Any(check => check.IsEnabled && check.IsChecked == true)) { error.Text = translate("Choose at least one available video."); return; }
            completion.TrySetResult(checks.Where(check => check.IsEnabled && check.IsChecked == true).Select(check => (string)check.Tag).ToHashSet());
        };
        back.Click += (_, _) => completion.TrySetResult(null);
        buttons.Children.Add(apply); buttons.Children.Add(back); Grid.SetRow(buttons, 3); grid.Children.Add(buttons);
        Content = grid; RefreshLanguage();
        Unloaded += (_, _) => completion.TrySetResult(null);
    }

    internal void RefreshLanguage()
    {
        all.Content = translate("Select all"); none.Content = translate("Clear selection"); apply.Content = translate("Use selection"); back.Content = translate("Back to Add download");
        if (error.Text.Length > 0) error.Text = translate("Choose at least one available video.");
    }
}
