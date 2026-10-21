using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckDownloadSettings(MainWindow window, MainViewModel model, string root, string captures)
    {
        Check(!model.IsSpeedLimited && model.QueueAttempts == 2 && model.DownloadConnections == 1, "Existing defaults mean unlimited speed, one retry and one download part");
        var original = model.Queue.Add(new() { Url = "https://example.com/old.mp4", Folder = root });
        model.SettingsVisible = true;
        await Task.Delay(60); window.UpdateLayout();
        var expander = Descendants<Expander>(window.SettingsPane).Single(); expander.IsExpanded = true;
        await Task.Delay(60); window.UpdateLayout();
        var view = Descendants<DownloadLimitsView>(expander).Single();
        var toggle = (CheckBox)WpfTree.Find(view, "SpeedLimitToggle");
        var speed = (TextBox)WpfTree.Find(view, "SpeedLimitInput");
        CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, "preferences-default-en-dark.png"));
        void ToggleSpeed() => ((IToggleProvider)new CheckBoxAutomationPeer(toggle).GetPattern(PatternInterface.Toggle)).Toggle();
        ToggleSpeed(); window.UpdateLayout();
        Check(model.IsSpeedLimited && model.SpeedLimitKiB == 1024 && speed.IsVisible, "Explicit speed limiting starts with an explained positive cap instead of a magic zero");
        foreach (string invalid in new[] { "oops", "0", "-1", "1048577" })
        {
            speed.SetCurrentValue(TextBox.TextProperty, invalid); speed.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            Check(Validation.GetHasError(speed) && model.IsSpeedLimited && model.SpeedLimitKiB == 1024 && new SettingsRepository(root).Load().SpeedLimitKiB == 1024, "Invalid active cap preserves the last saved value: " + invalid);
        }
        speed.SetCurrentValue(TextBox.TextProperty, "2048"); speed.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        Check(!Validation.GetHasError(speed) && model.SpeedLimitKiB == 2048, "A corrected cap clears its error and keeps the existing KiB/s storage units");
        ToggleSpeed(); window.UpdateLayout();
        Check(!model.IsSpeedLimited && model.SpeedLimitKiB == 0 && !speed.IsVisible, "Turning the cap off preserves the legacy unlimited sentinel while hiding its numeric editor");
        ToggleSpeed(); window.UpdateLayout();
        Check(model.SpeedLimitKiB == 2048 && speed.Text == "2048", "Turning the cap back on restores the last positive value in this session");
        var retries = (ComboBox)WpfTree.Find(view, "RetriesInput");
        foreach (int totalAttempts in Enumerable.Range(1, 5))
        {
            retries.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, totalAttempts);
            Check(model.QueueAttempts == totalAttempts && new SettingsRepository(root).Load().QueueAttempts == totalAttempts, "The selected retry count preserves total-attempt storage semantics: " + totalAttempts);
        }
        model.QueueAttempts = 2;
        var connections = (ComboBox)WpfTree.Find(view, "ConnectionsInput"); connections.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, 4);
        model.SpeedLimitKiB = 512; model.DirectFileAcceleration = true;
        var receipt = model.ReceiveBrowserRequest(new(2, "enqueue", "https://youtu.be/jNQXAC9IVRw", "settings_clarity")); model.DismissNoticeCommand.Execute(null);
        var added = model.Queue.Entries[^1].Options;
        Check(receipt.Accepted && added.Connections == 4 && added.BytesPerSecond == 512 * 1024 && added.MaximumAttempts == 2 && added.DirectFileAcceleration, "New downloads receive the same parts, bytes-per-second cap, total attempts and MP4 preference through the existing protocol");
        Check(original.Options.Connections == 1 && original.Options.BytesPerSecond == 0 && original.Options.MaximumAttempts == 2 && !original.Options.DirectFileAcceleration, "Changing preferences leaves already queued download options unchanged");
        model.SettingsVisible = true;
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false }) foreach (double width in new[] { 1280d, 760d })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            window.Width = width; window.Height = width == 760 ? 640 : 800; await Task.Delay(100); window.UpdateLayout();
            Check(model.QueueAttempts == 2 && model.DownloadConnections == 4 && ((KeyValuePair<int, string>)retries.SelectedItem).Key == 2, "Locale and layout changes preserve retry/parts selections");
            Check(Descendants<TextBlock>(connections).Any(text => text.Text == ((KeyValuePair<int, string>)connections.SelectedItem).Value) && Descendants<TextBlock>(retries).Any(text => text.Text == ((KeyValuePair<int, string>)retries.SelectedItem).Value), "Rendered selections show only their translated labels, without internal keys or brackets");
            foreach (bool limited in new[] { false, true })
            {
                model.IsSpeedLimited = limited; await Task.Delay(40); window.UpdateLayout();
                Check(window.ContextViewport.ScrollableWidth <= 1 && connections.ActualWidth <= view.ActualWidth && speed.IsVisible == limited,
                    "Preferences wrap without horizontal overflow and expose the correct cap state: " + language + "/" + width + "/" + limited);
                CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, $"preferences-{language}-{dark}-{width}-{limited}.png"));
            }
        }
        model.SelectLanguageCommand.Execute("en"); model.IsSpeedLimited = true;
        speed.SetCurrentValue(TextBox.TextProperty, "0"); speed.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        Check(Validation.GetHasError(speed), "Zero cannot silently disable an active speed limit");
        model.IsSpeedLimited = false; model.IsSpeedLimited = true; window.UpdateLayout();
        Check(!Validation.GetHasError(speed) && model.SpeedLimitKiB == 512, "Explicit off/on recovers an invalid draft without losing the last valid cap");
    }

    private static void CaptureDownloadPreferences(MainWindow owner, MainViewModel model, double width, string path)
    {
        // Capture a real native root: rendering an RTL child directly loses its ancestor's mirror transform.
        var content = new DownloadLimitsView { DataContext = model, FlowDirection = model.ContentDirection };
        var fixture = new Window
        {
            Owner = owner, DataContext = model, Width = width + 48, SizeToContent = SizeToContent.Height,
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowActivated = false, ShowInTaskbar = false,
            Background = owner.Background, FontSize = owner.FontSize, FontFamily = model.ContentFont, FlowDirection = FlowDirection.LeftToRight,
            Content = new Border { Padding = new Thickness(24), Background = owner.Background, Child = content }
        };
        try { fixture.Show(); fixture.UpdateLayout(); Capture(fixture, path); }
        finally { fixture.Close(); }
    }
}
