using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckUiRefinement(MainWindow window, MainViewModel model, string captures)
    {
        Check(window.InputBindings.OfType<KeyBinding>().Any(k => k.Key == Key.J && k.Modifiers == ModifierKeys.Control && ReferenceEquals(k.Command, model.HistoryCommand)) &&
            window.InputBindings.OfType<KeyBinding>().Any(k => k.Key == Key.Q && k.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && ReferenceEquals(k.Command, model.QueueCommand)),
            "History and queue shortcuts use the same commands as their visible actions");
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        for (int wait = 0; wait < 100 && !model.CanDownload; wait++) await Task.Delay(25);
        Check(model.CanDownload && ReferenceEquals(model.ActivityIcon, Application.Current.Resources["CheckIcon"]) && ReferenceEquals(model.ActivityBrush, Application.Current.Resources["Success"]),
            "A checked video has a semantic ready icon without beginning a download");
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            Check(model.ContactLabel == (language == "ar" ? "تواصل معي" : "Contact me"), "The contact action has the same meaning in " + language);
            await Task.Delay(350); window.UpdateLayout();
            Capture(window, System.IO.Path.Combine(captures, "refined-ready-" + language + (dark ? "-dark" : "-light") + ".png"));
            model.SettingsVisible = true;
            await Task.Delay(180); window.UpdateLayout();
            Capture(window, System.IO.Path.Combine(captures, "refined-settings-" + language + (dark ? "-dark" : "-light") + ".png"));
            MainWindow.FocusLinkCommand.Execute(null, window);
            await Task.Delay(120);
            var link = (TextBox)WpfTree.Find(window, "LinkInput");
            Check(!model.SettingsVisible && link.IsKeyboardFocused && link.SelectedText == model.Url,
                "Focus-link shortcut returns from Settings and selects the existing link in " + language);
            await InspectQueueModal(window, model, async dialog =>
            {
                var viewport = (ScrollViewer)WpfTree.Find(dialog, "QueueViewport");
                var limits = Descendants<Expander>(dialog).Single(e => Equals(e.Header, model.DownloadLimitsLabel));
                var toggle = Descendants<System.Windows.Controls.Primitives.ToggleButton>(limits).First();
                ((IToggleProvider)new ToggleButtonAutomationPeer(toggle).GetPattern(PatternInterface.Toggle)).Toggle();
                await Task.Delay(80); dialog.UpdateLayout();
                Check(limits.IsExpanded, "The themed advanced header retains native toggle accessibility");
                var speed = (TextBox)WpfTree.Find(dialog, "SpeedLimitInput");
                model.IsSpeedLimited = true;
                model.SelectedSpeedPreset = -1;
                int previousSpeed = model.SpeedLimitKiB;
                speed.SetCurrentValue(TextBox.TextProperty, "oops"); speed.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Check(Validation.GetHasError(speed) && model.SpeedLimitKiB == previousSpeed,
                    "Invalid numeric input preserves the saved setting and shows an error");
                viewport.ScrollToVerticalOffset(limits.TransformToAncestor(viewport).Transform(new Point()).Y + viewport.VerticalOffset);
                await Task.Delay(80); dialog.UpdateLayout();
                Check(Descendants<TextBlock>(limits).Any(t => t.IsVisible && t.Text == model.SpeedLimitValidationHint), "Numeric recovery guidance is visible and localized");
                Capture(dialog, System.IO.Path.Combine(captures, "refined-validation-" + language + (dark ? "-dark" : "-light") + ".png"));
                speed.SetCurrentValue(TextBox.TextProperty, "1024"); speed.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                Check(!Validation.GetHasError(speed) && model.SpeedLimitKiB == 1024, "Valid input clears the error and saves the setting");
                var check = (CheckBox)WpfTree.Find(dialog, "RangeAccelerationToggle");
                bool previous = model.DirectFileAcceleration;
                ((IToggleProvider)new CheckBoxAutomationPeer(check).GetPattern(PatternInterface.Toggle)).Toggle();
                Check(model.DirectFileAcceleration != previous, "The themed checkbox retains accessible toggle behavior");
                limits.IsExpanded = false;
                var schedule = Descendants<Expander>(dialog).Single(e => Equals(e.Header, model.ScheduleLabel)); schedule.IsExpanded = true;
                await Task.Delay(60); viewport.ScrollToEnd(); dialog.UpdateLayout();
                var date = Descendants<DatePicker>(dialog).Single();
                Check(System.Windows.Automation.AutomationProperties.GetName(date) == model.ScheduleDateLabel, "The schedule date has a meaningful accessible label");
                var dateText = (System.Windows.Controls.Primitives.DatePickerTextBox)date.Template.FindName("PART_TextBox", date);
                Check(ReferenceEquals(dateText.Foreground, Application.Current.Resources["Ink"]) && ReferenceEquals(date.Background, Application.Current.Resources["Field"]), "The actual date text uses the readable theme foreground and field background");
                date.IsDropDownOpen = true;
                await Task.Delay(80);
                var popup = (System.Windows.Controls.Primitives.Popup)date.Template.FindName("PART_Popup", date);
                Check(popup.IsOpen && popup.Child is Calendar, "The styled date field retains its native calendar popup");
                var calendar = (Calendar)popup.Child;
                calendar.SelectedDate = DateTime.Today.AddDays(2);
                await Task.Delay(60);
                Check(model.ScheduledDate == DateTime.Today.AddDays(2), "Selecting a calendar date updates the queue schedule");
                date.IsDropDownOpen = false;
                Capture(dialog, System.IO.Path.Combine(captures, "refined-schedule-" + language + (dark ? "-dark" : "-light") + ".png"));
            });
        }
        var download = model.DownloadCommand.ExecuteAsync(null);
        for (int wait = 0; wait < 100 && !model.IsBusy; wait++) await Task.Delay(25);
        Check(model.IsBusy && !MainWindow.FocusLinkCommand.CanExecute(null, window), "Focus-link cannot abandon an active download");
        model.CancelCommand.Execute(null); await download;
        Check(!ReferenceEquals(model.ActivityBrush, Application.Current.Resources["Success"]), "Cancelled work is not presented as a successful download");
    }
}
