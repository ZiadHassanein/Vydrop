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
        var expander = Descendants<Expander>(window.SettingsPane).Single(item => Equals(item.Header, model.DownloadLimitsLabel)); expander.IsExpanded = true;
        await Task.Delay(60); window.UpdateLayout();
        var view = Descendants<DownloadLimitsView>(expander).Single();
        var toggle = (CheckBox)WpfTree.Find(view, "SpeedLimitToggle");
        var speed = (TextBox)WpfTree.Find(view, "SpeedLimitInput");
        var preset = (ComboBox)WpfTree.Find(view, "SpeedPresetInput");
        CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, "preferences-default-en-dark.png"));
        void ToggleSpeed() => ((IToggleProvider)new CheckBoxAutomationPeer(toggle).GetPattern(PatternInterface.Toggle)).Toggle();
        ToggleSpeed(); window.UpdateLayout();
        Check(model.IsSpeedLimited && model.SpeedLimitKiB == 1024 && preset.IsVisible && !speed.IsVisible && Equals(preset.SelectedValue, 1024), "Enabling the limit starts with a simple 1 MiB/s choice instead of a conversion exercise");
        CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, "preferences-preset-en-dark.png"));
        foreach (int kib in new[] { 512, 2048, 5120, 10240, 1024 })
        {
            preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, kib); window.UpdateLayout();
            Check(model.SpeedLimitKiB == kib && new SettingsRepository(root).Load().SpeedLimitKiB == kib && !speed.IsVisible,
                "Preset selection saves its exact legacy KiB/s value without showing a numeric field: " + kib);
        }
        preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, -1); window.UpdateLayout();
        Check(speed.IsVisible && speed.Text == "1024", "Custom exposes the exact saved value without changing the limit");
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
        ((Expander)WpfTree.Find(view, "AdvancedDownloadOptions")).IsExpanded = true; window.UpdateLayout();
        var retries = (ComboBox)WpfTree.Find(view, "RetriesInput");
        foreach (int totalAttempts in Enumerable.Range(1, 5))
        {
            retries.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, totalAttempts);
            Check(model.QueueAttempts == totalAttempts && new SettingsRepository(root).Load().QueueAttempts == totalAttempts, "The selected retry count preserves total-attempt storage semantics: " + totalAttempts);
        }
        model.QueueAttempts = 2;
        var connections = (ComboBox)WpfTree.Find(view, "ConnectionsInput"); connections.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, 8);
        Check(new SettingsRepository(root).Load().Connections == 8, "Eight download connections persist across app restarts");
        model.SpeedLimitKiB = 512; model.DirectFileAcceleration = true;
        var receipt = model.ReceiveBrowserRequest(new(2, "enqueue", "https://youtu.be/jNQXAC9IVRw", "settings_clarity")); model.DismissNoticeCommand.Execute(null);
        var added = model.Queue.Entries[^1].Options;
        Check(receipt.Accepted && added.Connections == 8 && added.BytesPerSecond == 512 * 1024 && added.MaximumAttempts == 2 && added.DirectFileAcceleration, "New downloads receive eight connections and the saved limits through the existing protocol");
        Check(original.Options.Connections == 1 && original.Options.BytesPerSecond == 0 && original.Options.MaximumAttempts == 2 && !original.Options.DirectFileAcceleration, "Changing preferences leaves already queued download options unchanged");
        model.SettingsVisible = true;
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false }) foreach (double width in new[] { 1280d, 760d })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            window.Width = width; window.Height = width == 760 ? 640 : 800; await Task.Delay(100); window.UpdateLayout();
            Check(model.QueueAttempts == 2 && model.DownloadConnections == 8 && ((KeyValuePair<int, string>)retries.SelectedItem).Key == 2, "Locale and layout changes preserve retry/parts selections");
            Check(Descendants<TextBlock>(connections).Any(text => text.Text == ((KeyValuePair<int, string>)connections.SelectedItem).Value) && Descendants<TextBlock>(retries).Any(text => text.Text == ((KeyValuePair<int, string>)retries.SelectedItem).Value), "Rendered selections show only their translated labels, without internal keys or brackets");
            foreach (bool limited in new[] { false, true })
            {
                model.IsSpeedLimited = limited; await Task.Delay(40); window.UpdateLayout();
                if (limited)
                {
                    preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, 1024); window.UpdateLayout();
                    Check(!speed.IsVisible && Equals(preset.SelectedValue, 1024), "The preset stays a preset after locale and layout changes");
                    CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, $"preferences-preset-{language}-{dark}-{width}.png"));
                    preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, -1); window.UpdateLayout();
                }
                var check = (System.Windows.Shapes.Path)toggle.Template.FindName("Check", toggle);
                var box = (Border)toggle.Template.FindName("Box", toggle);
                var checkBounds = check.TransformToAncestor(box).TransformBounds(new System.Windows.Rect(check.RenderSize));
                Check(check.FlowDirection == System.Windows.FlowDirection.LeftToRight && (!limited || Math.Abs(checkBounds.Left + checkBounds.Width / 2 - box.ActualWidth / 2) <= 1) &&
                    check.Visibility == (limited ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed),
                    "The checkmark keeps its orientation, centered alignment and correct state in both languages");
                Check(window.ContextViewport.ScrollableWidth <= 1 && connections.ActualWidth <= view.ActualWidth && speed.IsVisible == limited,
                    "Preferences wrap without horizontal overflow and expose the correct cap state: " + language + "/" + width + "/" + limited);
                CaptureDownloadPreferences(window, model, view.ActualWidth, Path.Combine(captures, $"preferences-{language}-{dark}-{width}-{limited}.png"));
            }
        }
        model.SelectLanguageCommand.Execute("en"); model.IsSpeedLimited = true;
        preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, 512);
        preset.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, -1); window.UpdateLayout();
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
