using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckFilterTabs(MainWindow window, MainViewModel model, string captures)
    {
        var manager = window.ManagerContent;
        Border Line(ToggleButton tab) => (Border)tab.Template.FindName("FilterSelection", tab);
        ScaleTransform Scale(ToggleButton tab) => (ScaleTransform)Line(tab).RenderTransform;
        MotionPreferences.SetTestOverride(true);
        try
        {
            async Task Exercise(Panel filters, string[] choices)
            {
                var tabs = filters.Children.OfType<ToggleButton>().ToArray();
                Rect[] Positions() => tabs.SelectMany(tab => Descendants<TextBlock>(tab)).Where(text => text.IsVisible)
                    .Select(text => text.TransformToAncestor(filters).TransformBounds(new Rect(text.RenderSize))).ToArray();
                foreach (string choice in choices)
                {
                    var tab = tabs.Single(item => Equals(item.CommandParameter, choice));
                    var before = Positions();
                    tab.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                    await Task.Delay(30); window.UpdateLayout();
                    Check(before.SequenceEqual(Positions()), "Hover keeps every tab label and counter in the same position");
                    tab.Focus(); tab.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); tab.Command.Execute(tab.CommandParameter);
                    window.UpdateLayout();
                    // Observe the clock before yielding: a loaded machine may resume a timer
                    // after the whole animation has finished instead of at its requested delay.
                    bool animatedSelection = Scale(tab).HasAnimatedProperties;
                    await Task.Delay(35); window.UpdateLayout();
                    Check(before.SequenceEqual(Positions()) && tabs.All(item => item.FontWeight == FontWeights.Medium) &&
                        tabs.All(item => Descendants<FrameworkElement>(item).All(visual => !InteractionMotion.GetEnabled(visual) && !HoverMotion.GetEnabled(visual))),
                        "Click and focus keep tab content stationary without a hover fill or press scaling");
                    Check(tab.Template.FindName("Underline", tab) == null && animatedSelection && Line(tab).IsVisible,
                        $"Selection animates only the underline instead of drawing a rectangular tab border (tab={choice}, animated={animatedSelection}, visible={Line(tab).IsVisible})");
                    Check(manager.LibraryContent.RenderTransform.Value.IsIdentity && !manager.LibraryContent.HasAnimatedProperties,
                        "Switching media filters does not slide or fade the download workspace");
                    await WaitFor(() => tabs.All(item => !Scale(item).HasAnimatedProperties && Scale(item).ScaleX == (item.IsChecked == true ? 1 : 0)),
                        "The underline clocks settle on the selected filter");
                    window.UpdateLayout();
                    Check(before.SequenceEqual(Positions()) && tabs.All(item => !Scale(item).HasAnimatedProperties && Scale(item).ScaleX == (item.IsChecked == true ? 1 : 0)),
                        "The final selection has one complete underline and releases every animation clock");
                    tab.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
                }
            }

            foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true }) foreach (double width in new[] { 760d, 1280d })
            {
                model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                window.Width = width; model.LibraryFilterCommand.Execute("all");
                await Task.Delay(280); window.UpdateLayout();
                await Exercise(manager.LibraryFilters, new[] { "video", "audio", "all" });
                Capture(window, Path.Combine(captures, $"filters-{language}-{dark}-{width}.png"));
            }
            window.Width = 760; model.LibraryFilterCommand.Execute("queue"); await Task.Delay(280); window.UpdateLayout();
            await Exercise(manager.QueueFilters, new[] { "pending", "paused", "queue" });
            Capture(window, Path.Combine(captures, "filters-wrapped-queue.png"));
            model.LibraryFilterCommand.Execute("all"); await Task.Delay(280);
            model.LibraryFilterCommand.Execute("video"); model.LibraryFilterCommand.Execute("audio"); model.LibraryFilterCommand.Execute("all");
            await Task.Delay(250);
            var tabs = manager.LibraryFilters.Children.OfType<ToggleButton>().ToArray();
            Check(tabs.All(tab => !Scale(tab).HasAnimatedProperties && Scale(tab).ScaleX == (tab.IsChecked == true ? 1 : 0)),
                "Rapid tab changes settle on the final selection without stale underlines");
            model.LibraryFilterCommand.Execute("video"); await Task.Delay(35);
            MotionPreferences.SetTestOverride(false);
            Check(tabs.All(tab => !Scale(tab).HasAnimatedProperties && Scale(tab).ScaleX == (tab.IsChecked == true ? 1 : 0)),
                "Changing Reduce Motion mid-animation immediately settles the correct underline");
            model.LibraryFilterCommand.Execute("audio"); await Task.Delay(35);
            Check(tabs.All(tab => !Scale(tab).HasAnimatedProperties && Scale(tab).ScaleX == (tab.IsChecked == true ? 1 : 0)),
                "Reduced motion keeps tab selection immediate and clear without spatial animation");
        }
        finally { MotionPreferences.SetTestOverride(null); }
    }
}
