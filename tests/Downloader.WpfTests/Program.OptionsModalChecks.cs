using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using YouTubeDownloader;
internal static partial class Program
{
    private static async Task CheckOutputOptionsModal(Window owner, MainViewModel model, FakeProvider provider, string captures)
    {
        provider.CaptionTracks = new() { new("en", "English"), new("ar", "العربية") };
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await WaitFor(() => model.CanDownload, "Subtitle fixture is ready");
        foreach (bool arabic in new[] { false, true }) foreach (bool dark in new[] { false, true })
        {
            model.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            await InspectOptionsModal(owner, model, async view =>
            {
                Check(owner.OwnedWindows.Count == 0 && owner.IsEnabled && ((MainWindow)owner).ManagerContent.NavigationSurface.IsEnabled, "Subtitles opens inside the workspace without disabling navigation");
                Check(view.FlowDirection == model.ContentDirection && ReferenceEquals(view.DataContext, model), "Subtitle section follows current language and settings");
                model.SelectedCaption = model.CaptionChoices.First(choice => choice.Language == "en");
                model.VttCaptions = true;
                await CheckCaptionSearch(view, model, captures, (arabic ? "ar" : "en") + (dark ? "-dark" : "-light"));
                Capture(owner, System.IO.Path.Combine(captures, "subtitle-section-" + arabic + "-" + dark + ".png"));
                ((Button)WpfTree.Find(view, "DoneButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Task.Delay(80);
                Check(model.AddDownloadVisible && !model.HasWorkspacePage && model.VttCaptions, "Done returns to Add download and preserves subtitle choices");
            });
        }
        model.AddQueueCommand.Execute(null);
        Check(model.Queue.Entries.Single().Options.Captions is { Language: "en", Format: CaptionFormat.Vtt }, "Inline subtitle choices are persisted with the queued job");
        foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
        model.Url = "https://www.youtube.com/playlist?list=PLFixture";
        await WaitFor(() => model.CanDownload, "Playlist fixture is ready");
        await InspectOptionsModal(owner, model, async view =>
        {
            await model.CaptionLanguagesCommand.ExecuteAsync(null);
            Check(model.OptionsNoticeVisible && !model.MainNoticeVisible, "Playlist language feedback stays in its section");
            provider.InspectionHook = token => { token.WaitHandle.WaitOne(3000); token.ThrowIfCancellationRequested(); };
            var loading = model.CaptionLanguagesCommand.ExecuteAsync(null);
            await Task.Delay(80);
            model.LibraryFilterCommand.Execute("queue");
            await loading;
            Check(model.IsIdle && !model.OptionsNoticeVisible, "Navigating away cancels subtitle lookup and clears transient feedback");
        });
        provider.InspectionHook = null; provider.CaptionTracks = new(); model.Url = "";
    }
    private static async Task InspectOptionsModal(Window owner, MainViewModel model, Func<SubtitleOptionsView, Task> inspect)
    {
        model.OptionsCommand.Execute(null);
        await Task.Delay(180);
        var view = (SubtitleOptionsView)((MainWindow)owner).WorkspaceView;
        try { await inspect(view); }
        finally { if (((MainWindow)owner).WorkspaceView == view) model.NewDownloadCommand.Execute(null); }
    }
    private static async Task CheckCaptionSearch(SubtitleOptionsView dialog, MainViewModel model, string captures, string variant)
    {
        var caption = (ComboBox)WpfTree.Find(dialog, "CaptionInput");
        var french = new CaptionChoice("fr-CA", "Français (Canada)");
        model.CaptionChoices.Add(french);
        var selected = model.SelectedCaption;
        caption.IsDropDownOpen = true;
        await Task.Delay(150);
        var search = (TextBox)caption.Template.FindName("CaptionSearchInput", caption);
        var empty = (TextBlock)caption.Template.FindName("CaptionSearchEmpty", caption);
        for (int attempt = 0; attempt < 20 && !search.IsKeyboardFocused; attempt++) await Task.Delay(50);
        Check(search.IsKeyboardFocused && search.Text == "", "Opening subtitle choices focuses the search field");
        bool Visible(string language) => ((ComboBoxItem)caption.ItemContainerGenerator.ContainerFromItem(model.CaptionChoices.First(choice => choice.Language == language))).Visibility == Visibility.Visible;
        search.Text = "  ENGLISH  ";
        Check(Visible("en") && !Visible("ar") && !Visible("fr-CA") && ReferenceEquals(model.SelectedCaption, selected),
            "Subtitle search filters names without case/space sensitivity and preserves the chosen download language");
        search.Text = "العربية";
        Check(Visible("ar") && !Visible("en"), "Subtitle search finds Arabic names");
        search.Text = "francais";
        Check(Visible("fr-CA") && !Visible("en"), "Subtitle search ignores accents");
        search.Text = "fr-CA";
        Check(Visible("fr-CA"), "Subtitle search accepts regional language codes");
        search.Text = "no-such-language";
        Check(empty.Visibility == Visibility.Visible && model.CaptionChoices.All(choice =>
            ((ComboBoxItem)caption.ItemContainerGenerator.ContainerFromItem(choice)).Visibility == Visibility.Collapsed) &&
            ReferenceEquals(model.SelectedCaption, selected) && model.VttCaptions,
            "An empty search result explains recovery without clearing subtitle language or format");
        search.Text = "العربية";
        await Task.Delay(75);
        var popup = (System.Windows.Controls.Primitives.Popup)caption.Template.FindName("PART_Popup", caption);
        // The native root retains the RTL transform; rendering its child alone mirrors the text.
        var source = (HwndSource)PresentationSource.FromVisual(popup.Child);
        GetWindowRect(source.Handle, out var popupBounds);
        CaptureVisual(source.RootVisual, System.IO.Path.Combine(captures, "subtitle-search-" + variant + ".png"),
            popupBounds.Right - popupBounds.Left, popupBounds.Bottom - popupBounds.Top);
        search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(search), Environment.TickCount, Key.Enter)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Check(model.SelectedCaption.Language == "ar" && !caption.IsDropDownOpen && dialog.IsVisible,
            "Enter chooses the matching subtitle without closing the options dialog");
        caption.IsDropDownOpen = true;
        await Task.Delay(100);
        Check(search.Text == "" && Visible("en") && Visible("ar") && Visible("fr-CA"), "Reopening subtitle choices resets the filter");
        search.Text = "English";
        search.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(search), Environment.TickCount, Key.Escape)
            { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Check(!caption.IsDropDownOpen && dialog.IsVisible && model.SelectedCaption.Language == "ar",
            "Escape closes subtitle search and preserves the last selection");
        model.SelectedCaption = selected;
        model.CaptionChoices.Remove(french);
    }

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr handle);
}
