using System.Windows;
using System.Windows.Automation.Peers;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace YouTubeDownloader;

public partial class SubtitleOptionsView : UserControl
{
    public SubtitleOptionsView()
    {
        InitializeComponent();
        CaptionInput.ItemContainerGenerator.StatusChanged += (_, _) => FilterCaptionChoices();
        Loaded += (_, _) => { if (DataContext is MainViewModel model) model.PropertyChanged += NoticeChanged; };
        Unloaded += (_, _) => { if (DataContext is MainViewModel model) model.PropertyChanged -= NoticeChanged; };
        Loaded += (_, _) =>
        {
            CaptionInput.Focus();
            ViewMotion.Reveal(OptionsContent);
        };
        Unloaded += (_, _) =>
        {
            if (DataContext is not MainViewModel model) return;
            model.CaptionLanguagesCommand.Cancel();
            if (model.OptionsNoticeVisible) model.DismissNoticeCommand.Execute(null);
        };
    }

    private void DoneClicked(object sender, RoutedEventArgs args) => (DataContext as MainViewModel)?.NewDownloadCommand.Execute(null);

    private TextBox SearchInput => CaptionInput.Template?.FindName("CaptionSearchInput", CaptionInput) as TextBox;

    private void CaptionSearchOpened(object sender, EventArgs args)
    {
        if (SearchInput is not { } search) return;
        search.Clear();
        FilterCaptionChoices();
        // ComboBox also queues selection focus while its native popup opens.
        // Run after that work so the editable search remains the keyboard target.
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (CaptionInput.IsDropDownOpen) search.Focus();
        }));
    }

    private void CaptionSearchChanged(object sender, TextChangedEventArgs args) => FilterCaptionChoices();

    private void FilterCaptionChoices()
    {
        if (CaptionInput?.IsDropDownOpen != true || SearchInput is not { } search) return;
        int matches = 0;
        foreach (CaptionChoice choice in CaptionInput.Items)
        {
            bool match = MatchesCaption(choice, search.Text);
            if (match) matches++;
            // Keep the original items/selection intact: searching must never change download options.
            if (CaptionInput.ItemContainerGenerator.ContainerFromItem(choice) is ComboBoxItem item)
                item.Visibility = match ? Visibility.Visible : Visibility.Collapsed;
        }
        if (CaptionInput.Template.FindName("CaptionSearchEmpty", CaptionInput) is TextBlock empty)
        {
            empty.Visibility = matches == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (matches == 0) (UIElementAutomationPeer.FromElement(empty) ?? UIElementAutomationPeer.CreatePeerForElement(empty))?
                .RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
        if (CaptionInput.Template.FindName("CaptionResults", CaptionInput) is ScrollViewer results) results.ScrollToTop();
    }

    internal static bool MatchesCaption(CaptionChoice choice, string text)
    {
        string query = text.Trim();
        if (query.Length == 0) return true;
        static bool Contains(string value, string query) => CultureInfo.InvariantCulture.CompareInfo.IndexOf(value ?? "", query,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
        if (Contains(choice.Name, query) || Contains(choice.Language, query)) return true;
        if (string.IsNullOrEmpty(choice.Language)) return false;
        try
        {
            var culture = CultureInfo.GetCultureInfo(choice.Language);
            return Contains(culture.EnglishName, query) || Contains(culture.NativeName, query);
        }
        catch (CultureNotFoundException) { return false; }
    }

    private void ClearCaptionSearch(object sender, RoutedEventArgs args)
    {
        SearchInput?.Clear();
        SearchInput?.Focus();
    }

    private void CaptionSearchKeyDown(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            CaptionInput.IsDropDownOpen = false;
            CaptionInput.Focus();
            args.Handled = true;
        }
        else if (args.Key is Key.Enter or Key.Down or Key.Up)
        {
            var matches = CaptionInput.Items.Cast<CaptionChoice>().Where(choice => MatchesCaption(choice, SearchInput?.Text ?? ""));
            var choice = args.Key == Key.Up ? matches.LastOrDefault() : matches.FirstOrDefault();
            if (choice != null)
            {
                if (args.Key == Key.Enter) { CaptionInput.SelectedItem = choice; CaptionInput.IsDropDownOpen = false; CaptionInput.Focus(); }
                else if (CaptionInput.ItemContainerGenerator.ContainerFromItem(choice) is ComboBoxItem item) item.Focus();
            }
            args.Handled = true;
        }
    }

    private void KeepSearchInput(object sender, KeyEventArgs args)
    {
        // Text editing belongs to this field; Tab still follows normal keyboard navigation.
        if (args.Key != Key.Tab) args.Handled = true;
    }

    private void NoticeChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(MainViewModel.OptionsNoticeVisible) ||
            DataContext is not MainViewModel { OptionsNoticeVisible: true }) return;
        (UIElementAutomationPeer.FromElement(OptionsNoticeHeading) ?? UIElementAutomationPeer.CreatePeerForElement(OptionsNoticeHeading))?
            .RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }
}
