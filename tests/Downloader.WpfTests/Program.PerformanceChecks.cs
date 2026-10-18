using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task MeasurePlaylistPerformance(MainWindow owner, string captures)
    {
        var playlist = new PlaylistInfo { Title = "1,000-video local performance fixture", Entries = Enumerable.Range(1, 1000)
            .Select(index => new PlaylistEntry { Id = index == 2 ? null : index.ToString(System.Globalization.CultureInfo.InvariantCulture), Title = "Fixture video " + index, MediaUrl = "https://example.com/" + index }).ToList() };
        var timings = new List<double>();
        for (int iteration = 0; iteration < 5; iteration++)
        {
            var watch = Stopwatch.StartNew();
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Exception failure = null;
            _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
            {
                Window dialog = null;
                try
                {
                    dialog = owner.OwnedWindows.Cast<Window>().Single();
                    timings.Add(watch.Elapsed.TotalMilliseconds);
                    var choices = LogicalDescendants(dialog).OfType<ListBox>().Single();
                    Check(choices.Items.Count == 1000, "Performance workload retains every available playlist choice");
                    int rendered = Enumerable.Range(0, choices.Items.Count).Count(index => choices.ItemContainerGenerator.ContainerFromIndex(index) != null);
                    Check(rendered is > 0 and < 100, "The large playlist renders only its visible rows and a bounded cache");
                    if (iteration == 0)
                    {
                        Capture(dialog, System.IO.Path.Combine(captures, "playlist-large.png"));
                        var boxes = choices.Items.Cast<CheckBox>().ToArray();
                        var buttons = LogicalDescendants(dialog).OfType<Button>().ToArray();
                        buttons.Single(b => Equals(b.Content, "Clear selection")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(boxes.All(box => box.IsChecked == false), "Clear selection updates virtualized and visible choices");
                        buttons.Single(b => Equals(b.Content, "Select all")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        Check(boxes.All(box => box.IsChecked == box.IsEnabled), "Select all updates every available unrealized choice and skips unavailable videos");
                        Check(boxes[0].Focus(), "Keyboard navigation begins on the first playlist checkbox");
                        foreach (var routed in new[] { Keyboard.KeyDownEvent, Keyboard.KeyUpEvent })
                            boxes[0].RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(boxes[0]), Environment.TickCount, Key.End) { RoutedEvent = routed });
                        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        dialog.UpdateLayout();
                        var target = boxes[^1];
                        var focus = Keyboard.FocusedElement as UIElement;
                        Check(target.IsVisible && (target.IsKeyboardFocusWithin || ReferenceEquals(focus, choices.ItemContainerGenerator.ContainerFromIndex(999))), "End reaches the last playlist row without using the mouse or programmatic scrolling: visible=" + target.IsVisible + ", focused=" + focus + ", item=" + (focus as ContentControl)?.Content + ", last=" + choices.ItemContainerGenerator.ContainerFromIndex(999));
                        Check(boxes.All(box => box.IsChecked == box.IsEnabled), "Keyboard navigation preserves the existing checked choices");
                        foreach (var routed in new[] { Keyboard.KeyDownEvent, Keyboard.KeyUpEvent })
                            focus.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(focus), Environment.TickCount, Key.Space) { RoutedEvent = routed });
                        Check(target.IsChecked == false, "Space toggles a distant playlist choice");
                        choices.ScrollIntoView(boxes[0]);
                        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        choices.ScrollIntoView(target);
                        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        Check(target.IsChecked == false, "Selection survives recycling and scrolling");
                        focus = Keyboard.FocusedElement as UIElement;
                        focus.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(focus), Environment.TickCount, Key.Home) { RoutedEvent = Keyboard.KeyDownEvent });
                        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        Check(boxes[0].IsKeyboardFocusWithin || ReferenceEquals(Keyboard.FocusedElement, choices.ItemContainerGenerator.ContainerFromIndex(0)), "Home returns keyboard focus to the first choice");
                        focus = Keyboard.FocusedElement as UIElement;
                        focus.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(focus), Environment.TickCount, Key.Down) { RoutedEvent = Keyboard.KeyDownEvent });
                        await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        Check(boxes[2].IsKeyboardFocusWithin || ReferenceEquals(Keyboard.FocusedElement, choices.ItemContainerGenerator.ContainerFromIndex(2)), "Arrow navigation skips an unavailable playlist choice");
                        Check(boxes[1].IsChecked == false, "Keyboard navigation leaves unavailable choices unchecked");
                        Check(boxes.Where(box => !ReferenceEquals(box, target)).All(box => box.IsChecked == box.IsEnabled), "Scrolling and keyboard navigation preserve every other checked choice");
                    }
                    LogicalDescendants(dialog).OfType<Button>().Single(b => Equals(b.Content, "Use selection")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                catch (Exception ex) { failure = ex; }
                finally { if (dialog?.IsVisible == true) dialog.Close(); finished.TrySetResult(); }
            }));
            var selected = new DesktopActions(owner).SelectPlaylist(playlist, null, value => value);
            await finished.Task;
            if (failure != null) throw failure;
            Check(selected?.Count == (iteration == 0 ? 998 : 999), "Applying selection returns exactly the chosen available videos: " + selected?.Count);
        }
        Console.WriteLine("Playlist dialog first-idle milliseconds (1,000 entries, five runs): " + string.Join(", ", timings.Select(value => value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))));
        Console.WriteLine(count + " WPF playlist checks passed.");
    }
}
