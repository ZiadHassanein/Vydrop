using System.Windows;
using System.Windows.Controls;

namespace YouTubeDownloader;
public sealed partial class DesktopActions
{
    public void ShowOutputOptions(MainViewModel model)
    {
        var workArea = owner is MainWindow main ? main.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new OutputOptionsWindow
        {
            Owner = owner, DataContext = model, Icon = owner.Icon,
            Width = Math.Min(540, owner.ActualWidth - 48),
            MaxHeight = Math.Min(owner.ActualHeight - 48, workArea.Height - 48)
        };
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }

    public void ShowQueue(MainViewModel model)
    {
        model.ClearQueueMessage();
        var workArea = owner is MainWindow main ? main.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new QueueWindow
        {
            Owner = owner, DataContext = model, Icon = owner.Icon,
            MaxWidth = Math.Min(owner.ActualWidth - 48, workArea.Width - 48),
            MaxHeight = Math.Min(owner.ActualHeight - 48, workArea.Height - 48)
        };
        dialog.Width = Math.Min(dialog.Width, dialog.MaxWidth);
        dialog.Height = Math.Min(dialog.Height, dialog.MaxHeight);
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }
    public HashSet<string> SelectPlaylist(PlaylistInfo playlist, HashSet<string> selected, Func<string, string> t)
    {
        var dialog = Dialog(t("Choose videos"), 560);
        var grid = new Grid { Margin = new Thickness(24) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = new StackPanel();
        header.Children.Add(new TextBlock { Text = playlist.Title, FontSize = 18, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        var selectActions = new WrapPanel { Margin = new Thickness(0, 12, 0, 12) };
        var all = new Button { Content = t("Select all"), Style = (Style)Application.Current.Resources["QuietButton"] };
        var none = new Button { Content = t("Clear selection"), Style = all.Style, Margin = new Thickness(12, 0, 0, 0) };
        selectActions.Children.Add(all); selectActions.Children.Add(none); header.Children.Add(selectActions); grid.Children.Add(header);
        var checks = new List<CheckBox>();
        int index = 0;
        foreach (var entry in playlist.Entries)
        {
            index++;
            var check = new CheckBox
            {
                Tag = entry.Id, Content = new TextBlock { Text = index + ". " + entry.Title, TextWrapping = TextWrapping.Wrap },
                IsEnabled = entry.Id != null && entry.MediaUrl != null,
                IsChecked = entry.Id != null && entry.MediaUrl != null && (selected == null || selected.Contains(entry.Id)),
                Margin = new Thickness(0, 0, 0, 12)
            };
            check.SetResourceReference(Control.ForegroundProperty, "Ink");
            checks.Add(check);
        }
        all.Click += (_, _) => { foreach (var check in checks.Where(c => c.IsEnabled)) check.IsChecked = true; };
        none.Click += (_, _) => { foreach (var check in checks) check.IsChecked = false; };
        var choices = new ListBox { ItemsSource = checks, Style = (Style)Application.Current.Resources["PlaylistChoices"] };
        // The list supplies virtualized navigation; each checkbox owns its checked state.
        choices.GotKeyboardFocus += (_, args) =>
        {
            if (args.NewFocus is ListBoxItem { Content: CheckBox check }) check.Focus();
        };
        Grid.SetRow(choices, 1); grid.Children.Add(choices);
        var error = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
        error.SetResourceReference(TextBlock.ForegroundProperty, "Error");
        Grid.SetRow(error, 2); grid.Children.Add(error);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        var apply = new Button { Content = t("Use selection"), Style = (Style)Application.Current.Resources["PrimaryButton"] };
        var cancel = new Button { Content = t("Cancel"), IsCancel = true, Margin = new Thickness(12, 0, 0, 0) };
        apply.Click += (_, _) =>
        {
            if (!checks.Any(c => c.IsEnabled && c.IsChecked == true)) { error.Text = t("Choose at least one available video."); return; }
            dialog.DialogResult = true;
        };
        buttons.Children.Add(apply); buttons.Children.Add(cancel); Grid.SetRow(buttons, 3); grid.Children.Add(buttons);
        dialog.Content = grid;
        return dialog.ShowDialog() == true ? checks.Where(c => c.IsEnabled && c.IsChecked == true).Select(c => (string)c.Tag).ToHashSet() : null;
    }
    public void ShowBrowserIntegration(Func<string, string> t)
        => ShowBrowserIntegration(t, new BrowserSetupActions());

    internal void ShowBrowserIntegration(Func<string, string> t, IBrowserSetupActions actions, SetupBrowser? installerBrowser = null)
    {
        var main = owner.DataContext as MainViewModel;
        var workArea = owner is MainWindow window ? window.CurrentWorkArea() : SystemParameters.WorkArea;
        var dialog = new BrowserSetupWindow
        {
            Owner = owner, Icon = owner.Icon,
            DataContext = new BrowserSetupViewModel(t, actions, main?.IsArabic == true),
            FontFamily = main?.ContentFont ?? owner.FontFamily,
            FlowDirection = main?.ContentDirection ?? owner.FlowDirection,
            Width = Math.Min(680, Math.Min(owner.ActualWidth - 48, workArea.Width - 48)),
            Height = Math.Min(700, Math.Min(owner.ActualHeight - 48, workArea.Height - 48))
        };
        if (installerBrowser is { } browser)
            dialog.ContentRendered += (_, _) => ((BrowserSetupViewModel)dialog.DataContext).BeginInstallerSetup(browser);
        var previousFocus = System.Windows.Input.Keyboard.FocusedElement as UIElement;
        dialog.ShowDialog();
        if (previousFocus is { IsEnabled: true, IsVisible: true }) previousFocus.Focus();
    }
}
