using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckSystemTheme(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        bool dark = model.IsDark;
        MotionPreferences.SetTestOverride(false);
        var palettes = new[]
        {
            new ThemeResources.ContrastPalette(Brushes.Black, Brushes.White, Brushes.Cyan, Brushes.Black),
            new ThemeResources.ContrastPalette(Brushes.White, Brushes.Black, Brushes.DarkBlue, Brushes.White)
        };
        var status = new TextBlock();
        status.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(MainViewModel.StatusBrush)) { Source = model });
        var notice = new TextBlock();
        notice.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(MainViewModel.NoticeBrush)) { Source = model });
        try
        {
            provider.MaintenanceHandler = (_, _) => Task.FromException<MediaToolUpdateResult>(new System.IO.IOException("Local fixture failure."));
            await model.ToolRefreshCommand.ExecuteAsync(null);
            provider.InspectionHook = _ => throw new InvalidOperationException("Local fixture unavailable.");
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await WaitFor(() => model.IsIdle && model.HasStatus, "System-theme fixture displays status and notice feedback");
            Check(model.NoticeVisible, "Notice is visible during the palette change");
            for (int index = 0; index < palettes.Length; index++)
            {
                ThemeResources.ContrastOverride = palettes[index];
                window.SystemTheme.Refresh();
                await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.DataBind);
                window.UpdateLayout();
                Check(!window.ThemeTransition.IsTransitioning, "System palette updates never animate a theme wipe");
                Check(ReferenceEquals(status.Foreground, palettes[index].Foreground) && ReferenceEquals(notice.Foreground, palettes[index].Foreground), "Status and notice bindings follow updated system brush instances");
                foreach (bool audio in new[] { false, true })
                {
                    model.Audio = audio;
                    window.UpdateLayout();
                    var selected = (RadioButton)WpfTree.Find(window, audio ? "AudioChoice" : "VideoChoice");
                    var unselected = (RadioButton)WpfTree.Find(window, audio ? "VideoChoice" : "AudioChoice");
                    var face = (Border)selected.Template.FindName("Face", selected);
                    var other = (Border)unselected.Template.FindName("Face", unselected);
                    Check(face.BorderThickness.Left > other.BorderThickness.Left, "High Contrast keeps " + (audio ? "Audio" : "Video") + " selected without changing text colour or weight");
                    Check(selected.Foreground == unselected.Foreground && selected.FontWeight == unselected.FontWeight, "Format text styling remains stable in High Contrast");
                }
                Capture(window, System.IO.Path.Combine(captures, "contrast-" + index + ".png"));
            }
            await CheckDynamicDialog(window, model, palettes, browser: false);
            await CheckDynamicDialog(window, model, palettes, browser: true);

            int subscriptions = SystemThemeMonitor.ActiveSubscriptions;
            var temporary = new Window();
            var monitor = new SystemThemeMonitor(temporary, () => model.IsDark, () => { });
            Check(SystemThemeMonitor.ActiveSubscriptions == subscriptions + 1, "A theme observer owns one subscription");
            monitor.Dispose();
            monitor.Dispose();
            temporary.Close();
            monitor.Refresh();
            Check(SystemThemeMonitor.ActiveSubscriptions == subscriptions, "Theme disposal is idempotent and detaches its subscription");
        }
        finally
        {
            ThemeResources.ContrastOverride = null;
            window.SystemTheme.Refresh();
            MotionPreferences.SetTestOverride(null);
            provider.MaintenanceHandler = null;
            provider.InspectionHook = null;
            model.Url = "";
            model.SettingsVisible = false;
        }
        Check(model.IsDark == dark && (Thickness)Application.Current.Resources["SelectedBorderThickness"] == new Thickness(1), "Leaving High Contrast restores the saved appearance and ordinary one-pixel selection border");
        Check(!Application.Current.Resources.Contains("InfoAccent"), "The unused palette token is absent");
    }

    private static async Task CheckDynamicDialog(MainWindow owner, MainViewModel model, ThemeResources.ContrastPalette[] palettes, bool browser)
    {
        Exception failure = null;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
        {
            Window dialog = null;
            try
            {
                dialog = owner.OwnedWindows.Cast<Window>().Single();
                foreach (var palette in palettes)
                {
                    ThemeResources.ContrastOverride = palette;
                    owner.SystemTheme.Refresh();
                    await owner.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.DataBind);
                    Check(ReferenceEquals(dialog.Background, palette.Background) && ReferenceEquals(dialog.Foreground, palette.Foreground), "An open code-built dialog follows current system brushes");
                    if (browser)
                    {
                        var hint = LogicalDescendants(dialog).OfType<TextBlock>().Single(t => t.Text.StartsWith("Keep Vydrop", StringComparison.Ordinal));
                        Check(ReferenceEquals(hint.Foreground, palette.Foreground), "Browser integration hint refreshes without reopening the dialog");
                    }
                    else
                    {
                        var choices = LogicalDescendants(dialog).OfType<ListBox>().Single();
                        var check = choices.Items.Cast<CheckBox>().Single();
                        Check(check.IsLoaded, "The system-theme fixture checks a realized playlist row");
                        Check(ReferenceEquals(check.Foreground, palette.Foreground), "Playlist checkboxes refresh their foreground while the dialog stays open");
                    }
                }
            }
            catch (Exception ex) { failure = ex; }
            finally { dialog?.Close(); done.TrySetResult(); }
        }));
        var actions = new DesktopActions(owner);
        if (browser) actions.ShowBrowserIntegration(t => t);
        else actions.SelectPlaylist(new PlaylistInfo { Title = "Fixture", Entries = new() { new() { Id = "fixture", MediaUrl = "https://example.com/video", Title = "Video" } } }, null, t => t);
        await done.Task;
        if (failure != null) throw failure;
    }

    private static IEnumerable<DependencyObject> LogicalDescendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in LogicalDescendants(child)) yield return descendant;
        }
    }
}
